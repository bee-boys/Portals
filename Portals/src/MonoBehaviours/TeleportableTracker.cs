using System;

using Il2CppInterop.Runtime.Attributes;

using MelonLoader;

using UnityEngine;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class TeleportableTracker : MonoBehaviour
{
    public TeleportableTracker(IntPtr intPtr) : base(intPtr) { }

    [HideFromIl2Cpp]
    public event Action<Collider> OnTriggerEnterEvent, OnTriggerExitEvent;

    public BoxCollider TrackerCollider => _trackerCollider;
    private BoxCollider _trackerCollider = null;

    private void Awake()
    {
        gameObject.layer = PortalConstants.TrackerLayer;

        var kinematicRb = gameObject.AddComponent<Rigidbody>();
        kinematicRb.isKinematic = true;
        kinematicRb.useGravity = false;

        _trackerCollider = gameObject.AddComponent<BoxCollider>();
        _trackerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger)
        {
            return;
        }

        OnTriggerEnterEvent?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger)
        {
            return;
        }

        OnTriggerExitEvent?.Invoke(other);
    }

    public void SetBounds(Bounds bounds)
    {
        _trackerCollider.center = bounds.center;
        _trackerCollider.size = bounds.size;
    }
}
