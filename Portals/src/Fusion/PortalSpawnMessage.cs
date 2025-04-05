using LabFusion.Data;
using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.SDK.Modules;

using Portals.MonoBehaviours;
using Portals.Pooling;

using UnityEngine;

namespace Portals.Fusion;

public class PortalSpawnData : INetSerializable
{
    public byte playerId;

    public ushort gunId;

    public bool primary;

    public Vector2 size;

    public Vector3 position;

    public Quaternion rotation;

    public PortalShape shape;

    public int id;

    private SerializedQuaternion _compressedRotation = null;

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

    public void Serialize(INetSerializer serializer)
    {
        if (!serializer.IsReader)
        {
            _compressedRotation = SerializedQuaternion.Compress(rotation);
        }

        serializer.SerializeValue(ref playerId);

        serializer.SerializeValue(ref gunId);
        serializer.SerializeValue(ref primary);
        serializer.SerializeValue(ref size);

        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);

        serializer.SerializeValue(ref shape, Precision.OneByte);
        serializer.SerializeValue(ref id);

        if (serializer.IsReader)
        {
            rotation = _compressedRotation.Expand();
        }
    }
}

public class PortalSpawnMessage : ModuleMessageHandler
{
    protected override void OnHandleMessage(ReceivedMessage received)
    {
        var data = received.ReadData<PortalSpawnData>();

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
