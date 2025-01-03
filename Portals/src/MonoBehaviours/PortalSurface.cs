using System;

using UnityEngine;

using MelonLoader;

using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class PortalSurface : MonoBehaviour
{
    public PortalSurface(IntPtr intPtr) : base(intPtr) { }

    #region FIELD INJECTION
    public Il2CppReferenceField<Transform> centerPivot;

    public Il2CppReferenceField<Renderer> frontRenderer;

    public Il2CppReferenceField<Renderer> backRenderer;

    public Il2CppReferenceField<Renderer> corridorRenderer;

    public Il2CppReferenceField<Transform> corridorPivot;

    public Il2CppReferenceField<Shader> alwaysVisibleShader;
    #endregion

    #region FIELDS
    private Material _surfaceMaterial = null;
    #endregion

    #region PROPERTIES
    [HideFromIl2Cpp]
    public Transform CenterPivot => centerPivot.Get();

    [HideFromIl2Cpp]
    public Renderer FrontRenderer => frontRenderer.Get();

    [HideFromIl2Cpp]
    public Renderer BackRenderer => backRenderer.Get();

    [HideFromIl2Cpp]
    public Renderer CorridorRenderer => corridorRenderer.Get();

    [HideFromIl2Cpp]
    public Transform CorridorPivot => corridorPivot.Get();

    [HideFromIl2Cpp]
    public Shader AlwaysVisibleShader => alwaysVisibleShader.Get();

    [HideFromIl2Cpp]
    public Material SurfaceMaterial => _surfaceMaterial;
    #endregion

    #region METHODS
    public void SetOutline(Color color)
    {
        SurfaceMaterial.SetColor("_Outline", color);
    }
    
    public void SetInside(Color color)
    {
        SurfaceMaterial.SetColor("_Inside", color);
    }

    public void SetOpen(float open)
    {
        SurfaceMaterial.SetFloat("_Open", open);
    }
    #endregion

    #region UNITY
    private void Awake()
    {
        _surfaceMaterial = FrontRenderer.material;

        BackRenderer.sharedMaterial = _surfaceMaterial;
    }
    #endregion
}
