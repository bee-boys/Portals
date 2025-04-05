using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.SDK.Modules;

using UnityEngine;

namespace Portals.Fusion;

public class PortalGunFireData : INetSerializable
{
    public const int Size = sizeof(byte) + sizeof(ushort) + sizeof(bool) + sizeof(float) * 2;

    public byte playerId;

    public ushort entityId;

    public bool primary;

    public Vector2 size;

    public int? GetSize() => Size;

    public void Serialize(INetSerializer serializer)
    {
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref entityId);
        serializer.SerializeValue(ref primary);
        serializer.SerializeValue(ref size);
    }
}

public class PortalGunFireMessage : ModuleMessageHandler
{
    protected override void OnHandleMessage(ReceivedMessage received)
    {
        var data = received.ReadData<PortalGunFireData>();

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
