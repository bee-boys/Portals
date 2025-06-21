using Il2CppInterop.Runtime.Attributes;

using System;
using System.Linq;

using UnityEngine;

using MelonLoader;

using Portals.Pool;

using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Interaction;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class PortalGun : MonoBehaviour
{
    public PortalGun(IntPtr intPtr) : base(intPtr) { }

    public delegate bool FireCallback(PortalGun gun, bool primary, Vector2 size);

    public static event FireCallback OnFireEvent;

    #region PROPERTIES
    [HideFromIl2Cpp]
    public Color PrimaryOutsideColor { get; set; }

    [HideFromIl2Cpp]
    public Color PrimaryInsideColor { get; set; }

    [HideFromIl2Cpp]
    public Color SecondaryOutsideColor { get; set; }

    [HideFromIl2Cpp]
    public Color SecondaryInsideColor { get; set; }

    [HideFromIl2Cpp]
    public PortalShape PortalShape { get; set; }

    [HideFromIl2Cpp]
    public int PortalID { get; set; }

    [HideFromIl2Cpp]
    public Transform FirePoint { get; set; }

    [HideFromIl2Cpp]
    public AudioClip[] PrimaryOpenSounds { get; set; } = Array.Empty<AudioClip>();

    [HideFromIl2Cpp]
    public AudioClip[] SecondaryOpenSounds { get; set; } = Array.Empty<AudioClip>();

    [HideFromIl2Cpp]
    public AudioClip[] FizzleSounds { get; set; } = Array.Empty<AudioClip>();

    [HideFromIl2Cpp]
    public AudioClip[] InvalidSounds { get; set; } = Array.Empty<AudioClip>();

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
            ID = PortalID,
        });
    }

    public void Fire(bool primary) => Fire(primary, PortalConstants.DefaultSize);

    public void Fire(bool primary, float scale) => Fire(primary, PortalConstants.DefaultSize * scale);

    public void Fire(bool primary, Vector2 size)
    {
        if (OnFireEvent != null)
        {
            bool fire = OnFireEvent(this, primary, size);

            if (!fire)
            {
                return;
            }
        }

        var direction = FirePoint.forward;
        var velocity = direction * 50f;

        if (MarrowBody && MarrowBody.HasRigidbody)
        {
            velocity += MarrowBody._rigidbody.velocity;
        }

        PortalEffectSpawner.ShootProjectile(new PortalProjectile.ProjectileData()
        {
            Color = primary ? PrimaryInsideColor : SecondaryInsideColor,
            Velocity = velocity,
            Direction = direction,
            MaxTime = 3f,
            Position = FirePoint.position,
            InvalidSounds = InvalidSounds,

            Size = size,
            Shape = PortalShape,
            Primary = primary,
            ID = PortalID,
            Origin = this,
        });
    }
    
    public void RegisterPortal(Portal portal, bool primary)
    {
        var outsideColor = primary ? PrimaryOutsideColor : SecondaryOutsideColor;
        var insideColor = primary ? PrimaryInsideColor : SecondaryInsideColor;

        portal.CollectWallColliders();

        var portalPosition = portal.transform.position;

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
    #endregion

    #region UNITY
    private void Awake()
    {
        MarrowBody = GetComponent<MarrowBody>();
    }
    #endregion

    #region INJECTION
    public void SetColors(Color primaryOutsideColor, Color primaryInsideColor, Color secondaryOutsideColor, Color secondaryInsideColor)
    {
        PrimaryOutsideColor = primaryOutsideColor;
        PrimaryInsideColor = primaryInsideColor;
        SecondaryOutsideColor = secondaryOutsideColor;
        SecondaryInsideColor = secondaryInsideColor;
    }

    public void SetPortalShape(int portalShape)
    {
        PortalShape = (PortalShape)portalShape;
    }

    public void SetPortalID(int portalID)
    {
        PortalID = portalID;
    }

    public void SetFirePoint(Transform firePoint)
    {
        FirePoint = firePoint;
    }

    public void AddPrimaryOpenSound(AudioClip audioClip) 
    {
        var sounds = PrimaryOpenSounds.ToList();
        sounds.Add(audioClip);
        PrimaryOpenSounds = sounds.ToArray();
    }

    public void AddSecondaryOpenSound(AudioClip audioClip) 
    {
        var sounds = SecondaryOpenSounds.ToList();
        sounds.Add(audioClip);
        SecondaryOpenSounds = sounds.ToArray();
    }

    public void AddFizzleSound(AudioClip audioClip) 
    {
        var sounds = FizzleSounds.ToList();
        sounds.Add(audioClip);
        FizzleSounds = sounds.ToArray();
    }

    public void AddInvalidSound(AudioClip audioClip) 
    {
        var sounds = InvalidSounds.ToList();
        sounds.Add(audioClip);
        InvalidSounds = sounds.ToArray();
    }
    #endregion
}
