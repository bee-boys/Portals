using System;
using System.Collections.Generic;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow;

using Portals.Rendering;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class Teleportable : MonoBehaviour
{
    public Teleportable(IntPtr intPtr) : base(intPtr) { }

    public MarrowEntity MarrowEntity => _marrowEntity;
    private MarrowEntity _marrowEntity = null;

    public bool HasHostManager => _hasHostManager;
    public InteractableHostManager HostManager => _hostManager;

    private InteractableHostManager _hostManager = null;
    private bool _hasHostManager = false;

    public List<TeleportableBody> Bodies => _bodies;
    private List<TeleportableBody> _bodies = new();

    public bool HasPortals => _inPortal && _outPortal;

    public bool HasClone => _cloneRenderer && _cloneRenderer.OriginalTransform && _cloneRenderer.CloneTransform;

    public float EnterSign => _initialSign;

    public Portal InPortal => _inPortal;

    public Portal OutPortal => _outPortal;

    protected Portal _inPortal = null;
    protected Portal _outPortal = null;

    protected float _initialSign = 0f;

    protected CloneRenderer _cloneRenderer = null;

    private void Awake()
    {
        OnTeleportableAwake();
    }

    private void Start()
    {
        OnTeleportableStart();
    }

    private void OnDestroy()
    {
        OnTeleportableDestroy();
    }

    private void OnEnable()
    {
        OnTeleportableEnable();
    }

    private void OnDisable()
    {
        OnTeleportableDisable();
    }

    protected virtual void OnTeleportableAwake() 
    { 
    }

    protected virtual void OnTeleportableStart()
    {
    }

    protected virtual void OnTeleportableDestroy() 
    {
        DestroyClone();
    }

    protected virtual void OnTeleportableEnable()
    {
    }

    protected virtual void OnTeleportableDisable()
    {
        ClearPortals();
    }

    protected virtual void SetupEntity(MarrowEntity marrowEntity)
    {
        _marrowEntity = marrowEntity;

        _hostManager = marrowEntity.GetComponent<InteractableHostManager>();
        _hasHostManager = _hostManager != null;

        foreach (var body in marrowEntity.Bodies)
        {
            SetupBody(body);
        }
    }

    protected virtual void SetupBody(MarrowBody marrowBody)
    {
        var tracker = marrowBody.gameObject.AddComponent<TeleportableBody>();

        _bodies.Add(tracker);

        tracker.OnPortalEnterEvent += OnPortalEnterCallback;
        tracker.OnPortalExitEvent += OnPortalExitCallback;
    }

    protected int _portalCount = 0;

    protected void OnPortalEnterCallback(TeleportableBody body, Portal portal)
    {
        _portalCount++;

        if (_portalCount == 1)
        {
            SetPortals(portal, portal.OtherPortal);
        }
    }

    protected void OnPortalExitCallback(TeleportableBody body, Portal portal)
    {
        _portalCount--;

        if (_portalCount <= 0)
        {
            SetPortals(null, null);

            _portalCount = 0;
        }
    }

    public virtual Vector3 GetAnchor()
    {
        return transform.position;
    }

    public virtual void Teleport(Portal inPortal, Portal outPortal)
    {
    }

    public void SetPortals(Portal inPortal, Portal outPortal)
    {
        if (inPortal == null || outPortal == null)
        {
            ClearPortals();
            return;
        }

        _inPortal = inPortal;
        _outPortal = outPortal;

        _initialSign = GetPortalSign(inPortal);

        if (_cloneRenderer)
        {
            _cloneRenderer.Show();
        }
    }

    public void ClearPortals()
    {
        _inPortal = null;
        _outPortal = null;

        _initialSign = 0f;
        _portalCount = 0;

        if (_cloneRenderer)
        {
            _cloneRenderer.Hide();
        }
    }

    public void CalculateTrackers()
    {
        foreach (var body in Bodies)
        {
            body.CalculateTracker();
        }
    }

    public void CreateClone(GameObject root)
    {
        DestroyClone();

        _cloneRenderer = CloneCreator.CreateClone(root);

        if (HasPortals)
        {
            _cloneRenderer.Show();
        }
    }

    public void DestroyClone()
    {
        if (_cloneRenderer == null)
        {
            return;
        }

        Destroy(_cloneRenderer.gameObject);
        _cloneRenderer = null;
    }

    public void UpdateClone()
    {
        if (!HasClone || !HasPortals)
        {
            return;
        }

        var cloneMatrix = CalculateTeleportedMatrix(_cloneRenderer.OriginalTransform.localToWorldMatrix, _inPortal.transform, _outPortal.transform);

        _cloneRenderer.CloneTransform.position = cloneMatrix.GetPosition();
        _cloneRenderer.CloneTransform.rotation = cloneMatrix.rotation;
        _cloneRenderer.CloneTransform.localScale = cloneMatrix.lossyScale;

        _cloneRenderer.OnCloneUpdate();
    }

    public float GetPortalSign(Portal portal)
    {
        return GetPortalSign(portal, GetAnchor());
    }

    public float GetPortalSign(Portal portal, Vector3 point)
    {
        return Mathf.Sign(portal.transform.InverseTransformPoint(point).z);
    }

    public Matrix4x4 CalculateTeleportedMatrix(Matrix4x4 matrix, Transform inTransform, Transform outTransform)
    {
        return CalculateTeleportedMatrix(matrix, inTransform.localToWorldMatrix, outTransform.localToWorldMatrix);
    }

    public Matrix4x4 CalculateTeleportedMatrix(Matrix4x4 matrix, Matrix4x4 inMatrix, Matrix4x4 outMatrix)
    {
        var relativeMatrix = inMatrix.inverse * matrix;

        var newMatrix = outMatrix * relativeMatrix;

        return newMatrix;
    }

    public bool PassedThrough(float initialSign, float currentSign)
    {
        if (Mathf.Approximately(currentSign, 0f))
        {
            return false;
        }

        return Mathf.Abs(currentSign - initialSign) > 1f;
    }

    public bool InBounds(Portal portal, Vector3 point)
    {
        var portalExtents = portal.Size * 0.5f;
        var pointInPortal = portal.transform.InverseTransformPoint(point);

        bool outOfBounds =
            pointInPortal.x > portalExtents.x || pointInPortal.x < -portalExtents.x ||
            pointInPortal.y > portalExtents.y || pointInPortal.y < -portalExtents.y;

        return !outOfBounds;
    }

    public bool IsGrabbed()
    {
        if (HasHostManager)
        {
            return HostManager.grabbedHosts.Count > 0;
        }

        foreach (var tracker in Bodies)
        {
            if (!tracker.HasHost)
            {
                continue;
            }

            if (tracker.Host.HandCount() > 0)
            {
                return true;
            }
        }

        return false;
    }
}
