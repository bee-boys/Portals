using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.SDK.Modules;

namespace Portals.Fusion;

public class TeleportableTeleportData : INetSerializable
{
    public const int Size = NetworkEntityReference.Size;

    public NetworkEntityReference Entity;

    public int? GetSize() => Size;

    public void Serialize(INetSerializer serializer)
    {
        serializer.SerializeValue(ref Entity);
    }
}

public class TeleportableTeleportMessage : ModuleMessageHandler
{
    protected override void OnHandleMessage(ReceivedMessage received)
    {
        var data = received.ReadData<TeleportableTeleportData>();

        var networkEntity = data.Entity.GetEntity();

        if (networkEntity == null)
        {
            return;
        }

        var extender = networkEntity.GetExtender<TeleportableExtender>();

        if (extender == null)
        {
            return;
        }

        var teleportable = extender.Component;

        if (teleportable.HasPortals)
        {
            teleportable.Teleport(teleportable.InPortal, teleportable.OutPortal);
        }
    }
}
