using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;

using Il2CppInterop.Runtime.Attributes;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class TeleportableBody : MonoBehaviour
{
    public TeleportableBody(IntPtr intPtr) : base(intPtr) { }

    [HideFromIl2Cpp]
    public Teleportable Teleportable { get; set; } = null;

    public MarrowBody MarrowBody => _marrowBody;
    public InteractableHost Host => _host;
    public bool HasHost => _hasHost;
    public bool HasRigidbody => MarrowBody.HasRigidbody && !MarrowBody._rigidbody.isKinematic;

    public float Mass => MarrowBody.Mass;

    public TeleportableTracker Tracker => _tracker;

    private MarrowBody _marrowBody = null;
    private InteractableHost _host = null;
    private bool _hasHost = false;

    private TeleportableTracker _tracker = null;

    public Portal CurrentPortal => OverridePortal ? OverridePortal : TriggeredPortal;

    public Portal OverridePortal
    {
        get
        {
            return _overridePortal;
        }
        set
        {
            var previous = CurrentPortal;

            _overridePortal = value;

            CheckPortalChange(previous, CurrentPortal);
        }
    }
    private Portal _overridePortal = null;

    public Portal TriggeredPortal
    {
        get
        {
            return _triggeredPortal;
        }
        set
        {
            var previousTriggered = _triggeredPortal;
            var currentTriggered = value;

            if (previousTriggered != null)
            {
                previousTriggered.OnClosedEvent -= OnTriggeredPortalClosed;
            }

            var previous = CurrentPortal;

            _triggeredPortal = currentTriggered;

            if (currentTriggered != null)
            {
                currentTriggered.OnClosedEvent += OnTriggeredPortalClosed;
            }

            CheckPortalChange(previous, CurrentPortal);
        }
    }

    private Portal _triggeredPortal = null;

    [HideFromIl2Cpp]
    public event Action<TeleportableBody, Portal> OnPortalEnterEvent, OnPortalExitEvent;

    [HideFromIl2Cpp]
    public TeleportableBody HostBody { get; private set; } = null;

    [HideFromIl2Cpp]
    public List<TeleportableBody> ParasiteBodies { get; private set; } = new();

    [HideFromIl2Cpp]
    public bool IsPacked { get; private set; } = false;

    private void Awake()
    {
        _marrowBody = GetComponent<MarrowBody>();

        GetHost();

        CreateTracker();
    }

    private void OnDisable()
    {
        TriggeredPortal = null;
        OverridePortal = null;
    }

    private void CheckPortalChange(Portal previous, Portal current)
    {
        if (previous == current)
        {
            return;
        }

        if (previous != null)
        {
            OnPortalExitEvent?.Invoke(this, previous);
        }

        if (current != null)
        {
            OnPortalEnterEvent?.Invoke(this, current);
        }
    }

    private void OnTriggeredPortalClosed()
    {
        TriggeredPortal = null;
    }

    private void GetHost()
    {
        _host = GetComponent<InteractableHost>();
        _hasHost = _host != null;

        if (!HasHost)
        {
            return;
        }

        _host.onHandAttachedDelegate += (Il2CppSystem.Action<InteractableHost, Hand>)OnHandAttached;
        _host.onHandDetachedDelegate += (Il2CppSystem.Action<InteractableHost, Hand>)OnHandDetached;
    }

    private void OnHandAttached(InteractableHost host, Hand hand)
    {
        var handTeleportable = hand.GetComponentInParent<Teleportable>();

        if (handTeleportable != null)
        {
            OverridePortal = handTeleportable.InPortal;
        }
    }

    private void OnHandDetached(InteractableHost host, Hand hand)
    {
        if (host.HandCount() <= 0)
        {
            var handTeleportable = hand.GetComponentInParent<Teleportable>();

            if (HasRigidbody && handTeleportable != null && handTeleportable.HasPortals && Teleportable.PassedThrough(handTeleportable.EnterSign, Teleportable.GetPortalSign(handTeleportable.InPortal)) && !Teleportable.IsGrabbed())
            {
                Teleportable.Teleport(handTeleportable.InPortal, handTeleportable.OutPortal);
            }

            OverridePortal = null;
        }
    }

    private void CreateTracker()
    {
        var trackerGameObject = new GameObject("Teleportable Tracker");
        trackerGameObject.transform.parent = transform;
        trackerGameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        trackerGameObject.transform.localScale = Vector3.one;

        _tracker = trackerGameObject.AddComponent<TeleportableTracker>();

        _tracker.OnTriggerEnterEvent += OnTrackerTriggerEnter;
        _tracker.OnTriggerExitEvent += OnTrackerTriggerExit;

        CalculateTracker();
    }

    private void OnTrackerTriggerEnter(Collider other)
    {
        if (!HasRigidbody)
        {
            return;
        }

        var portal = other.GetComponentInParent<Portal>();

        if (portal != null && ValidatePortal(portal))
        {
            TriggeredPortal = portal;
        }
    }

    private void OnTrackerTriggerExit(Collider other)
    {
        if (!TriggeredPortal)
        {
            return;
        }

        var portal = other.GetComponentInParent<Portal>();

        if (portal == TriggeredPortal)
        {
            TriggeredPortal = null;
        }
    }

    public bool ValidatePortal(Portal portal)
    {
        if (portal.ParentBody == null)
        {
            return true;
        }

        var portalEntity = portal.ParentBody.Entity;

        if (portalEntity == Teleportable.MarrowEntity)
        {
            return false;
        }

        return true;
    }

    public void CalculateTracker()
    {
        bool nullColliders = MarrowBody.Colliders.Any((c) => c == null);

        if (!nullColliders && MarrowBody.HasRigidbody)
        {
            MarrowBody.CalculateBounds();
        }

        // Bounds is scaled, while _bounds is unscaled
        var unscaledBounds = MarrowBody._bounds;

        Tracker.SetBounds(unscaledBounds);
    }

    public Vector3 GetAnchor()
    {
        return Tracker.transform.TransformPoint(Tracker.TrackerCollider.center);
    }

    public void Pack(TeleportableBody hostBody)
    {
        // Unpack from existing host body
        if (IsPacked)
        {
            Unpack();
        }

        hostBody.Teleportable.HookBodyPortal(this);
        Teleportable.HookBodyPortal(hostBody);

        hostBody.ParasiteBodies.Add(this);

        HostBody = hostBody;
        IsPacked = true;
    }

    public void Unpack()
    {
        if (!IsPacked)
        {
            return;
        }

        HostBody.Teleportable.UnHookBodyPortal(this);
        Teleportable.UnHookBodyPortal(HostBody);

        HostBody.ParasiteBodies.Remove(this);

        HostBody = null;
        IsPacked = false;
    }

    private readonly List<Collider> _ignoredColliders = new();

    public void IgnoreCollision(Portal portal, bool ignore = true)
    {
        foreach (var collider in portal.WallColliders)
        {
            if (ignore)
            {
                _ignoredColliders.Add(collider);
            }
            else
            {
                _ignoredColliders.Remove(collider);
            }

            MarrowBody.IgnoreCollision(collider, ignore);
        }
    }

    public void ResetCollision()
    {
        foreach (var collider in _ignoredColliders)
        {
            if (collider == null)
            {
                continue;
            }

            MarrowBody.IgnoreCollision(collider, false);
        }

        _ignoredColliders.Clear();
    }

    public bool IsGrabbed()
    {
        return IsGrabbed(0);
    }

    private bool IsGrabbed(int depth)
    {
        if (!HasHost)
        {
            return false;
        }

        if (Host.HandCount() > 0)
        {
            return true;
        }

        if (depth < 4)
        {
            foreach (var parasite in ParasiteBodies)
            {
                if (parasite.IsGrabbed(depth + 1))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
