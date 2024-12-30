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

    public Portal TriggeredPortal => _triggeredPortal;

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
        if (TriggeredPortal)
        {
            OnPortalExitEvent?.Invoke(this, TriggeredPortal);

            _triggeredPortal = null;
        }
    }

    private void CreateTracker()
    {
        GameObject trackerGameObject = new GameObject("Teleportable Tracker");
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
        if (TriggeredPortal)
        {
            return;
        }

        if (!HasRigidbody)
        {
            return;
        }

        var portal = other.GetComponentInParent<Portal>();

        if (portal != null)
        {
            _triggeredPortal = portal;

            OnPortalEnterEvent?.Invoke(this, portal);
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
            _triggeredPortal = null;

            OnPortalExitEvent?.Invoke(this, portal);
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
