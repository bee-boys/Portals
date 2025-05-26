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
    public byte PlayerID;

    public ushort GunID;

    public bool Primary;

    public Vector2 Size;

    public Vector3 Position;

    public Quaternion Rotation;

    public PortalShape Shape;

    public int ID;

    private SerializedQuaternion _compressedRotation = null;

    public static PortalSpawnData Create(byte playerID, ushort gunID, PortalSpawner.PortalSpawnInfo spawnInfo)
    {
        return new PortalSpawnData()
        {
            PlayerID = playerID,

            GunID = gunID,
            Primary = spawnInfo.Primary,
            Size = spawnInfo.Size,

            Position = spawnInfo.Position,
            Rotation = spawnInfo.Rotation,
            Shape = spawnInfo.Shape,
            ID = spawnInfo.ID.GetValueOrDefault(),
        };
    }

    public void Serialize(INetSerializer serializer)
    {
        if (!serializer.IsReader)
        {
            _compressedRotation = SerializedQuaternion.Compress(Rotation);
        }

        serializer.SerializeValue(ref PlayerID);

        serializer.SerializeValue(ref GunID);
        serializer.SerializeValue(ref Primary);
        serializer.SerializeValue(ref Size);

        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref _compressedRotation);

        serializer.SerializeValue(ref Shape, Precision.OneByte);
        serializer.SerializeValue(ref ID);

        if (serializer.IsReader)
        {
            Rotation = _compressedRotation.Expand();
        }
    }
}

public class PortalSpawnMessage : ModuleMessageHandler
{
    protected override void OnHandleMessage(ReceivedMessage received)
    {
        var data = received.ReadData<PortalSpawnData>();

        var networkEntity = NetworkEntityManager.IdManager.RegisteredEntities.GetEntity(data.GunID);

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
            Size = data.Size,
            Shape = data.Shape,
            ID = data.ID,
            Position = data.Position,
            Rotation = data.Rotation,
            Primary = data.Primary,
            SpawnCallback = OnPortalSpawned,
        };

        PortalSpawner.Spawn(spawnInfo);

        void OnPortalSpawned(Portal portal)
        {
            if (gun != null)
            {
                gun.RegisterPortal(portal, data.Primary);
            }
        }
    }
}
