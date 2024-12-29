using System;

using HarmonyLib;

using Il2CppSLZ.Marrow;

using UnityEngine;
using UnityEngine.Rendering;

namespace Portals.Patching;

[HarmonyPatch(typeof(OpenControllerRig))]
public static class OpenControllerRigPatches
{
    public static event Action<ScriptableRenderContext, Camera> BeginCameraRendering;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(OpenControllerRig.OnBeginCameraRendering))]
    private static void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        try
        {
            BeginCameraRendering?.Invoke(ctx, cam);
        }
        catch (Exception e)
        {
            PortalsMod.Logger.Error("Caught exception in OnBeginCameraRendering: ", e);
        }
    }
}
