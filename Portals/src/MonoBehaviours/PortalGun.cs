using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using System;

using UnityEngine;

using MelonLoader;

using Portals.Pooling;

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

    public Il2CppValueField<int> portalId;

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
    public int PortalId => portalId.Get();

    [HideFromIl2Cpp]
    public Transform FirePoint => firePoint.Get();

    [HideFromIl2Cpp]
    public AudioClip[] PrimaryOpenSounds => _primaryOpenSounds;

    [HideFromIl2Cpp]
    public AudioClip[] SecondaryOpenSounds => _secondaryOpenSounds;
    #endregion

    #region METHODS
    public void Fire(bool primary) => Fire(primary, PortalConstants.DefaultSize);

    public void Fire(bool primary, float scale) => Fire(primary, PortalConstants.DefaultSize * scale);

    public void Fire(bool primary, Vector2 size)
    {
        var hits = Physics.RaycastAll(FirePoint.position, FirePoint.forward, float.PositiveInfinity, ~0, QueryTriggerInteraction.Ignore);

        RaycastHit hitInfo = default;
        bool foundRay = false;

        foreach (var hit in hits)
        {
            if (hit.rigidbody)
            {
                continue;
            }

            if (hit.collider.GetComponentInParent<Portal>())
            {
                continue;
            }

            // Get the closest hit
            if (foundRay && hit.distance >= hitInfo.distance)
            {
                continue;
            }

            hitInfo = hit;
            foundRay = true;
        }

        if (!foundRay)
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

        var spawnInfo = new PortalSpawner.PortalSpawnInfo()
        {
            Position = position,
            Rotation = rotation,
            Size = size,
            Shape = PortalShape,

            Primary = primary,
            ID = PortalId,

            SpawnCallback = OnPortalSpawned,
        };

        PortalSpawner.Spawn(spawnInfo);

        void OnPortalSpawned(Portal portal)
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
            }
            else
            {
                portal.Surface.SetOutline(SecondaryOutsideColor);
                portal.Surface.SetInside(SecondaryInsideColor);
            }

            portal.Expander.Expand();

            var openSounds = primary ? PrimaryOpenSounds : SecondaryOpenSounds;

            if (openSounds != null)
            {
                Audio3dManager.PlayAtPoint(openSounds, position, Audio3dManager.hardInteraction, 0.4f, 1f, new(0f), new(0.4f), new(1f));
            }
        }
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
