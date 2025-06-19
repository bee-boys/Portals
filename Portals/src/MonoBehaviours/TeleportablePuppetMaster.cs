using System;

using Il2CppSLZ.Marrow.PuppetMasta;

using MelonLoader;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class TeleportablePuppetMaster : TeleportableEntity
{
    public TeleportablePuppetMaster(IntPtr intPtr) : base(intPtr) { }

    public PuppetMaster PuppetMaster => _puppetMaster;

    private PuppetMaster _puppetMaster = null;

    protected override void OnTeleportableAwake()
    {
        base.OnTeleportableAwake();

        _puppetMaster = GetComponentInChildren<PuppetMaster>();
    }

    protected override void OnTeleport(Portal inPortal, Portal outPortal)
    {
        // updateJointAnchors doesn't like scale
        // So if the puppet teleports, just disable it
        PuppetMaster.updateJointAnchors = false;

        base.OnTeleport(inPortal, outPortal);
    }
}
