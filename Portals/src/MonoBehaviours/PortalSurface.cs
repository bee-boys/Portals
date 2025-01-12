using System;

using UnityEngine;

using MelonLoader;

using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;
using Portals.Rendering;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class PortalSurface : MonoBehaviour
{
    public PortalSurface(IntPtr intPtr) : base(intPtr) { }

    public const float OpenLength = 0.3f;

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
    private Material _corridorMaterial = null;

    private bool _open = false;

    private float _openPercent = 0f;

    private float _openingElapsed = 0f;

    private float _scalingElapsed = 0f;
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

    [HideFromIl2Cpp]
    public Material CorridorMaterial => _corridorMaterial;

    public bool Open
    {
        get
        {
            return _open;
        }
        set
        {
            _open = value;

            if (!value)
            {
                OpenPercent = 0f;
            }
            else
            {
                _openingElapsed = 0f;
                OpenPercent = 0f;
            }
        }
    }

    public float OpenPercent
    {
        get
        {
            return _openPercent;
        }
        set
        {
            _openPercent = value;

            SurfaceMaterial.SetFloat(PortalShaderConstants.OpenId, value);
        }
    }
    #endregion

    #region METHODS
    public void SetOutline(Color color)
    {
        SurfaceMaterial.SetColor(PortalShaderConstants.OutlineId, color);
    }
    
    public void SetInside(Color color)
    {
        SurfaceMaterial.SetColor(PortalShaderConstants.InsideId, color);
    }
    #endregion

    #region UNITY
    private void Awake()
    {
        _surfaceMaterial = FrontRenderer.material;

        BackRenderer.sharedMaterial = _surfaceMaterial;

        _corridorMaterial = CorridorRenderer.material;
    }

    private void OnEnable()
    {
        OpenPercent = 0f;
        _openingElapsed = 0f;
        _scalingElapsed = 0f;

        CenterPivot.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (Open && _openingElapsed < OpenLength)
        {
            _openingElapsed += Time.deltaTime;

            OpenPercent = Mathf.Clamp01(_openingElapsed / OpenLength);
        }

        if (_scalingElapsed < OpenLength)
        {
            _scalingElapsed += Time.deltaTime;

            CenterPivot.localScale = Vector3.one * Mathf.Clamp01(_scalingElapsed / OpenLength);
        }
    }
    #endregion
}
