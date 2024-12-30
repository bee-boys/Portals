using System;

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
            var previous = CurrentPortal;

            _triggeredPortal = value;

            CheckPortalChange(previous, CurrentPortal);
        }
    }

    private Portal _triggeredPortal = null;

    [HideFromIl2Cpp]
    public event Action<TeleportableBody, Portal> OnPortalEnterEvent, OnPortalExitEvent;

    private void Awake()
    {
        _marrowBody = GetComponent<MarrowBody>();
        _host = GetComponent<InteractableHost>();
        _hasHost = _host != null;

        CreateTracker();
    }

    private void OnDisable()
    {
        OverridePortal = null;
        TriggeredPortal = null;
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
}
