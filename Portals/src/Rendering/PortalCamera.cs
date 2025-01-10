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

    public RenderTexture TargetTexture { get; set; }

    public Camera.StereoscopicEye Eye { get; set; }

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

        var data = GameObject.AddComponent<UniversalAdditionalCameraData>();
        data.antialiasing = AntialiasingMode.None;
        data.allowXRRendering = true;

        var (width, height) = GetDimensions();

        TargetTexture = new RenderTexture(width, height, 24);
        Camera.targetTexture = TargetTexture;

        Camera.stereoTargetEye = StereoTargetEyeMask.None;

        Eye = eye;
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
        GameObject.Destroy(GameObject);

        TargetTexture.Release();
        TargetTexture = null;
    }
}
