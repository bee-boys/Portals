using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using MelonLoader;

using System;

using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;

using Il2CppSLZ.Marrow.Interaction;
using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Pool;

using Portals.Patching;
using Portals.Rendering;

using UnityEngine.XR;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class Portal : MonoBehaviour
{
    public Portal(IntPtr intPtr) : base(intPtr) { }

    #region FIELD INJECTION
    public Il2CppReferenceField<MonoBehaviour> otherPortal;
    public Il2CppReferenceField<MonoBehaviour> surface;
    public Il2CppReferenceField<MonoBehaviour> expander;
    public Il2CppValueField<Vector2> size;
    #endregion

    #region FIELDS
    private Portal _otherPortal = null;

    private PortalSurface _surface = null;

    private PortalExpander _expander = null;
    #endregion

    #region PROPERTIES
    [HideFromIl2Cpp]
    public Portal OtherPortal 
    {
        get
        {
            return _otherPortal;
        }
        set
        {
            var wasOpen = OtherPortal != null;

            _otherPortal = value;

            bool isOpen = value != null;

            Surface.Open = isOpen;

            if (!isOpen && wasOpen)
            {
                OnClosedEvent?.Invoke();
            }
        }
    }

    [HideFromIl2Cpp]
    public PortalSurface Surface => _surface;

    [HideFromIl2Cpp]
    public PortalExpander Expander => _expander;

    [HideFromIl2Cpp]
    public Vector2 Size => size.Get();

    [HideFromIl2Cpp]
    public Matrix4x4 PortalEnterMatrix => Matrix4x4.TRS(transform.position, transform.rotation, GetFlatScale(transform.lossyScale));

    [HideFromIl2Cpp]
    public Matrix4x4 PortalEnterMatrixInverse => PortalEnterMatrix.inverse;

    [HideFromIl2Cpp]
    public Matrix4x4 PortalExitMatrix => Matrix4x4.TRS(transform.position, Quaternion.AngleAxis(180f, transform.up) * transform.rotation, GetFlatScale(transform.lossyScale));

    [HideFromIl2Cpp]
    public Matrix4x4 PortalExitMatrixInverse => PortalExitMatrix.inverse;

    [HideFromIl2Cpp]
    public List<Collider> WallColliders { get; set; } = new();

    [HideFromIl2Cpp]
    public event Action OnClosedEvent;

    [HideFromIl2Cpp]
    public AudioClip[] FizzleSounds { get; set; } = null;

    [HideFromIl2Cpp]
    public Poolee Poolee { get; set; } = null;
    #endregion

    private PortalCamera _leftEyeCamera = null;
    private PortalCamera _rightEyeCamera = null;

    private PortalNearPlane _leftEyeNearPlane = null;
    private PortalNearPlane _rightEyeNearPlane = null;

    private void Awake()
    {
        var otherPortal = this.otherPortal.Get();

        if (otherPortal != null)
        {
            OtherPortal = otherPortal.TryCast<Portal>();
        }

        _surface = surface.Get().TryCast<PortalSurface>();

        _expander = expander.Get().TryCast<PortalExpander>();

        Expander.ToggleCollision(false);
    }

    private void Start()
    {
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            if (collider.isTrigger)
            {
                collider.gameObject.layer = (int)MarrowLayers.Socket;
            }
        }

        _leftEyeCamera = new PortalCamera(this, Camera.StereoscopicEye.Left);
        _rightEyeCamera = new PortalCamera(this, Camera.StereoscopicEye.Right);

        Surface.SurfaceMaterial.SetTexture(PortalShaderConstants.LeftEyeTextureId, _leftEyeCamera.TargetTexture);
        Surface.SurfaceMaterial.SetTexture(PortalShaderConstants.RightEyeTextureId, _rightEyeCamera.TargetTexture);

        _leftEyeNearPlane = new PortalNearPlane(Surface.AlwaysVisibleShader);
        _rightEyeNearPlane = new PortalNearPlane(Surface.AlwaysVisibleShader);

        _leftEyeNearPlane.Material.SetTexture(PortalShaderConstants.MainTextureId, _leftEyeCamera.TargetTexture);
        _leftEyeNearPlane.Material.SetFloat(PortalShaderConstants.TargetEyeId, 0f);

        _rightEyeNearPlane.Material.SetTexture(PortalShaderConstants.MainTextureId, _rightEyeCamera.TargetTexture);
        _rightEyeNearPlane.Material.SetFloat(PortalShaderConstants.TargetEyeId, 1f);

        Surface.CorridorRenderer.enabled = false;

        OpenControllerRigPatches.PreBeginCameraRendering += OnPreBeginCameraRendering;
        OpenControllerRigPatches.BeginCameraRendering += OnBeginCameraRendering;

        Poolee = GetComponentInParent<Poolee>();
    }

    private void OnDisable()
    {
        // Break the connection
        if (OtherPortal)
        {
            OtherPortal.OtherPortal = null;
            OtherPortal = null;
        }

        Expander.ToggleCollision(false);

        FizzleSounds = null;
    }

    public void Close()
    {
        OtherPortal = null;

        Poolee.Despawn();
    }

    public void Fizzle()
    {
        OtherPortal = null;

        // Play fizzle effects
        if (FizzleSounds != null)
        {
            Audio3dManager.PlayAtPoint(FizzleSounds, transform.position, Audio3dManager.hardInteraction, 0.6f, 1f, new(0f), new(0.4f), new(1f));
        }

        Poolee.Despawn();
    }

    private void OnDestroy()
    {
        _leftEyeNearPlane.Destroy();
        _rightEyeNearPlane.Destroy();

        _leftEyeCamera.Destroy();
        _rightEyeCamera.Destroy();

        OpenControllerRigPatches.PreBeginCameraRendering -= OnPreBeginCameraRendering;
        OpenControllerRigPatches.BeginCameraRendering -= OnBeginCameraRendering;
    }

    private Vector3 GetFlatScale(Vector3 scale)
    {
        scale.z = Math.Min(scale.x, scale.y);

        return scale;
    }

    public bool HasCamera(Camera cam)
    {
        return cam == _leftEyeCamera.Camera || cam == _rightEyeCamera.Camera;
    }

    private void OnPreBeginCameraRendering(ScriptableRenderContext src, Camera cam)
    {
        _leftEyeNearPlane.Hide();
        _rightEyeNearPlane.Hide();
        Surface.CorridorRenderer.enabled = false;

        Surface.FrontRenderer.enabled = true;
        Surface.BackRenderer.enabled = true;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext src, Camera cam)
    {
        if (!OtherPortal)
        {
            return;
        }

        if (HasCamera(cam) || OtherPortal.HasCamera(cam))
        {
            return;
        }

        if (cam.targetTexture)
        {
            return;
        }

        if (cam.orthographic)
        {
            return;
        }

        if (Surface.FrontRenderer != null && !Surface.FrontRenderer.isVisible && !Surface.BackRenderer.isVisible)
        {
            return;
        }

        int iterations = 1;
        int initialValue = iterations - 1;
        float openPercent = Surface.OpenPercent;

        for (var i = initialValue; i >= 0; i--)
        {
            float percent = 0f;

            if (iterations > 1)
            {
                percent = openPercent * (1f - Math.Clamp(i / (float)initialValue, 0f, 1f));
            }

            Surface.SurfaceMaterial.SetFloat(PortalShaderConstants.OpenId, percent);

            ApplyPosition(src, cam, i);
        }

        Surface.SurfaceMaterial.SetFloat(PortalShaderConstants.OpenId, openPercent);

        DrawClippingPlane(cam);
    }

    private void ApplyPosition(ScriptableRenderContext src, Camera mainCamera, int iteration = 0)
    {
        // Transform changes
        var mainCameraTransform = mainCamera.transform;
        var otherPortalTransform = OtherPortal.transform;

        Vector3 newPosition = mainCameraTransform.position;
        Quaternion newRotation = mainCameraTransform.rotation;

        for (var i = 0; i <= iteration; i++)
        {
            newPosition = OtherPortal.PortalExitMatrix.MultiplyPoint3x4(PortalEnterMatrixInverse.MultiplyPoint3x4(newPosition));
            newRotation = OtherPortal.PortalExitMatrix.rotation * (PortalEnterMatrixInverse.rotation * newRotation);
        }

        float centerSign = Math.Sign(Vector3.Dot(otherPortalTransform.forward, otherPortalTransform.position - newPosition));

        if (IsVR(mainCamera))
        {
            CopyValues(mainCamera, _leftEyeCamera.Camera);
            CopyValues(mainCamera, _rightEyeCamera.Camera);

            Surface.SurfaceMaterial.SetInt("_ForceEye", 1);

            var (left, right) = GetEyes();

            var scaleDifference = CalculateScaleDifference(PortalEnterMatrix, OtherPortal.PortalExitMatrix);

            var offsetMatrix = Matrix4x4.TRS(newPosition, newRotation, Matrix4x4.Inverse(Matrix4x4.Scale(scaleDifference)) * mainCameraTransform.lossyScale);

            var leftEyeWorld = offsetMatrix.MultiplyPoint(left);
            var rightEyeWorld = offsetMatrix.MultiplyPoint(right);

            _leftEyeCamera.Transform.SetPositionAndRotation(leftEyeWorld, newRotation);
            _leftEyeCamera.Camera.projectionMatrix = CalculateEyeProjectionMatrix(mainCamera, Camera.StereoscopicEye.Left, centerSign, otherPortalTransform, _leftEyeCamera.Transform, _leftEyeCamera.Camera);

            Surface.SurfaceMaterial.SetFloat("_EyeOverride", 0f);

            UniversalRenderPipeline.RenderSingleCamera(src, _leftEyeCamera.Camera);

            _rightEyeCamera.Transform.SetPositionAndRotation(rightEyeWorld, newRotation);
            _rightEyeCamera.Camera.projectionMatrix = CalculateEyeProjectionMatrix(mainCamera, Camera.StereoscopicEye.Right, centerSign, otherPortalTransform, _rightEyeCamera.Transform, _rightEyeCamera.Camera);

            Surface.SurfaceMaterial.SetFloat("_EyeOverride", 1f);

            UniversalRenderPipeline.RenderSingleCamera(src, _rightEyeCamera.Camera);

            Surface.SurfaceMaterial.SetInt("_ForceEye", 0);

        }
        else
        {
            CopyValues(mainCamera, _leftEyeCamera.Camera);

            _leftEyeCamera.Transform.SetPositionAndRotation(newPosition, newRotation);

            var clipPlaneCameraSpace = CalculatePortalClipPlane(otherPortalTransform, _leftEyeCamera.Transform, _leftEyeCamera.Camera, centerSign);

            var newMatrix = CalculateObliqueMatrix(mainCamera.projectionMatrix, clipPlaneCameraSpace);

            _leftEyeCamera.Camera.projectionMatrix = newMatrix;

            UniversalRenderPipeline.RenderSingleCamera(src, _leftEyeCamera.Camera);
        }
    }

    private void CopyValues(Camera from, Camera to)
    {
        to.nearClipPlane = from.nearClipPlane;
        to.farClipPlane = from.farClipPlane;
        to.fieldOfView = from.fieldOfView;
    }

    private bool IsVR(Camera camera)
    {
        return !PortalsMod.IsMockHMD && camera.stereoTargetEye == StereoTargetEyeMask.Both;
    }

    private void DrawClippingPlane(Camera camera)
    {
        var cameraInPortal = PortalEnterMatrixInverse.MultiplyPoint3x4(camera.transform.position);
        var extents = Size * 0.5f;

        // Not in bounds
        if (cameraInPortal.x < -extents.x || cameraInPortal.x > extents.x || cameraInPortal.y < -extents.y || cameraInPortal.y > extents.y)
        {
            return;
        }

        if (IsVR(camera))
        {
            float centerSign = Math.Sign(Vector3.Dot(transform.forward, transform.position - camera.transform.position));

            if (centerSign <= 0f)
            {
                Surface.CorridorPivot.localRotation = Quaternion.identity;
            }
            else
            {
                Surface.CorridorPivot.localRotation = Quaternion.AngleAxis(180f, Vector3.up);
            }

            var (left, right) = GetEyes();

            float leftEyeSign = Math.Sign(Vector3.Dot(transform.forward, transform.position - camera.transform.localToWorldMatrix.MultiplyPoint(left)));
            float rightEyeSign = Math.Sign(Vector3.Dot(transform.forward, transform.position - camera.transform.localToWorldMatrix.MultiplyPoint(right)));

            bool isInFront = centerSign <= 0f;

            if (Mathf.Approximately(leftEyeSign, centerSign))
            {
                _leftEyeNearPlane.Render(this, camera, Camera.MonoOrStereoscopicEye.Left);
            }
            else
            {
                Surface.CorridorRenderer.enabled = true;

                Surface.CorridorMaterial.SetTexture(PortalShaderConstants.MainTextureId, _leftEyeCamera.TargetTexture);
                Surface.CorridorMaterial.SetFloat(PortalShaderConstants.TargetEyeId, 0f);

                Surface.FrontRenderer.enabled = isInFront;
                Surface.BackRenderer.enabled = !isInFront;
            }

            if (Mathf.Approximately(rightEyeSign, centerSign))
            {
                _rightEyeNearPlane.Render(this, camera, Camera.MonoOrStereoscopicEye.Right);
            }
            else
            {
                Surface.CorridorRenderer.enabled = true;

                Surface.CorridorMaterial.SetTexture(PortalShaderConstants.MainTextureId, _rightEyeCamera.TargetTexture);
                Surface.CorridorMaterial.SetFloat(PortalShaderConstants.TargetEyeId, 1f);

                Surface.FrontRenderer.enabled = isInFront;
                Surface.BackRenderer.enabled = !isInFront;
            }
        }
        else
        {
            _leftEyeNearPlane.Render(this, camera, Camera.MonoOrStereoscopicEye.Mono);
        }
    }

    private static Vector3 CalculateScaleDifference(Matrix4x4 portalMatrix, Matrix4x4 otherMatrix)
    {
        var selfScale = portalMatrix.lossyScale;
        var otherScale = otherMatrix.lossyScale;

        var scaleDifference = new Vector3(selfScale.x / otherScale.x, selfScale.y / otherScale.y, selfScale.z / otherScale.z);

        return scaleDifference;
    }

    private static Matrix4x4 CalculateEyeProjectionMatrix(Camera camera, Camera.StereoscopicEye eye, float centerSign, Transform otherPortalTransform, Transform portalCameraTransform, Camera portalCamera)
    {
        var stereoProjectionMatrix = camera.GetStereoProjectionMatrix(eye);

        float eyeSign = Math.Sign(Vector3.Dot(otherPortalTransform.forward, otherPortalTransform.position - portalCameraTransform.position));

        if (!Mathf.Approximately(eyeSign, centerSign))
        {
            return stereoProjectionMatrix;
        }

        var clipPlane = CalculatePortalClipPlane(otherPortalTransform, portalCameraTransform, portalCamera, eyeSign);

        var obliqueMatrix = CalculateObliqueMatrix(stereoProjectionMatrix, clipPlane);

        return obliqueMatrix;
    }

    private static Vector4 CalculatePortalClipPlane(Transform otherPortalTransform, Transform portalCameraTransform, Camera portalCamera, float sign)
    {
        if (Mathf.Approximately(sign, 0f))
        {
            sign = 1f;
        }

        var normal = otherPortalTransform.forward * sign;

        var plane = new Plane(normal, otherPortalTransform.position + normal * 0.001f);

        var clipPlane = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
        var clipPlaneCameraSpace = Matrix4x4.Transpose(portalCamera.cameraToWorldMatrix) * clipPlane;

        return clipPlaneCameraSpace;
    }

    private static (Vector3 left, Vector3 right) GetEyes()
    {
        var centerPos = InputTracking.GetLocalPosition(XRNode.CenterEye);
        var centerRot = InputTracking.GetLocalRotation(XRNode.CenterEye);

        var left = Quaternion.Inverse(centerRot) * (InputTracking.GetLocalPosition(XRNode.LeftEye) - centerPos);
        var right = Quaternion.Inverse(centerRot) * (InputTracking.GetLocalPosition(XRNode.RightEye) - centerPos);

        return (left, right);
    }

    private static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane)
    {
        Matrix4x4 obliqueMatrix = projection;
        Vector4 q = projection.inverse * new Vector4(
            Math.Sign(clipPlane.x),
            Math.Sign(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
        obliqueMatrix[2] = c.x - projection[3];
        obliqueMatrix[6] = c.y - projection[7];
        obliqueMatrix[10] = c.z - projection[11];
        obliqueMatrix[14] = c.w - projection[15];
        return obliqueMatrix;
    }
}