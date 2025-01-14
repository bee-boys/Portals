using HarmonyLib;

using Il2CppSLZ.Marrow;

using Portals.Rendering;

namespace Portals.Patching;

[HarmonyPatch(typeof(OpenControllerRig))]
public static class OpenControllerRigPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(OpenControllerRig.OnMarrowReady))]
    private static void OnMarrowReady()
    {
        // Hook AFTER the OpenControllerRig has
        // This way we get the most recent headset position
        RenderingHooks.HookRenderPipeline();
    }
}
