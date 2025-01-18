using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

using Portals.MonoBehaviours;

using System;

namespace Portals.Rendering;

public class PortalCamera
{
    public Camera Camera { get; set; }

    public Transform Transform { get; set; }

    public GameObject GameObject { get; set; }

    private RenderTexture _targetTexture = null;
    public RenderTexture TargetTexture
    {
        get
        {
            return _targetTexture;
        }
        set
        {
            _targetTexture = value;

            Camera.targetTexture = value;

            OnTargetTextureChanged?.Invoke(value);
        }
    }

    public Camera.StereoscopicEye Eye { get; set; }

    public event Action<RenderTexture> OnTargetTextureChanged;

    public PortalCamera(Portal portal, Camera.StereoscopicEye eye)
    {
        GameObject = new GameObject($"{eye} Portal Camera");
        Transform = GameObject.transform;

        Transform.parent = portal.transform;

        Camera = GameObject.AddComponent<Camera>();
        Camera.enabled = false;
        Camera.useOcclusionCulling = false; // Breaks with oblique camera matrices
        Camera.allowHDR = true;
        Camera.allowMSAA = false;
        Camera.clearFlags = CameraClearFlags.SolidColor;
        Camera.backgroundColor = Color.black;
        Camera.stereoTargetEye = StereoTargetEyeMask.None;

        var data = GameObject.AddComponent<UniversalAdditionalCameraData>();
        data.antialiasing = AntialiasingMode.None;
        data.allowXRRendering = true;

        var (width, height) = GetDimensions();

        Eye = eye;

        PortalPreferences.OnRenderScaleChanged += OnRenderScaleChanged;

        OnRenderScaleChanged(PortalPreferences.RenderScale);
    }

    public void ReleaseTexture()
    {
        if (TargetTexture != null)
        {
            TargetTexture.Release();
            TargetTexture = null;
        }
    }

    private void OnRenderScaleChanged(float value)
    {
        ReleaseTexture();

        var (width, height) = GetDimensions();

        var powerOfTwo = Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(Math.Min(width * value, height * value)));

        // Make sure to keep the depth at 24, depth of 0 doesn't render properly on Quest
        TargetTexture = new RenderTexture(powerOfTwo, powerOfTwo, 24);
    }

    private static (int width, int height) GetDimensions()
    {
        int width = XRSettings.eyeTextureWidth;
        int height = XRSettings.eyeTextureHeight;

        if (width <= 0 || height <= 0)
        {
            width = Screen.width;
            height = Screen.height;
        }

        return (width, height);
    }

    public void Destroy()
    {
        PortalPreferences.OnRenderScaleChanged -= OnRenderScaleChanged;

        GameObject.Destroy(GameObject);

        ReleaseTexture();
    }
}
