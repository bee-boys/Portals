using UnityEngine;

namespace Portals.Rendering;

public static class PortalShaderConstants
{
    public static readonly int LeftEyeTextureID = Shader.PropertyToID("_LeftEyeTexture");
    public static readonly int RightEyeTextureID = Shader.PropertyToID("_RightEyeTexture");

    public static readonly int MainTextureID = Shader.PropertyToID("_MainTex");

    public static readonly int TargetEyeID = Shader.PropertyToID("_TargetEye");
    public static readonly int ForceEyeID = Shader.PropertyToID("_ForceEye");
    public static readonly int EyeOverrideID = Shader.PropertyToID("_EyeOverride");

    public static readonly int OpenID = Shader.PropertyToID("_Open");

    public static readonly int OutlineID = Shader.PropertyToID("_Outline");
    public static readonly int InsideID = Shader.PropertyToID("_Inside");
}
