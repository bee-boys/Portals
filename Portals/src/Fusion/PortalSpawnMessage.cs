using LabFusion.Data;
using LabFusion.Entities;
using LabFusion.Exceptions;
using LabFusion.Network;
using LabFusion.SDK.Modules;

using Portals.MonoBehaviours;
using Portals.Pooling;

using UnityEngine;

namespace Portals.Fusion;

public class PortalSpawnData : IFusionSerializable
{
    public byte playerId;

    public ushort gunId;

    public bool primary;

    public Vector2 size;

    public Vector3 position;

    public Quaternion rotation;

    public PortalShape shape;

    public int id;

    public static PortalSpawnData Create(byte playerId, ushort gunId, PortalSpawner.PortalSpawnInfo spawnInfo)
    {
        return new PortalSpawnData()
        {
            playerId = playerId,

            gunId = gunId,
            primary = spawnInfo.Primary,
            size = spawnInfo.Size,

            position = spawnInfo.Position,
            rotation = spawnInfo.Rotation,
            shape = spawnInfo.Shape,
            id = spawnInfo.ID.GetValueOrDefault(),
        };
    }

    public void Serialize(FusionWriter writer)
    {
        writer.Write(playerId);

        writer.Write(gunId);
        writer.Write(primary);
        writer.Write(size);

        writer.Write(position);
        writer.Write(SerializedQuaternion.Compress(rotation));

        writer.Write((byte)shape);
        writer.Write(id);
    }

    public void Deserialize(FusionReader reader)
    {
        playerId = reader.ReadByte();

        gunId = reader.ReadUInt16();
        primary = reader.ReadBoolean();
        size = reader.ReadVector2();

        position = reader.ReadVector3();
        rotation = reader.ReadFusionSerializable<SerializedQuaternion>().Expand();

        shape = (PortalShape)reader.ReadByte();
        id = reader.ReadInt32();
    }
}

public class PortalSpawnMessage : ModuleMessageHandler
{
    public override void HandleMessage(byte[] bytes, bool isServerHandled = false)
    {
        using var reader = FusionReader.Create(bytes);

        var data = reader.ReadFusionSerializable<PortalSpawnData>();

        if (isServerHandled)
        {
            MessageSender.BroadcastMessageExcept(data.playerId, NetworkChannel.Reliable, FusionMessage.ModuleCreate<PortalSpawnMessage>(bytes), false);
            return;
        }

        var networkEntity = NetworkEntityManager.IdManager.RegisteredEntities.GetEntity(data.gunId);

        if (networkEntity == null)
        {
            return;
        }

        var gunExtender = networkEntity.GetExtender<PortalGunExtender>();

        if (gunExtender == null)
        {
            return;
        }

        var gun = gunExtender.Component;

        var spawnInfo = new PortalSpawner.PortalSpawnInfo()
        {
            Size = data.size,
            Shape = data.shape,
            ID = data.id,
            Position = data.position,
            Rotation = data.rotation,
            Primary = data.primary,
            SpawnCallback = OnPortalSpawned,
        };

        PortalSpawner.Spawn(spawnInfo);

        void OnPortalSpawned(Portal portal)
        {
            if (gun != null)
            {
                gun.RegisterPortal(portal, data.primary);
            }
        }
    }
}
