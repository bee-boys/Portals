using System;

using Il2CppInterop.Runtime.InteropTypes.Fields;

using Il2CppSLZ.Marrow.Interaction;

using MelonLoader;

using UnityEngine;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class PortalExpander : MonoBehaviour
{
    public PortalExpander(IntPtr intPtr) : base(intPtr) { }

    #region FIELD INJECTION
    public Il2CppReferenceField<BoxCollider> rightCollider;
    public Il2CppReferenceField<BoxCollider> topCollider;
    public Il2CppReferenceField<BoxCollider> leftCollider;
    public Il2CppReferenceField<BoxCollider> bottomCollider;
    #endregion

    #region PROPERTIES
    public BoxCollider RightCollider => rightCollider.Get();

    public BoxCollider TopCollider => topCollider.Get();

    public BoxCollider LeftCollider => leftCollider.Get();

    public BoxCollider BottomCollider => bottomCollider.Get();
    #endregion

    #region METHODS
    public void ToggleCollision(bool enabled)
    {
        RightCollider.enabled = enabled;
        TopCollider.enabled = enabled;
        LeftCollider.enabled = enabled;
        BottomCollider.enabled = enabled;
    }

    public void Expand()
    {
        ToggleCollision(false);

        Expand(RightCollider);
        Expand(TopCollider);
        Expand(LeftCollider);
        Expand(BottomCollider);

        ToggleCollision(true);
    }

    private void Expand(BoxCollider collider)
    {
        var start = collider.transform.position;
        var direction = collider.transform.right;

        var normal = transform.forward;

        Vector3 position = start;

        for (var i = 0; i < 30; i++)
        {
            var newPosition = position + direction * 0.1f;

            var above = newPosition + normal * 0.1f;
            var below = newPosition - normal * 0.1f;

            if (!Physics.Linecast(above, below, ~0, QueryTriggerInteraction.Ignore))
            {
                break;
            }

            position = newPosition;
        }

        var vector = collider.transform.InverseTransformVector(position - start);
        float distance = vector.magnitude;

        var size = collider.size;
        size.x = distance;
        size.y = 1f + (distance * 2f);

        var center = collider.center;
        center.x = distance * 0.5f;

        collider.size = size;
        collider.center = center;
    }

    public void IgnoreCollision(Teleportable teleportable, bool ignore = true)
    {
        foreach (var body in teleportable.Bodies)
        {
            body.MarrowBody.IgnoreCollision(RightCollider, ignore);
            body.MarrowBody.IgnoreCollision(TopCollider, ignore);
            body.MarrowBody.IgnoreCollision(LeftCollider, ignore);
            body.MarrowBody.IgnoreCollision(BottomCollider, ignore);
        }
    }

    private void OnTeleportableCreated(Teleportable teleportable)
    {
        IgnoreCollision(teleportable, true);
    }

    private void IgnoreExistingTeleportables()
    {
        foreach (var teleportable in Teleportable.Teleportables)
        {
            IgnoreCollision(teleportable, true);
        }
    }

    private void OverrideLayers()
    {
        var layer = (int)MarrowLayers.Football;

        // I'd rather not override this, but this is the closest layer to what I want that can't get step detected
        // Also it doesn't really affect anything as far as I can tell, since the feet and knee joint disables collision
        Physics.IgnoreLayerCollision(layer, layer, false);

        // Football layer is used to prevent the expanded colliders from blocking force grabs and causing stepping
        RightCollider.gameObject.layer = layer;
        TopCollider.gameObject.layer = layer;
        LeftCollider.gameObject.layer = layer;
        BottomCollider.gameObject.layer = layer;
    }
    #endregion

    #region UNITY
    private void Awake()
    {
        OverrideLayers();

        Teleportable.OnTeleportableCreated += OnTeleportableCreated;

        IgnoreExistingTeleportables();
    }

    private void OnDestroy()
    {
        Teleportable.OnTeleportableCreated -= OnTeleportableCreated;
    }
    #endregion
}
