using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using MelonLoader;

using System;

using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSLZ.Marrow.Interaction;

using Portals.Patching;
using Portals.Rendering;

using UnityEngine.XR;

namespace Portals;

[RegisterTypeInIl2Cpp]
public class Portal : MonoBehaviour
{
    public Portal(IntPtr intPtr) : base(intPtr) { }

    public Il2CppReferenceField<MonoBehaviour> otherPortal;
    public Il2CppReferenceField<Renderer> portalRenderer;
    public Il2CppReferenceField<Renderer> portalNearPlane;
    public Il2CppValueField<Vector2> size;


    [HideFromIl2Cpp]
    public Portal OtherPortal => otherPortal.Get().TryCast<Portal>();

    [HideFromIl2Cpp]
    public Renderer PortalRenderer => portalRenderer.Get();

    [HideFromIl2Cpp]
    public Renderer PortalNearPlane => portalNearPlane.Get();

    [HideFromIl2Cpp]
    public Vector2 Size => size.Get();

    private PortalCamera _leftEyeCamera = null;
    private PortalCamera _rightEyeCamera = null;

    private PortalNearPlane _leftEyeNearPlane = null;
    private PortalNearPlane _rightEyeNearPlane = null;

    private Material _rendererMaterial = null;

    public void Awake()
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

        _rendererMaterial = PortalRenderer.material;

        _rendererMaterial.SetTexture(PortalShaderConstants.LeftEyeTextureName, _leftEyeCamera.TargetTexture);
        _rendererMaterial.SetTexture(PortalShaderConstants.RightEyeTextureName, _rightEyeCamera.TargetTexture);

        _leftEyeNearPlane = new PortalNearPlane(PortalNearPlane.sharedMaterial.shader);
        _rightEyeNearPlane = new PortalNearPlane(PortalNearPlane.sharedMaterial.shader);

        _leftEyeNearPlane.Material.SetTexture(PortalShaderConstants.LeftEyeTextureName, _leftEyeCamera.TargetTexture);
        _leftEyeNearPlane.Material.SetTexture(PortalShaderConstants.RightEyeTextureName, _rightEyeCamera.TargetTexture);

        _rightEyeNearPlane.Material.SetTexture(PortalShaderConstants.LeftEyeTextureName, _leftEyeCamera.TargetTexture);
        _rightEyeNearPlane.Material.SetTexture(PortalShaderConstants.RightEyeTextureName, _rightEyeCamera.TargetTexture);

        OpenControllerRigPatches.BeginCameraRendering += OnBeginCameraRendering;
    }

    public void OnDestroy()
    {
        _leftEyeNearPlane.Destroy();
        _rightEyeNearPlane.Destroy();

        _leftEyeCamera.Destroy();
        _rightEyeCamera.Destroy();

        OpenControllerRigPatches.BeginCameraRendering -= OnBeginCameraRendering;
    }

    public bool HasCamera(Camera cam)
    {
        return cam == _leftEyeCamera.Camera || cam == _rightEyeCamera.Camera;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext src, Camera cam)
    {
        _leftEyeNearPlane.MeshRenderer.enabled = false;
        _rightEyeNearPlane.MeshRenderer.enabled = false;

        if (PortalRenderer != null && !PortalRenderer.isVisible)
        {
            return;
        }

        if (cam.orthographic)
        {
            return;
        }

        if (HasCamera(cam) || OtherPortal.HasCamera(cam))
        {
            return;
        }

        int iterations = 1;
        int initialValue = iterations - 1;

        for (var i = initialValue; i >= 0; i--)
        {
            float percent = Math.Clamp(i / (float)initialValue, 0f, 1f);

            _rendererMaterial.SetFloat("_Fade", percent);

            ApplyPosition(src, cam, i);
        }

        _rendererMaterial.SetFloat("_Fade", 0f);

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
            newPosition = otherPortalTransform.TransformPoint(transform.InverseTransformPoint(newPosition));
            newRotation = otherPortalTransform.rotation * (Quaternion.Inverse(transform.rotation) * newRotation);
        }

        float centerSign = Math.Sign(Vector3.Dot(otherPortalTransform.forward, otherPortalTransform.position - newPosition));

        if (IsVR(mainCamera))
        {
            _rendererMaterial.SetInt("_ForceEye", 1);

            var (left, right) = GetEyes();

            var scaleDifference = CalculateScaleDifference(transform, otherPortalTransform);

            var offsetMatrix = Matrix4x4.TRS(newPosition, newRotation, Matrix4x4.Inverse(Matrix4x4.Scale(scaleDifference)) * mainCameraTransform.lossyScale);

            var leftEyeWorld = offsetMatrix.MultiplyPoint(left);
            var rightEyeWorld = offsetMatrix.MultiplyPoint(right);

            _leftEyeCamera.Transform.SetPositionAndRotation(leftEyeWorld, newRotation);
            _leftEyeCamera.Camera.projectionMatrix = CalculateEyeProjectionMatrix(mainCamera, Camera.StereoscopicEye.Left, centerSign, otherPortalTransform, _leftEyeCamera.Transform, _leftEyeCamera.Camera);

            _rendererMaterial.SetFloat("_EyeOverride", 0f);

            UniversalRenderPipeline.RenderSingleCamera(src, _leftEyeCamera.Camera);

            _rightEyeCamera.Transform.SetPositionAndRotation(rightEyeWorld, newRotation);
            _rightEyeCamera.Camera.projectionMatrix = CalculateEyeProjectionMatrix(mainCamera, Camera.StereoscopicEye.Right, centerSign, otherPortalTransform, _rightEyeCamera.Transform, _rightEyeCamera.Camera);

            _rendererMaterial.SetFloat("_EyeOverride", 1f);

            UniversalRenderPipeline.RenderSingleCamera(src, _rightEyeCamera.Camera);

            _rendererMaterial.SetInt("_ForceEye", 0);

        }
        else
        {
            _leftEyeCamera.Transform.SetPositionAndRotation(newPosition, newRotation);

            var clipPlaneCameraSpace = CalculatePortalClipPlane(otherPortalTransform, _leftEyeCamera.Transform, _leftEyeCamera.Camera, centerSign);

            var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);

            _leftEyeCamera.Camera.projectionMatrix = newMatrix;
            UniversalRenderPipeline.RenderSingleCamera(src, _leftEyeCamera.Camera);
        }
    }

    private bool IsVR(Camera camera)
    {
        return !PortalsMod.IsMockHMD && camera.stereoTargetEye == StereoTargetEyeMask.Both;
    }

    private void DrawClippingPlane(Camera camera)
    {
        if (IsVR(camera))
        {
            _leftEyeNearPlane.Render(this, camera, Camera.MonoOrStereoscopicEye.Left);
            _rightEyeNearPlane.Render(this, camera, Camera.MonoOrStereoscopicEye.Right);
        }
        else
        {
            _leftEyeNearPlane.Render(this, camera, Camera.MonoOrStereoscopicEye.Mono);
        }
    }

    private static Vector3 CalculateScaleDifference(Transform portal, Transform other)
    {
        var selfScale = portal.lossyScale;
        var otherScale = other.lossyScale;

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
        var clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlane;

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