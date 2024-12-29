using HarmonyLib;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.PuppetMasta;

using Portals.MonoBehaviours;

namespace Portals.Patching;

[HarmonyPatch(typeof(MarrowEntity))]
public static class MarrowEntityPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MarrowEntity.Awake))]
    public static void Awake(MarrowEntity __instance)
    {
        var rigManager = __instance.GetComponentInParent<RigManager>();

        if (rigManager != null)
        {
            if (__instance == rigManager.physicsRig.marrowEntity)
            {
                rigManager.gameObject.AddComponent<TeleportableRigManager>();
            }
        }
        else if (__instance.GetComponentInChildren<PuppetMaster>())
        {
            __instance.gameObject.AddComponent<TeleportablePuppetMaster>();
        }
        else
        {
            __instance.gameObject.AddComponent<TeleportableEntity>();
        }
    }
}
