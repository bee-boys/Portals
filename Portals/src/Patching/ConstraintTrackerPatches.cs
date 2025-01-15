using HarmonyLib;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;

using Portals.MonoBehaviours;

namespace Portals.Patching;

[HarmonyPatch(typeof(ConstraintTracker))]
public static class ConstraintTrackerPatches
{
    [HarmonyPrefix()]
    [HarmonyPatch(nameof(ConstraintTracker.DeleteConstraint))]
    public static void DeleteConstraint(ConstraintTracker __instance)
    {
        var marrowBody = __instance.GetComponentInParent<MarrowBody>();

        if (marrowBody == null)
        {
            return;
        }

        var teleportableBody = marrowBody.GetComponent<TeleportableBody>();

        if (teleportableBody == null)
        {
            return;
        }

        teleportableBody.Unpack();
    }
}
