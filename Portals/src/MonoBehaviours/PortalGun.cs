using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using System;

using UnityEngine;

using MelonLoader;

using Portals.Pooling;

using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Interaction;

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

    public Il2CppReferenceField<Il2CppReferenceArray<AudioClip>> fizzleSounds;

    public Il2CppReferenceField<Il2CppReferenceArray<AudioClip>> invalidSounds;
    #endregion

    #region FIELDS
    private AudioClip[] _primaryOpenSounds = null;
    private AudioClip[] _secondaryOpenSounds = null;
    private AudioClip[] _fizzleSounds = null;
    private AudioClip[] _invalidSounds = null;
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

    [HideFromIl2Cpp]
    public AudioClip[] FizzleSounds => _fizzleSounds;

    [HideFromIl2Cpp]
    public AudioClip[] InvalidSounds => _invalidSounds;

    [HideFromIl2Cpp]
    public MarrowBody MarrowBody { get; set; } = null;
    #endregion

    #region METHODS
    public void Fizzle()
    {
        PortalSpawner.Fizzle(new PortalSpawner.PortalFizzleInfo()
        {
            Primary = true,
            Secondary = true,
            ID = PortalId,
        });
    }

    public void Fire(bool primary) => Fire(primary, PortalConstants.DefaultSize);

    public void Fire(bool primary, float scale) => Fire(primary, PortalConstants.DefaultSize * scale);

    public void Fire(bool primary, Vector2 size)
    {
        var direction = FirePoint.forward;
        var velocity = direction * 50f;

        var outsideColor = primary ? PrimaryOutsideColor : SecondaryOutsideColor;
        var insideColor = primary ? PrimaryInsideColor : SecondaryInsideColor;

        if (MarrowBody && MarrowBody.HasRigidbody)
        {
            velocity += MarrowBody._rigidbody.velocity;
        }

        var spawnInfo = new PortalSpawner.PortalSpawnInfo()
        {
            Size = size,
            Shape = PortalShape,

            Primary = primary,
            ID = PortalId,

            SpawnCallback = OnPortalSpawned,
        };

        PortalEffectSpawner.ShootProjectile(new PortalProjectile.ProjectileData()
        {
            SpawnInfo = spawnInfo,
            Color = insideColor,
            Velocity = velocity,
            Direction = direction,
            MaxTime = 3f,
            Position = FirePoint.position,
            InvalidSounds = InvalidSounds,
        });

        void OnPortalSpawned(Portal portal)
        {
            portal.WallColliders.Clear();

            var portalPosition = portal.transform.position;
            var portalRotation = portal.transform.rotation;

            var overlapBox = Physics.OverlapBox(portalPosition, new Vector3(size.x * 0.5f, size.y * 0.5f, 2f), portalRotation, ~0, QueryTriggerInteraction.Ignore);

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

            portal.Surface.SetOutline(outsideColor);
            portal.Surface.SetInside(insideColor);

            portal.Expander.Expand();

            portal.FizzleSounds = FizzleSounds;

            var openSounds = primary ? PrimaryOpenSounds : SecondaryOpenSounds;

            if (openSounds != null)
            {
                Audio3dManager.PlayAtPoint(openSounds, portalPosition, Audio3dManager.hardInteraction, 0.4f, 1f, new(0f), new(0.4f), new(1f));
            }
        }
    }
    #endregion

    #region UNITY
    private void Awake()
    {
        MarrowBody = GetComponent<MarrowBody>();

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

        var fizzleSounds = this.fizzleSounds.Get();

        if (fizzleSounds != null)
        {
            _fizzleSounds = fizzleSounds;
        }

        var invalidSounds = this.invalidSounds.Get();

        if (invalidSounds != null)
        {
            _invalidSounds = invalidSounds;
        }
    }
    #endregion
}
