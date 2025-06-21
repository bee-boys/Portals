using System;
using System.Collections.Generic;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow.Interaction;

using Il2CppInterop.Runtime.Attributes;

using Portals.Rendering;

namespace Portals.MonoBehaviours;

public delegate void PortalTransitionEvent(Portal inPortal, Portal outPortal);

[RegisterTypeInIl2Cpp]
public class Teleportable : MonoBehaviour
{
    public Teleportable(IntPtr intPtr) : base(intPtr) { }

    public static readonly List<Teleportable> Teleportables = new();

    public static event Action<Teleportable> OnTeleportableEnabled, OnTeleportableDisabled;

    public delegate bool TryTeleportCallback(Teleportable teleportable, Portal inPortal, Portal outPortal);
    public delegate void TeleportCallback(Teleportable teleportable, Portal inPortal, Portal outPortal);

    public static event TryTeleportCallback OnTryTeleportEvent;
    public static event TeleportCallback OnBeforeTeleportEvent;

    public MarrowEntity MarrowEntity => _marrowEntity;
    private MarrowEntity _marrowEntity = null;

    [HideFromIl2Cpp]
    public List<TeleportableBody> Bodies => _bodies;
    private List<TeleportableBody> _bodies = new();

    public bool HasPortals => _inPortal && _outPortal;

    public bool HasClone => _cloneRenderer && _cloneRenderer.OriginalTransform && _cloneRenderer.CloneTransform;

    public float EnterSign => _initialSign;

    public Portal InPortal => _inPortal;

    public Portal OutPortal => _outPortal;

    [HideFromIl2Cpp]
    public event PortalTransitionEvent OnPortalsEntered, OnPortalsExited;

    protected Portal _inPortal = null;
    protected Portal _outPortal = null;

    protected float _initialSign = 0f;

    protected CloneRenderer _cloneRenderer = null;
    public CloneRenderer CloneRenderer => _cloneRenderer;

    private void Awake()
    {
        Teleportables.Add(this);

        OnTeleportableAwake();
    }

    private void Start()
    {
        OnTeleportableStart();
    }

    private void OnDestroy()
    {
        Teleportables.Remove(this);

        OnTeleportableDestroy();
    }

    private void OnEnable()
    {
        OnTeleportableEnable();

        OnTeleportableEnabled?.Invoke(this);
    }

    private void OnDisable()
    {
        OnTeleportableDisable();

        OnTeleportableDisabled?.Invoke(this);
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

        foreach (var body in marrowEntity.Bodies)
        {
            SetupBody(body);
        }
    }

    protected virtual void SetupBody(MarrowBody marrowBody)
    {
        var body = marrowBody.gameObject.AddComponent<TeleportableBody>();

        _bodies.Add(body);

        body.Teleportable = this;

        HookBodyPortal(body);
    }

    public void HookBodyPortal(TeleportableBody body)
    {
        body.OnPortalEnterEvent += OnPortalEnterCallback;
        body.OnPortalExitEvent += OnPortalExitCallback;

        if (body.CurrentPortal != null)
        {
            OnPortalEnterCallback(body, body.CurrentPortal);
        }
    }

    public void UnhookBodyPortal(TeleportableBody body)
    {
        body.OnPortalEnterEvent -= OnPortalEnterCallback;
        body.OnPortalExitEvent -= OnPortalExitCallback;

        if (body.CurrentPortal != null)
        {
            OnPortalExitCallback(body, body.CurrentPortal);
        }
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

    public bool TryTeleport(Portal inPortal, Portal outPortal)
    {
        if (OnTryTeleportEvent != null)
        {
            bool teleport = OnTryTeleportEvent(this, inPortal, outPortal);

            if (!teleport)
            {
                return false;
            }
        }

        OnBeforeTeleportEvent?.Invoke(this, inPortal, outPortal);

        Teleport(inPortal, outPortal);

        return true;
    }

    public void Teleport(Portal inPortal, Portal outPortal)
    {
        OnTeleport(inPortal, outPortal);

        SetPortals(outPortal, inPortal);

        UpdateClone();

        foreach (var body in Bodies)
        {
            foreach (var parasite in body.ParasiteBodies)
            {
                if (!parasite.HasRigidbody)
                {
                    continue;
                }

                parasite.Teleportable.Teleport(inPortal, outPortal);
            }
        }
    }

    protected virtual void OnTeleport(Portal inPortal, Portal outPortal) { }

    public void SetPortals(Portal inPortal, Portal outPortal)
    {
        if (inPortal == null || outPortal == null)
        {
            ClearPortals();
            return;
        }

        // Reenable collision with the previous portal
        if (_inPortal)
        {
            OnPortalsExited?.Invoke(_inPortal, _outPortal);

            IgnoreCollision(_inPortal, false);

            _inPortal.Expander.IgnoreCollision(this, true);
        }

        _inPortal = inPortal;
        _outPortal = outPortal;

        var grabbingTeleportable = GetGrabbingTeleportable();

        // One sided portals should only ever check from positive Z direction for stability reasons
        if (inPortal.OneSided)
        {
            _initialSign = 1f;
        }
        else if (grabbingTeleportable)
        {
            _initialSign = GetPortalSign(inPortal, grabbingTeleportable.GetAnchor());
        }
        else
        {
            _initialSign = GetPortalSign(inPortal);
        }

        // Disable collision with the new portal
        IgnoreCollision(inPortal, true);

        // Enable collision with its expander
        _inPortal.Expander.IgnoreCollision(this, _initialSign, false);

        if (HasClone)
        {
            CloneRenderer.Show();
        }
        else
        {
            _ensureCloneNextFrame = true;
        }

        OnPortalsChanged(inPortal, outPortal);

        OnPortalsEntered?.Invoke(inPortal, outPortal);
    }

    public void ClearPortals()
    {
        if (_inPortal && _outPortal)
        {
            OnPortalsExited?.Invoke(_inPortal, _outPortal);
        }

        if (_inPortal)
        {
            _inPortal.Expander.IgnoreCollision(this, true);
        }

        ResetCollision();

        _inPortal = null;
        _outPortal = null;

        _initialSign = 0f;
        _portalCount = 0;

        if (HasClone)
        {
            CloneRenderer.Hide();
        }

        OnPortalsChanged(null, null);
    }

    public void IgnoreCollision(Portal portal, bool ignore = true)
    {
        foreach (var body in Bodies)
        {
            body.IgnoreCollision(portal, ignore);
        }
    }

    public void ResetCollision()
    {
        foreach (var body in Bodies)
        {
            body.ResetCollision();
        }
    }

    protected virtual void OnPortalsChanged(Portal inPortal, Portal outPortal) { }

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

        _cloneRenderer = CloneCreator.CreateCloneRenderer(root);

        if (HasPortals)
        {
            CloneRenderer.Show();
        }
    }

    private bool _ensureCloneNextFrame = false;

    public void EnsureCloneExists()
    {
        if (HasClone)
        {
            return;
        }

        OnEnsureClone();

        if (HasClone && HasPortals)
        {
            CloneRenderer.Show();
        }
    }

    protected virtual void OnEnsureClone() { }

    private void Update()
    {
        if (_ensureCloneNextFrame)
        {
            _ensureCloneNextFrame = false;
            EnsureCloneExists();
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

        var cloneMatrix = CalculateTeleportedMatrix(CloneRenderer.OriginalTransform.localToWorldMatrix, _inPortal.PortalEnterMatrix, _outPortal.PortalExitMatrix);

        CloneRenderer.CloneTransform.position = cloneMatrix.GetPosition();
        CloneRenderer.CloneTransform.rotation = cloneMatrix.rotation;
        CloneRenderer.CloneTransform.localScale = cloneMatrix.lossyScale;

        CloneRenderer.OnCloneUpdate();
    }

    public float GetPortalSign(Portal portal)
    {
        return GetPortalSign(portal, GetAnchor());
    }

    public float GetPortalSign(Portal portal, Vector3 point)
    {
        return Mathf.Sign(portal.transform.InverseTransformPoint(point).z);
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
        foreach (var body in Bodies)
        {
            if (body.IsPacked)
            {
                return true;
            }

            if (body.IsGrabbed())
            {
                return true;
            }
        }

        return false;
    }

    [HideFromIl2Cpp]
    public Teleportable GetGrabbingTeleportable()
    {
        foreach (var body in Bodies)
        {
            if (!body.HasHost)
            {
                continue;
            }

            foreach (var hand in body.Host._hands)
            {
                var teleportable = hand.GetComponentInParent<Teleportable>();

                if (teleportable)
                {
                    return teleportable;
                }
            }
        }

        return null;
    }
}
