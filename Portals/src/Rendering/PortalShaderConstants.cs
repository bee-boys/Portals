using UnityEngine;

namespace Portals.Rendering;

public static class PortalShaderConstants
{
    public static readonly int LeftEyeTextureId = Shader.PropertyToID("_LeftEyeTexture");
    public static readonly int RightEyeTextureId = Shader.PropertyToID("_RightEyeTexture");

    public static readonly int MainTextureId = Shader.PropertyToID("_MainTex");

    public static readonly int TargetEyeId = Shader.PropertyToID("_TargetEye");

    public static readonly int OpenId = Shader.PropertyToID("_Open");
}
