using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

using Portals.MonoBehaviours;
using Portals.Pooling;

using System;

using UnityEngine;

namespace Portals.Fusion;

public class PortalsModule : Module
{
    public override string Name => "Portals";

    public override ConsoleColor Color => ConsoleColor.Cyan;

    public override string Author => "Lakatrazz";

    public override Version Version => new(PortalsMod.Version);

    public static bool IgnoreOverrides { get; set; } = false;

    protected override void OnModuleRegistered()
    {
        base.OnModuleRegistered();

        EntityComponentManager.RegisterComponent<PortalGunExtender>();
        ModuleMessageHandler.RegisterHandler<PortalGunFireMessage>();
        ModuleMessageHandler.RegisterHandler<PortalSpawnMessage>();

        PortalGun.OnFireEvent += OnPortalGunFired;
        TeleportableRigManager.OnScaleEvent += OnRigManagerScaled;

        PortalProjectile.OnCheckHit += OnCheckProjectileHit;
        PortalProjectile.OnProjectileHit += OnProjectileHit;
    }

    protected override void OnModuleUnregistered()
    {
        base.OnModuleUnregistered();

        PortalGun.OnFireEvent -= OnPortalGunFired;
        TeleportableRigManager.OnScaleEvent -= OnRigManagerScaled;

        PortalProjectile.OnCheckHit -= OnCheckProjectileHit;
        PortalProjectile.OnProjectileHit -= OnProjectileHit;
    }


    private void OnProjectileHit(PortalGun origin, PortalSpawner.PortalSpawnInfo spawnInfo)
    {
        if (!NetworkInfo.HasServer)
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

        var data = PortalSpawnData.Create(PlayerIdManager.LocalSmallId, networkEntity.Id, spawnInfo);

        MessageRelay.RelayModule<PortalSpawnMessage, PortalSpawnData>(data, NetworkChannel.Reliable, RelayType.ToOtherClients);
    }

    private bool OnCheckProjectileHit(PortalProjectile projectile)
    {
        if (!NetworkInfo.HasServer)
        {
            return true;
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

        if (!NetworkInfo.HasServer)
        {
            return true;
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
                playerId = PlayerIdManager.LocalSmallId,
                entityId = networkEntity.Id,
                primary = primary,
                size = size
            };

            MessageRelay.RelayModule<PortalGunFireMessage, PortalGunFireData>(data, NetworkChannel.Reliable, RelayType.ToOtherClients);

            return true;
        }
        else
        {
            return false;
        }
    }


    private bool OnRigManagerScaled(TeleportableRigManager teleportable, float scale)
    {
        if (IgnoreOverrides)
        {
            return true;
        }

        if (!NetworkInfo.HasServer)
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
