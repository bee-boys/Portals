using LabFusion.Data;
using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.SDK.Modules;

using UnityEngine;

namespace Portals.Fusion;

public class PortalGunFireData : IFusionSerializable
{
    public byte playerId;

    public ushort entityId;

    public bool primary;

    public Vector2 size;

    public static PortalGunFireData Create(byte playerId, ushort entityId, bool primary, Vector2 size)
    {
        return new PortalGunFireData()
        {
            playerId = playerId,
            entityId = entityId,
            primary = primary,
            size = size,
        };
    }

    public void Serialize(FusionWriter writer)
    {
        writer.Write(playerId);
        writer.Write(entityId);
        writer.Write(primary);
        writer.Write(size);
    }

    public void Deserialize(FusionReader reader)
    {
        playerId = reader.ReadByte();
        entityId = reader.ReadUInt16();
        primary = reader.ReadBoolean();
        size = reader.ReadVector2();
    }
}

public class PortalGunFireMessage : ModuleMessageHandler
{
    public override void HandleMessage(byte[] bytes, bool isServerHandled = false)
    {
        using var reader = FusionReader.Create(bytes);

        var data = reader.ReadFusionSerializable<PortalGunFireData>();

        if (isServerHandled)
        {
            MessageSender.BroadcastMessageExcept(data.playerId, NetworkChannel.Reliable, FusionMessage.ModuleCreate<PortalGunFireMessage>(bytes), false);
            return;
        }

        var networkEntity = NetworkEntityManager.IdManager.RegisteredEntities.GetEntity(data.entityId);

        if (networkEntity == null)
        {
            return;
        }

        var extender = networkEntity.GetExtender<PortalGunExtender>();

        if (extender == null)
        {
            return;
        }

        var gun = extender.Component;

        PortalsModule.IgnoreOverrides = true;

        gun.Fire(data.primary, data.size);

        PortalsModule.IgnoreOverrides = false;
    }
}
