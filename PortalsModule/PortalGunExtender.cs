using LabFusion.Entities;
using LabFusion.Utilities;

using Portals.MonoBehaviours;

namespace PortalsModule;

public class PortalGunExtender : EntityComponentExtender<PortalGun>
{
    public static readonly FusionComponentCache<PortalGun, NetworkEntity> Cache = new();

    protected override void OnRegister(NetworkEntity networkEntity, PortalGun component)
    {
        Cache.Add(component, networkEntity);
    }

    protected override void OnUnregister(NetworkEntity networkEntity, PortalGun component)
    {
        Cache.Remove(component);
    }
}
