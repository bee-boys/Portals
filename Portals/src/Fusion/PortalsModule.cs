using Il2CppSLZ.Marrow;
using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;

using Portals.MonoBehaviours;

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

        PortalGun.OnFireEvent += OnPortalGunFired;
        TeleportableRigManager.OnScaleEvent += OnRigManagerScaled;
    }

    protected override void OnModuleUnregistered()
    {
        base.OnModuleUnregistered();

        PortalGun.OnFireEvent -= OnPortalGunFired;
        TeleportableRigManager.OnScaleEvent -= OnRigManagerScaled;
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
            var writer = FusionWriter.Create();
            var data = PortalGunFireData.Create(PlayerIdManager.LocalSmallId, networkEntity.Id, primary, size);

            writer.Write(data);

            var message = FusionMessage.ModuleCreate<PortalGunFireMessage>(writer);

            MessageSender.SendToServer(NetworkChannel.Reliable, message);

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
