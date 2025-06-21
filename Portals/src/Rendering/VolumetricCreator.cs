using BoneLib;

using Il2Cpp;
using Il2CppSLZ.Bonelab.SaveData;

using UnityEngine;

namespace Portals.Rendering;

public static class VolumetricCreator
{
    public static VolumetricRendering GetPlayerVolumetricRendering()
    {
        return Player.ControllerRig.headset.GetComponent<VolumetricRendering>();
    }

    public static void AssignComputeShaders(VolumetricRendering volumetricRendering)
    {
        var playerVolumetricRendering = GetPlayerVolumetricRendering();

        volumetricRendering.FroxelFogCompute = playerVolumetricRendering.FroxelFogCompute;
        volumetricRendering.FroxelIntegrationCompute = playerVolumetricRendering.FroxelIntegrationCompute;
        volumetricRendering.ClipmapCompute = playerVolumetricRendering.ClipmapCompute;
        volumetricRendering.BlurCompute = playerVolumetricRendering.BlurCompute;
    }

    public static VolumetricRendering AddVolumetricRendering(Camera camera)
    {
        var volumetricRendering = camera.gameObject.AddComponent<VolumetricRendering>();

        volumetricRendering.tempOffset = 0f;
        volumetricRendering.cam = camera;
        volumetricRendering.volumetricData = GetVolumetricData();
        volumetricRendering.reprojectionAmount = 0.95f;
        volumetricRendering.FroxelBlur = VolumetricRendering.BlurType.None;
        volumetricRendering.SliceDistributionUniformity = 0.5f;
        volumetricRendering.albedo = Color.white;
        volumetricRendering.meanFreePath = 15f;
        volumetricRendering.StaticLightMultiplier = 1f;

        AssignComputeShaders(volumetricRendering);

        return volumetricRendering;
    }

    public static VolumetricData GetVolumetricData()
    {
        if (HelperMethods.IsAndroid())
        {
            return GraphicsManager.QuestVolumetricRenderingSettings;
        }

        return GraphicsManager.PCVolumetricRenderingSettings;
    }
}
