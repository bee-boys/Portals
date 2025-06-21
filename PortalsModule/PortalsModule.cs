using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.Scene;
using LabFusion.SDK.Modules;

using Portals;
using Portals.MonoBehaviours;
using Portals.Pool;

using System.Reflection;

using UnityEngine;

using Module = LabFusion.SDK.Modules.Module;

namespace PortalsModule;

public class PortalsModule : Module
{
    public override string Name => "Portals";

    public override ConsoleColor Color => ConsoleColor.Cyan;

    public override string Author => "Lakatrazz";

    public override Version Version => new(PortalsMod.Version);

    public static bool IgnoreOverrides { get; set; } = false;

    public static Assembly? ModuleAssembly { get; private set; } = null;

    protected override void OnModuleRegistered()
    {
        ModuleAssembly = Assembly.GetExecutingAssembly();

        EntityComponentManager.LoadComponents(ModuleAssembly);
        ModuleMessageManager.LoadHandlers(ModuleAssembly);

        PortalGun.OnFireEvent += OnPortalGunFired;

        Teleportable.OnTryTeleportEvent += OnTryTeleport;
        Teleportable.OnBeforeTeleportEvent += OnBeforeTeleport;

        TeleportableRigManager.OnScaleEvent += OnRigManagerScaled;

        PortalProjectile.OnCheckHit += OnCheckProjectileHit;
        PortalProjectile.OnProjectileHit += OnProjectileHit;
    }

    protected override void OnModuleUnregistered()
    {
        PortalGun.OnFireEvent -= OnPortalGunFired;

        Teleportable.OnTryTeleportEvent -= OnTryTeleport;
        Teleportable.OnBeforeTeleportEvent -= OnBeforeTeleport;

        TeleportableRigManager.OnScaleEvent -= OnRigManagerScaled;

        PortalProjectile.OnCheckHit -= OnCheckProjectileHit;
        PortalProjectile.OnProjectileHit -= OnProjectileHit;
    }


    private void OnProjectileHit(PortalGun origin, PortalSpawner.PortalSpawnInfo spawnInfo)
    {
        if (!NetworkSceneManager.IsLevelNetworked)
        {
            return;
        }

        if (PortalPreferences.DisableInFusion)
        {
            return;
        }

        if (origin == null)
        {
            return;
        }

        var networkEntity = PortalGunExtender.Cache.Get(origin);

        if (networkEntity == null)
        {
            return;
        }

        if (!networkEntity.IsOwner)
        {
            return;
        }

        var data = PortalSpawnData.Create(PlayerIDManager.LocalSmallID, networkEntity.ID, spawnInfo);

        MessageRelay.RelayModule<PortalSpawnMessage, PortalSpawnData>(data, CommonMessageRoutes.ReliableToOtherClients);
    }

    private bool OnCheckProjectileHit(PortalProjectile projectile)
    {
        if (!NetworkSceneManager.IsLevelNetworked)
        {
            return true;
        }

        if (PortalPreferences.DisableInFusion)
        {
            return false;
        }

        if (projectile.Data.Origin == null)
        {
            return true;
        }

        // Only let the owner of the gun spawn portals
        var networkEntity = PortalGunExtender.Cache.Get(projectile.Data.Origin);

        if (networkEntity == null)
        {
            return true;
        }

        if (!networkEntity.IsOwner)
        {
            return false;
        }

        return true;
    }

    private bool OnPortalGunFired(PortalGun gun, bool primary, Vector2 size)
    {
        if (IgnoreOverrides)
        {
            return true;
        }

        if (!NetworkSceneManager.IsLevelNetworked)
        {
            return true;
        }

        if (PortalPreferences.DisableInFusion)
        {
            return false;
        }

        var networkEntity = PortalGunExtender.Cache.Get(gun);

        if (networkEntity == null)
        {
            return true;
        }

        if (networkEntity.IsOwner)
        {
            var data = new PortalGunFireData()
            {
                PlayerID = PlayerIDManager.LocalSmallID,
                Entity = new(networkEntity),
                Primary = primary,
                Dimensions = size
            };

            MessageRelay.RelayModule<PortalGunFireMessage, PortalGunFireData>(data, CommonMessageRoutes.ReliableToOtherClients);

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool OnTryTeleport(Teleportable teleportable, Portal inPortal, Portal outPortal)
    {
        if (!NetworkSceneManager.IsLevelNetworked)
        {
            return true;
        }

        var networkEntity = TeleportableExtender.Cache.Get(teleportable);

        if (networkEntity == null)
        {
            return true;
        }

        if (networkEntity.IsOwner)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnBeforeTeleport(Teleportable teleportable, Portal inPortal, Portal outPortal)
    {
        if (!NetworkSceneManager.IsLevelNetworked)
        {
            return;
        }

        var networkEntity = TeleportableExtender.Cache.Get(teleportable);

        if (networkEntity == null)
        {
            return;
        }

        if (!networkEntity.IsOwner)
        {
            return;
        }

        var data = new TeleportableTeleportData()
        {
            Entity = new(networkEntity),
        };

        MessageRelay.RelayModule<TeleportableTeleportMessage, TeleportableTeleportData>(data, CommonMessageRoutes.ReliableToOtherClients);
    }

    private bool OnRigManagerScaled(TeleportableRigManager teleportable, float scale)
    {
        if (IgnoreOverrides)
        {
            return true;
        }

        if (!NetworkSceneManager.IsLevelNetworked)
        {
            return true;
        }

        var networkEntity = IMarrowEntityExtender.Cache.Get(teleportable.MarrowEntity);

        if (networkEntity == null)
        {
            return true;
        }

        // Only scale if we own this RigManager
        if (networkEntity.IsOwner)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
