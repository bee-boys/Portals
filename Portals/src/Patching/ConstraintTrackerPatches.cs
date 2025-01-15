using HarmonyLib;

using Il2CppSLZ.Marrow;

using Portals.MonoBehaviours;

namespace Portals.Patching;

[HarmonyPatch(typeof(ConstraintTracker))]
public static class ConstraintTrackerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ConstraintTracker.DeleteConstraint))]
    public static void DeleteConstraint(ConstraintTracker __instance)
    {
        var trackerBody = __instance.GetComponent<TeleportableBody>();

        if (trackerBody == null)
        {
            return;
        }

        trackerBody.Unpack();
    }
}
