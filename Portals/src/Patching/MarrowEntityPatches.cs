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

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MarrowEntity.Pack))]
    public static void Pack(MarrowBody hostBody, MarrowBody parasiteBody)
    {
        var hostTeleportable = hostBody.GetComponent<TeleportableBody>();

        if (hostTeleportable == null)
        {
            return;
        }

        var parasiteTeleportable = parasiteBody.GetComponent<TeleportableBody>();

        if (parasiteTeleportable == null)
        {
            return;
        }

        hostTeleportable.Pack(parasiteTeleportable);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MarrowEntity.Unpack))]
    public static void Unpack(MarrowBody hostBody, MarrowBody parasiteBody)
    {
        var hostTeleportable = hostBody.GetComponent<TeleportableBody>();

        if (hostTeleportable == null)
        {
            return;
        }

        var parasiteTeleportable = parasiteBody.GetComponent<TeleportableBody>();

        if (parasiteTeleportable == null)
        {
            return;
        }

        hostTeleportable.Unpack(parasiteTeleportable);
    }
}
