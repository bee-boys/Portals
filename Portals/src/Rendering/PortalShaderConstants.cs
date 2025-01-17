using UnityEngine;

namespace Portals.Rendering;

public static class PortalShaderConstants
{
    public static readonly int LeftEyeTextureId = Shader.PropertyToID("_LeftEyeTexture");
    public static readonly int RightEyeTextureId = Shader.PropertyToID("_RightEyeTexture");

    public static readonly int MainTextureId = Shader.PropertyToID("_MainTex");

    public static readonly int TargetEyeId = Shader.PropertyToID("_TargetEye");
    public static readonly int ForceEyeId = Shader.PropertyToID("_ForceEye");
    public static readonly int EyeOverrideId = Shader.PropertyToID("_EyeOverride");

    public static readonly int OpenId = Shader.PropertyToID("_Open");

    public static readonly int OutlineId = Shader.PropertyToID("_Outline");
    public static readonly int InsideId = Shader.PropertyToID("_Inside");
}
