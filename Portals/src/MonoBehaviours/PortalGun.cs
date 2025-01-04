using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using System;

using UnityEngine;

using MelonLoader;

using Portals.Pooling;

using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Audio;

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

    public Il2CppReferenceField<Il2CppReferenceArray<AudioClip>> primaryOpenSounds;

    public Il2CppReferenceField<Il2CppReferenceArray<AudioClip>> secondaryOpenSounds;
    #endregion

    #region FIELDS
    private AudioClip[] _primaryOpenSounds = null;
    private AudioClip[] _secondaryOpenSounds = null;
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

    [HideFromIl2Cpp]
    public AudioClip[] PrimaryOpenSounds => _primaryOpenSounds;

    [HideFromIl2Cpp]
    public AudioClip[] SecondaryOpenSounds => _secondaryOpenSounds;
    #endregion

    private Portal _primaryPortal = null;
    private Portal _secondaryPortal = null;

    #region METHODS
    public void Fire(bool primary) => Fire(primary, new Vector2(0.9f, 1.8f));

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

        var normal = hitInfo.normal;
        var uphill = Vector3.Cross(normal, Vector3.Cross(normal, Vector3.down));

        var position = hitInfo.point + normal * 0.001f;

        var up = uphill;

        float dot = Vector3.Dot(normal, Vector3.up);

        if (dot > 0.99f || dot < -0.99f)
        {
            up = (hitInfo.point - FirePoint.position).normalized;
        }

        var rotation = Quaternion.LookRotation(normal, up);

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

            var openSounds = primary ? PrimaryOpenSounds : SecondaryOpenSounds;

            if (openSounds != null)
            {
                Audio3dManager.PlayAtPoint(openSounds, position, Audio3dManager.hardInteraction, 0.4f, 1f, new(0f), new(0.4f), new(1f));
            }
        });
    }
    #endregion

    #region UNITY
    private void Awake()
    {
        var primaryOpenSounds = this.primaryOpenSounds.Get();

        if (primaryOpenSounds != null)
        {
            _primaryOpenSounds = primaryOpenSounds;
        }

        var secondaryOpenSounds = this.secondaryOpenSounds.Get();

        if (secondaryOpenSounds != null)
        {
            _secondaryOpenSounds = secondaryOpenSounds;
        }
    }
    #endregion
}
