using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.SDK.Modules;

using UnityEngine;

namespace Portals.Fusion;

public class PortalGunFireData : INetSerializable
{
    public const int Size = sizeof(byte) + NetworkEntityReference.Size + sizeof(bool) + sizeof(float) * 2;

    public byte PlayerID;

    public NetworkEntityReference Entity;

    public bool Primary;

    public Vector2 Dimensions;

    public int? GetSize() => Size;

    public void Serialize(INetSerializer serializer)
    {
        serializer.SerializeValue(ref PlayerID);
        serializer.SerializeValue(ref Entity);
        serializer.SerializeValue(ref Primary);
        serializer.SerializeValue(ref Dimensions);
    }
}

public class PortalGunFireMessage : ModuleMessageHandler
{
    protected override void OnHandleMessage(ReceivedMessage received)
    {
        var data = received.ReadData<PortalGunFireData>();

        var networkEntity = data.Entity.GetEntity();

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

        gun.Fire(data.Primary, data.Dimensions);

        PortalsModule.IgnoreOverrides = false;
    }
}
