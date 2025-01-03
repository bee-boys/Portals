using Il2CppInterop.Runtime.InteropTypes.Fields;

using System;

using UnityEngine;

using MelonLoader;

using Portals.Pooling;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSLZ.Marrow.Pool;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class PortalGun : MonoBehaviour
{
    public PortalGun(IntPtr intPtr) : base(intPtr) { }

    #region FIELD INJECTION
    public Il2CppValueField<Color> primaryOutsideColor;

    public Il2CppValueField<Color> primaryInsideColor;

    public Il2CppValueField<Color> secondaryOutsideColor;

    public Il2CppValueField<Color> secondaryInsideColor;

    public Il2CppValueField<int> portalShape;

    public Il2CppReferenceField<Transform> firePoint;
    #endregion

    #region PROPERTIES
    [HideFromIl2Cpp]
    public Color PrimaryOutsideColor => primaryOutsideColor.Get();

    [HideFromIl2Cpp]
    public Color PrimaryInsideColor => primaryInsideColor.Get();

    [HideFromIl2Cpp]
    public Color SecondaryOutsideColor => secondaryOutsideColor.Get();

    [HideFromIl2Cpp]
    public Color SecondaryInsideColor => secondaryInsideColor.Get();

    [HideFromIl2Cpp]
    public PortalShape PortalShape => (PortalShape)portalShape.Get();

    [HideFromIl2Cpp]
    public Transform FirePoint => firePoint.Get();
    #endregion

    private Portal _primaryPortal = null;
    private Portal _secondaryPortal = null;

    #region METHODS
    public void Fire(bool primary) => Fire(primary, new Vector2(1f, 2f));

    public void Fire(bool primary, Vector2 size)
    {
        var ray = Physics.Raycast(FirePoint.position, FirePoint.forward, out var hitInfo);

        if (!ray)
        {
            return;
        }

        if (hitInfo.collider.attachedRigidbody)
        {
            return;
        }

        var position = hitInfo.point + hitInfo.normal * 0.001f;

        var up = Vector3.up;

        float dot = Vector3.Dot(hitInfo.normal, up);

        if (dot > 0.95f || dot < -0.95f)
        {
            up = FirePoint.up;
        }

        var rotation = Quaternion.LookRotation(hitInfo.normal, up);

        if (primary && _primaryPortal)
        {
            _primaryPortal.GetComponent<Poolee>().Despawn();
            _primaryPortal = null;
        }
        else if (!primary && _secondaryPortal)
        {
            _secondaryPortal.GetComponent<Poolee>().Despawn();
            _secondaryPortal = null;
        }

        PortalSpawner.Spawn(position, rotation, size, PortalShape, (portal) =>
        {
            portal.WallColliders.Clear();

            var overlapBox = Physics.OverlapBox(position, new Vector3(size.x * 0.5f, size.y * 0.5f, 2f), rotation, ~0, QueryTriggerInteraction.Ignore);

            foreach (var hit in overlapBox)
            {
                if (hit.attachedRigidbody)
                {
                    continue;
                }

                if (hit.GetComponentInParent<Portal>())
                {
                    continue;
                }

                portal.WallColliders.Add(hit);
            }

            if (primary)
            {
                portal.Surface.SetOutline(PrimaryOutsideColor);
                portal.Surface.SetInside(PrimaryInsideColor);

                _primaryPortal = portal;
            }
            else
            {
                portal.Surface.SetOutline(SecondaryOutsideColor);
                portal.Surface.SetInside(SecondaryInsideColor);

                _secondaryPortal = portal;
            }

            if (_primaryPortal && _secondaryPortal)
            {
                _primaryPortal.OtherPortal = _secondaryPortal;
                _secondaryPortal.OtherPortal = _primaryPortal;
            }
        });
    }
    #endregion

    #region UNITY

    #endregion
}
