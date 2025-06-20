using LabFusion.Entities;
using LabFusion.Utilities;

using Portals.MonoBehaviours;

namespace PortalsModule;

public class TeleportableExtender : EntityComponentExtender<Teleportable>
{
    public static readonly FusionComponentCache<Teleportable, NetworkEntity> Cache = new();

    protected override void OnRegister(NetworkEntity networkEntity, Teleportable component)
    {
        Cache.Add(component, networkEntity);
    }

    protected override void OnUnregister(NetworkEntity networkEntity, Teleportable component)
    {
        Cache.Remove(component);
    }
}
