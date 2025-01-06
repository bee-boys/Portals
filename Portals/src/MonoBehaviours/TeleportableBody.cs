using System;
using System.Collections.Generic;

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

            OnTriggeredPortalChanged(previousTriggered, currentTriggered);
        }
    }

    private Portal _triggeredPortal = null;

    [HideFromIl2Cpp]
    public event Action<TeleportableBody, Portal> OnPortalEnterEvent, OnPortalExitEvent;

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

    private void OnTriggeredPortalChanged(Portal previous, Portal current)
    {
        if (previous && !Teleportable.PassedThrough(Teleportable.EnterSign, Teleportable.GetPortalSign(previous, GetAnchor())))
        {
            IgnoreCollision(previous, false);
        }

        if (current)
        {
            IgnoreCollision(current, true);
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

        if (portal != null)
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

    public void CalculateTracker()
    {
        MarrowBody.CalculateBounds();

        // Bounds is scaled, while _bounds is unscaled
        var unscaledBounds = MarrowBody._bounds;

        Tracker.SetBounds(unscaledBounds);
    }

    public Vector3 GetAnchor()
    {
        return MarrowBody.CenterOfMassInWorld;
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

    public void CheckCollision(Portal portal)
    {
        if (Intersects(portal))
        {
            IgnoreCollision(portal, true);
        }
    }

    public bool Intersects(Portal portal)
    {
        if (Teleportable.PassedThrough(Teleportable.EnterSign, Teleportable.GetPortalSign(portal, GetAnchor())))
        {
            return true;
        }

        var portalTransform = portal.transform;
        var trackerTransform = Tracker.transform;

        var portalBounds = CalculateAABB(portalTransform.position, new Vector3(portal.Size.x, portal.Size.y, 0.05f), portalTransform);
        var bodyBounds = CalculateAABB(trackerTransform.TransformPoint(Tracker.TrackerCollider.center), Tracker.TrackerCollider.size, trackerTransform);

        var bodyAnchor = portalTransform.InverseTransformDirection(GetAnchor());
        var rootAnchor = portalTransform.InverseTransformDirection(Teleportable.GetAnchor());

        rootAnchor.x = bodyAnchor.x;
        rootAnchor.y = bodyAnchor.y;

        bodyBounds.Encapsulate(portalTransform.TransformDirection(rootAnchor));

        return bodyBounds.Intersects(portalBounds);
    }

    private static Bounds CalculateAABB(Vector3 center, Vector3 size, Transform transform)
    {
        var min = size * -0.5f;
        var max = size * 0.5f;

        var c1 = transform.TransformPoint(min);
        var c2 = transform.TransformPoint(new Vector3(min.x, min.y, max.z));
        var c3 = transform.TransformPoint(new Vector3(min.x, max.y, min.z));
        var c4 = transform.TransformPoint(new Vector3(min.x, max.y, max.z));
        var c5 = transform.TransformPoint(new Vector3(max.x, min.y, min.z));
        var c6 = transform.TransformPoint(new Vector3(max.x, min.y, max.z));
        var c7 = transform.TransformPoint(new Vector3(max.x, max.y, min.z));
        var c8 = transform.TransformPoint(max);

        float minX = Mathf.Min(c1.x, c2.x, c3.x, c4.x, c5.x, c6.x, c7.x, c8.x);
        float minY = Mathf.Min(c1.y, c2.y, c3.y, c4.y, c5.y, c6.y, c7.y, c8.y);
        float minZ = Mathf.Min(c1.z, c2.z, c3.z, c4.z, c5.z, c6.z, c7.z, c8.z);

        float maxX = Mathf.Max(c1.x, c2.x, c3.x, c4.x, c5.x, c6.x, c7.x, c8.x);
        float maxY = Mathf.Max(c1.y, c2.y, c3.y, c4.y, c5.y, c6.y, c7.y, c8.y);
        float maxZ = Mathf.Max(c1.z, c2.z, c3.z, c4.z, c5.z, c6.z, c7.z, c8.z);

        var worldMin = new Vector3(minX, minY, minZ);
        var worldMax = new Vector3(maxX, maxY, maxZ);

        return new Bounds(center, worldMax - worldMin);
    }
}
