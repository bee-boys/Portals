using System;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.PuppetMasta;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class TeleportableEntity : Teleportable
{
    public TeleportableEntity(IntPtr intPtr) : base(intPtr) { }

    protected override void OnTeleportableAwake()
    {
        CreateClone(gameObject);

        var marrowEntity = GetComponent<MarrowEntity>();

        SetupEntity(marrowEntity);
    }

    private void LateUpdate()
    {
        if (!HasPortals)
        {
            return;
        }

        UpdateClone();

        var inPortal = _inPortal;
        var outPortal = _outPortal;

        var newSign = GetPortalSign(inPortal);

        if (PassedThrough(_initialSign, newSign) && InBounds(inPortal, GetAnchor()) && !IsGrabbed())
        {
            Teleport(inPortal, outPortal);
        }
    }

    public override void Teleport(Portal inPortal, Portal outPortal)
    {
        var inMatrix = inPortal.PortalEnterMatrix;
        var inMatrixInverse = inMatrix.inverse;

        var outMatrix = outPortal.PortalExitMatrix;

        var newMatrix = CalculateTeleportedMatrix(transform.localToWorldMatrix, inMatrix, outMatrix);

        transform.position = newMatrix.GetPosition();
        transform.rotation = newMatrix.rotation;

        var oldScale = transform.localScale;
        var newScale = newMatrix.lossyScale;

        var scaleFactor = Mathf.Abs((newScale.x / oldScale.x) * (newScale.y / oldScale.y) * (newScale.z / oldScale.z));
        bool changedScale = !Mathf.Approximately(scaleFactor, 1f);

        transform.localScale = newScale;

        foreach (var body in MarrowEntity.Bodies)
        {
            if (!body.HasRigidbody)
            {
                continue;
            }

            var rigidbody = body._rigidbody;

            rigidbody.mass *= scaleFactor;
            rigidbody.inertiaTensor *= scaleFactor;

            rigidbody.velocity = outMatrix.MultiplyVector(inMatrixInverse.MultiplyVector(rigidbody.velocity));
            rigidbody.angularVelocity = outMatrix.rotation * (inMatrixInverse.rotation * rigidbody.angularVelocity);
        }

        UpdateThrowing(inMatrixInverse, outMatrix);

        if (changedScale)
        {
            UpdateJoints();
        }

        base.Teleport(inPortal, outPortal);
    }

    public void Scale(float factor)
    {
        transform.localScale = transform.localScale * factor;

        float massFactor = factor * factor * factor;

        foreach (var body in MarrowEntity.Bodies)
        {
            if (!body.HasRigidbody)
            {
                body._cachedRigidbodyInfo.mass *= massFactor;
                body._cachedRigidbodyInfo.inertiaTensor *= massFactor;
                continue;
            }

            var rigidbody = body._rigidbody;

            rigidbody.mass *= massFactor;
            rigidbody.inertiaTensor *= massFactor;
        }

        UpdateJoints();
    }

    public override Vector3 GetAnchor()
    {
        Vector3 center = Vector3.zero;
        float mass = 0f;

        foreach (var body in Bodies)
        {
            center += body.GetAnchor() * body.Mass;
            mass += body.Mass;
        }

        center /= mass;

        return center;
    }

    private void UpdateJoints()
    {
        foreach (var joint in MarrowEntity.Joints)
        {
            if (!joint.HasConfigJoint)
            {
                continue;
            }

            var defaultJointInfo = joint._defaultConfigJointInfo;

            var configJoint = joint._configurableJoint;
            var swapBodies = configJoint.swapBodies;

            var anchor = swapBodies ? defaultJointInfo.connectedAnchor : defaultJointInfo.anchor;
            var connectedAnchor = swapBodies ? defaultJointInfo.anchor : defaultJointInfo.connectedAnchor;

            // Don't change it unless necessary, as it will change the joint space
            // Thankfully when it has to change MarrowJoint has a method for writing the original
            if (configJoint.autoConfigureConnectedAnchor)
            {
                configJoint.autoConfigureConnectedAnchor = false;

                joint.WriteJointSpace();
            }

            configJoint.anchor = anchor;
            configJoint.connectedAnchor = connectedAnchor;
        }
    }

    private void UpdateThrowing(Matrix4x4 inMatrixInverse, Matrix4x4 outMatrix)
    {
        foreach (var body in Bodies)
        {
            if (!body.HasHost)
            {
                continue;
            }

            foreach (var grip in body.Host._grips)
            {
                for (var i = 0; i < grip.velocityHistory.Length; i++)
                {
                    grip.velocityHistory[i] = outMatrix.MultiplyVector(inMatrixInverse.MultiplyVector(grip.velocityHistory[i]));
                    grip.angVelocityHistory[i] = outMatrix.rotation * (inMatrixInverse.rotation * grip.angVelocityHistory[i]);
                }
            }
        }
    }
}
