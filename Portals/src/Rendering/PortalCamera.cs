using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

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
        Camera.nearClipPlane = 0.01f;

        var data = GameObject.AddComponent<UniversalAdditionalCameraData>();
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.Low;

        Camera.clearFlags = CameraClearFlags.SolidColor;
        Camera.backgroundColor = Color.black;

        var dimensions = GetDimensions();

        TargetTexture = new RenderTexture(dimensions.width, dimensions.height, 24, UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
        Camera.targetTexture = TargetTexture;

        Camera.stereoTargetEye = StereoTargetEyeMask.None;

        Eye = eye;
    }

    private (int width, int height) GetDimensions()
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
