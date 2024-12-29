using System;
using System.Collections.Generic;

using MelonLoader;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Utilities;
using Il2CppSLZ.Marrow.Warehouse;
using Il2CppSLZ.VRMK;
using Il2CppSLZ.Bonelab;

using UnityEngine;

using Portals.Rendering;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class TeleportableRigManager : Teleportable
{
    public struct PendingTransform
    {
        public Transform transform;
        public Vector3 position;
        public Quaternion rotation;
    }

    public TeleportableRigManager(IntPtr intPtr) : base(intPtr) { }

    public RigManager RigManager => _rigManager;

    private RigManager _rigManager = null;

    public Transform Headset => _headset;

    private Transform _headset = null;

    private Il2CppSystem.Action _onPostLateUpdate = null;

    protected override void OnTeleportableAwake()
    {
        base.OnTeleportableAwake();

        _rigManager = GetComponent<RigManager>();

        _rigManager.onAvatarSwapped += (Il2CppSystem.Action)OnAvatarSwapped;

        _headset = _rigManager.controllerRig.TryCast<OpenControllerRig>().headset;

        CreateClone(_rigManager.gameObject);

        var marrowEntity = _rigManager.physicsRig.marrowEntity;

        SetupEntity(marrowEntity);
    }

    protected override void OnTeleportableStart()
    {
        base.OnTeleportableStart();

        _onPostLateUpdate = (Il2CppSystem.Action)OnPostLateUpdate;
        _rigManager.OnPostLateUpdate += _onPostLateUpdate;
    }

    protected override void OnTeleportableDestroy()
    {
        base.OnTeleportableDestroy();

        _rigManager.OnPostLateUpdate -= _onPostLateUpdate;
        _onPostLateUpdate = null;
    }

    private void OnPostLateUpdate()
    {
        UpdateRotationCorrection();

        if (!HasPortals)
        {
            return;
        }

        UpdateClone();

        var newSign = GetPortalSign(_inPortal);

        if (PassedThrough(_initialSign, newSign) && !IsGrabbed())
        {
            Teleport(_inPortal, _outPortal);

            SetPortals(_outPortal, _inPortal);

            UpdateClone();
        }
    }

    private bool _correctRotation = false;
    private bool _correctBallLoco = false;

    private void UpdateRotationCorrection()
    {
        if (!_correctRotation)
        {
            return;
        }

        if (RigManager.bodyState != RigManager.BodyState.OnFoot)
        {
            return;
        }

        var remapHeptaRig = RigManager.remapHeptaRig;
        var currentRotation = remapHeptaRig.transform.rotation;
        var targetRotation = Quaternion.FromToRotation(currentRotation * Vector3.up, Vector3.up) * currentRotation;

        var slerpRotation = Quaternion.Slerp(currentRotation, targetRotation, 1f - Mathf.Exp(-8f * Time.deltaTime));

        if (Quaternion.Angle(targetRotation, slerpRotation) <= 1f)
        {
            slerpRotation = targetRotation;
            _correctRotation = false;

            if (_correctBallLoco)
            {
                ToggleBallLoco(true);
                _correctBallLoco = false;
            }
        }

        remapHeptaRig.transform.rotation = slerpRotation;
    }

    private void ToggleBallLoco(bool enabled)
    {
        var physicsRig = RigManager.physicsRig;

        if (enabled)
        {
            physicsRig.EnableBallLoco();

            var pelvisPosition = physicsRig.m_pelvis.position;
            physicsRig.feet.transform.position = pelvisPosition;
            physicsRig.knee.transform.position = pelvisPosition;
        }
        else
        {
            physicsRig.DisableBallLoco();
        }
    }

    private void OnAvatarSwapped()
    {
        ClearPortals();

        CreateClone(RigManager.gameObject);
    }

    public override Vector3 GetAnchor()
    {
        return Headset.position;
    }

    public override void Teleport(Portal inPortal, Portal outPortal)
    {
        var inTransform = inPortal.transform;
        var outTransform = outPortal.transform;

        var newScale = outTransform.lossyScale.y / inTransform.lossyScale.y;

        if (!Mathf.Approximately(newScale, 1f))
        {
            ScalePlayer(newScale);
        }

        var pendingTransforms = new List<PendingTransform>();
        foreach (var rigidbody in MarrowEntity.Bodies)
        {
            var rigidbodyTransform = rigidbody.transform;
            var rigidbodyMatrix = CalculateTeleportedMatrix(rigidbodyTransform.localToWorldMatrix, inTransform, outTransform);

            pendingTransforms.Add(new PendingTransform()
            {
                transform = rigidbody.transform,
                position = rigidbodyMatrix.GetPosition(),
                rotation = rigidbodyMatrix.GetRotation(),
            });
        }

        foreach (var rig in RigManager.remapRigs)
        {
            pendingTransforms.Add(CreatePendingTransform(rig.transform, inTransform, outTransform));
        }

        pendingTransforms.Add(CreatePendingTransform(RigManager.controllerRig.transform, inTransform, outTransform));

        var anchor = RigManager.physicsRig.centerOfPressure;

        var newMatrix = CalculateTeleportedMatrix(anchor.localToWorldMatrix, inTransform, outTransform);

        var newPosition = newMatrix.GetPosition();
        var newRotation = newMatrix.rotation;

        var displacePosition = newPosition - anchor.position;
        var displaceRotation = newRotation * Quaternion.Inverse(anchor.rotation);

        var displaceTransform = SimpleTransform.Create(displacePosition, displaceRotation);

        RigManager.controllerRig.Teleport(displaceTransform, false);

        foreach (var rig in RigManager.remapRigs)
        {
            rig.Teleport(displaceTransform, false);
        }

        RigManager.physicsRig.Teleport(displaceTransform, false);

        foreach (var pendingTransform in pendingTransforms)
        {
            pendingTransform.transform.position = pendingTransform.position;
            pendingTransform.transform.rotation = pendingTransform.rotation;
        }

        foreach (var body in MarrowEntity.Bodies)
        {
            if (!body.HasRigidbody)
            {
                continue;
            }

            var rigidbody = body._rigidbody;

            rigidbody.velocity = outTransform.TransformVector(inTransform.InverseTransformVector(rigidbody.velocity));
            rigidbody.angularVelocity = outTransform.TransformDirection(inTransform.InverseTransformDirection(rigidbody.angularVelocity));
        }

        var remapRig = RigManager.remapHeptaRig;

        remapRig._currentVelocity = TransformVector2(remapRig._currentVelocity, inTransform, outTransform);
        remapRig._currentAcceleration = TransformVector2(remapRig._currentAcceleration, inTransform, outTransform);
        remapRig._effectiveAcceleration = TransformVector2(remapRig._effectiveAcceleration, inTransform, outTransform);

        TeleportHands(RigManager.physicsRig.leftHand, RigManager.physicsRig.rightHand, inPortal, outPortal);

        _correctRotation = true;

        if (Vector3.Angle(remapRig.transform.up, Vector3.up) > 10f)
        {
            ToggleBallLoco(false);
            _correctBallLoco = true;
        }

        RigManager.physicsRig.artOutput.ArtOutputUpdate(RigManager.physicsRig);
        RigManager.physicsRig.artOutput.ArtOutputLateUpdate(RigManager.physicsRig);
    }

    private Vector2 TransformVector2(Vector2 vector, Transform inTransform, Transform outTransform)
    {
        var newVector = new Vector3(vector.x, 0f, vector.y);

        newVector = outTransform.TransformVector(inTransform.InverseTransformVector(newVector));

        return new Vector2(newVector.x, newVector.z);
    }

    private PendingTransform CreatePendingTransform(Transform transform, Transform inTransform, Transform outTransform)
    {
        var matrix = CalculateTeleportedMatrix(transform.localToWorldMatrix, inTransform, outTransform);
        return new PendingTransform()
        {
            transform = transform,
            position = matrix.GetPosition(),
            rotation = matrix.GetRotation(),
        };
    }

    private void ScalePlayer(float factor)
    {
        var newScale = RigManager.avatar.transform.localScale * factor;

        var crate = RigManager.AvatarCrate.Crate;

        var onLoaded = (GameObject avatar) =>
        {
            var currentAvatar = RigManager.avatar.transform;

            GameObject instance = GameObject.Instantiate(avatar, CloneCreator.TempCloningTransform);
            instance.SetActive(false);
            instance.name = avatar.name;

            instance.transform.parent = RigManager.transform;
            instance.transform.SetPositionAndRotation(currentAvatar.position, currentAvatar.rotation);
            instance.transform.localScale = newScale;

            var avatarComponent = instance.GetComponent<Avatar>();
            RigManager.SwitchAvatar(avatarComponent);

            RigManager._avatarCrate = new AvatarCrateReference(crate.Barcode);
            RigManager.onAvatarSwapped?.Invoke();
            RigManager.onAvatarSwapped2?.Invoke(crate.Barcode);

            PlayerRefs.Instance.PlayerBodyVitals.PROPEGATE_SOFT();
        };

        var asset = crate.MainGameObject.Asset;

        if (asset != null)
        {
            onLoaded(asset);
        }
        else
        {
            crate.LoadAsset(onLoaded);
        }
    }

    private static void TeleportHands(Hand leftHand, Hand rightHand, Portal inPortal, Portal outPortal)
    {
        var leftEntity = GetEntityInHand(leftHand);
        var rightEntity = GetEntityInHand(rightHand);

        if (leftEntity == rightEntity)
        {
            rightEntity = null;
        }

        if (leftEntity != null)
        {
            leftEntity.Teleport(inPortal, outPortal);
        }

        if (rightEntity != null)
        {
            rightEntity.Teleport(inPortal, outPortal);
        }
    }

    private static TeleportableEntity GetEntityInHand(Hand hand)
    {
        var attachedObject = hand.m_CurrentAttachedGO;

        if (attachedObject == null)
        {
            return null;
        }

        var grip = Grip.Cache.Get(attachedObject);

        if (grip == null)
        {
            return null;
        }

        var entity = grip._marrowEntity;

        if (entity == null)
        {
            grip.ForceDetach(hand);
            return null;
        }

        var teleportableEntity = entity.GetComponent<TeleportableEntity>();

        if (teleportableEntity == null)
        {
            grip.ForceDetach(hand);
            return null;
        }

        return teleportableEntity;
    }
}
