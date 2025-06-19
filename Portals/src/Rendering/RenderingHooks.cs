using System;

using UnityEngine.Rendering;
using UnityEngine;

namespace Portals.Rendering;

public static class RenderingHooks
{
    public static event Action<ScriptableRenderContext, Camera> PreBeginCameraRendering;

    public static event Action<ScriptableRenderContext, Camera> BeginCameraRendering;

    private static bool _hooked = false;
    private static Il2CppSystem.Action<ScriptableRenderContext, Camera> _hook = null;

    public static void HookRenderPipeline()
    {
        UnHookRenderPipeline();

        _hook = (Il2CppSystem.Action<ScriptableRenderContext, Camera>)OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += _hook;
        _hooked = true;
    }

    public static void UnHookRenderPipeline()
    {
        if (_hooked)
        {
            RenderPipelineManager.beginCameraRendering -= _hook;

            _hooked = false;
            _hook = null;
        }
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        // Prevent occasional memory access issues
        if (ctx.m_Ptr == IntPtr.Zero || cam == null)
        {
            return;
        }

        try
        {
            PreBeginCameraRendering?.Invoke(ctx, cam);

            BeginCameraRendering?.Invoke(ctx, cam);
        }
        catch (Exception e)
        {
            PortalsMod.Logger.Error("Caught exception in OnBeginCameraRendering: ", e);
        }
    }
}
