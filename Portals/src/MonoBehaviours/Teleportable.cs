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

    public List<TeleportableTracker> Trackers => _trackers;
    private List<TeleportableTracker> _trackers = new();

    public bool HasPortals => _inPortal && _outPortal;

    protected Portal _inPortal = null;
    protected Portal _outPortal = null;

    protected float _initialSign = 0f;

    protected CloneRenderer _cloneRenderer = null;

    private List<Collider> _ignoredColliders = new();

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

    protected void SetupEntity(MarrowEntity marrowEntity)
    {
        _marrowEntity = marrowEntity;

        _hostManager = marrowEntity.GetComponent<InteractableHostManager>();
        _hasHostManager = _hostManager != null;

        foreach (var body in marrowEntity.Bodies)
        {
            var tracker = body.gameObject.AddComponent<TeleportableTracker>();

            _trackers.Add(tracker);

            tracker.OnTriggerEnterEvent += OnTriggerEnterCallback;
            tracker.OnTriggerExitEvent += OnTriggerExitCallback;
        }
    }

    protected int _portalCount = 0;

    protected void OnTriggerEnterCallback(Collider other)
    {
        var portal = other.GetComponentInParent<Portal>();

        if (portal != null)
        {
            _portalCount++;

            if (_portalCount == 1)
            {
                SetPortals(portal, portal.OtherPortal);
            }
        }
    }

    protected void OnTriggerExitCallback(Collider other)
    {
        var portal = other.GetComponentInParent<Portal>();

        if (portal != null)
        {
            _portalCount--;

            if (_portalCount <= 0)
            {
                SetPortals(null, null);

                _portalCount = 0;
            }
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

    public void IgnoreCollision(Collider collider, bool ignore)
    {
        if (ignore)
        {
            _ignoredColliders.Add(collider);
        }
        else
        {
            _ignoredColliders.Remove(collider);
        }

        foreach (var body in MarrowEntity.Bodies)
        {
            body.IgnoreCollision(collider, ignore);
        }
    }

    public void ResetCollisions()
    {
        foreach (var collider in _ignoredColliders)
        {
            foreach (var body in MarrowEntity.Bodies)
            {
                body.IgnoreCollision(collider, false);
            }
        }

        _ignoredColliders.Clear();
    }

    public void CreateClone(GameObject root)
    {
        DestroyClone();

        _cloneRenderer = CloneCreator.CreateClone(root);
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
        var cloneMatrix = CalculateTeleportedMatrix(_cloneRenderer.OriginalTransform.localToWorldMatrix, _inPortal.transform, _outPortal.transform);

        _cloneRenderer.CloneTransform.position = cloneMatrix.GetPosition();
        _cloneRenderer.CloneTransform.rotation = cloneMatrix.rotation;
        _cloneRenderer.CloneTransform.localScale = cloneMatrix.lossyScale;

        _cloneRenderer.OnCloneUpdate();
    }

    public float GetPortalSign(Portal portal)
    {
        return Mathf.Sign(portal.transform.InverseTransformPoint(GetAnchor()).z);
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
        return Mathf.Abs(currentSign - initialSign) > 1f;
    }

    public bool IsGrabbed()
    {
        if (HasHostManager)
        {
            return HostManager.grabbedHosts.Count > 0;
        }

        foreach (var tracker in Trackers)
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
