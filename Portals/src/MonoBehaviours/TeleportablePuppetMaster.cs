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

    public override void Teleport(Portal inPortal, Portal outPortal)
    {
        // updateJointAnchors doesn't like scale
        // So if the puppet teleports, just disable it
        PuppetMaster.updateJointAnchors = false;

        base.Teleport(inPortal, outPortal);

        foreach (var muscle in PuppetMaster.muscles)
        {
            var marrowJoint = muscle.marrowJoint;

            if (!marrowJoint || !marrowJoint.HasConfigJoint)
            {
                continue;
            }

            var configJoint = marrowJoint._configurableJoint;

            configJoint.anchor = configJoint.anchor;
            configJoint.connectedAnchor = configJoint.connectedAnchor;
        }
    }
}
