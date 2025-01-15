using HarmonyLib;

using Il2CppSLZ.Marrow;

using Portals.MonoBehaviours;

namespace Portals.Patching;

[HarmonyPatch(typeof(Constrainer))]
public static class ConstrainerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Constrainer.PrimaryButtonUp))]
    public static void PrimaryButtonUp(Constrainer __instance)
    {
        if (__instance._gO1 == null)
        {
            return;
        }

        var mb1 = __instance._mb1;
        var mb2 = __instance._mb2;

        if (mb1 == null || mb2 == null)
        {
            return;
        }

        // mb1 is the host, so mb2 should pack under it
        var teleportableHost = mb1.GetComponent<TeleportableBody>();
        var teleportableParasite = mb2.GetComponent<TeleportableBody>();

        if (teleportableHost == null || teleportableParasite == null)
        {
            return;
        }

        teleportableParasite.Pack(teleportableHost);
    }
}
