using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using MelonLoader;

using System;

using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;

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

    private Material _rendererMaterial = null;

    private Mesh _nearPlaneMesh = null;

    public void Awake()
    {
        _leftEyeCamera = new PortalCamera(this, Camera.StereoscopicEye.Left);
        _rightEyeCamera = new PortalCamera(this, Camera.StereoscopicEye.Right);

        _rendererMaterial = PortalRenderer.material;

        _rendererMaterial.SetTexture("_LeftEyeTexture", _leftEyeCamera.TargetTexture);
        _rendererMaterial.SetTexture("_RightEyeTexture", _rightEyeCamera.TargetTexture);

        var nearPlaneMaterial = PortalNearPlane.material;
        nearPlaneMaterial.SetTexture("_LeftEyeTexture", _leftEyeCamera.TargetTexture);
        nearPlaneMaterial.SetTexture("_RightEyeTexture", _leftEyeCamera.TargetTexture);

        _nearPlaneMesh = new Mesh();
        PortalNearPlane.GetComponent<MeshFilter>().sharedMesh = _nearPlaneMesh;

        PortalNearPlane.enabled = false;

        PortalNearPlane.transform.parent = null;

        OpenControllerRigPatches.BeginCameraRendering += OnBeginCameraRendering;
    }

    public void OnDestroy()
    {
        if (PortalNearPlane != null)
        {
            Destroy(PortalNearPlane.gameObject);
        }

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
        PortalNearPlane.enabled = false;

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

        if (!PortalsMod.IsMockHMD && mainCamera.stereoTargetEye == StereoTargetEyeMask.Both)
        {
            _rendererMaterial.SetInt("_ForceEye", 1);

            var (left, right) = GetEyes();

            var scaleDifference = CalculateScaleDifference(transform, otherPortalTransform);

            var offsetMatrix = Matrix4x4.TRS(newPosition, newRotation, Matrix4x4.Inverse(Matrix4x4.Scale(scaleDifference)) * mainCameraTransform.lossyScale);

            var leftEyeWorld = offsetMatrix.MultiplyPoint(left);
            var rightEyeWorld = offsetMatrix.MultiplyPoint(right);

            _leftEyeCamera.Transform.SetPositionAndRotation(leftEyeWorld, newRotation);
            _leftEyeCamera.Camera.projectionMatrix = CalculateObliqueMatrix(mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), CalculatePortalClipPlane(otherPortalTransform, _leftEyeCamera.Transform, _leftEyeCamera.Camera));

            _rendererMaterial.SetFloat("_EyeOverride", 0f);

            UniversalRenderPipeline.RenderSingleCamera(src, _leftEyeCamera.Camera);

            _rightEyeCamera.Transform.SetPositionAndRotation(rightEyeWorld, newRotation);
            _rightEyeCamera.Camera.projectionMatrix = CalculateObliqueMatrix(mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), CalculatePortalClipPlane(otherPortalTransform, _rightEyeCamera.Transform, _rightEyeCamera.Camera));

            _rendererMaterial.SetFloat("_EyeOverride", 1f);

            UniversalRenderPipeline.RenderSingleCamera(src, _rightEyeCamera.Camera);

            _rendererMaterial.SetInt("_ForceEye", 0);

        }
        else
        {
            _leftEyeCamera.Transform.SetPositionAndRotation(newPosition, newRotation);

            var clipPlaneCameraSpace = CalculatePortalClipPlane(otherPortalTransform, _leftEyeCamera.Transform, _leftEyeCamera.Camera);

            var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);

            _leftEyeCamera.Camera.projectionMatrix = newMatrix;
            UniversalRenderPipeline.RenderSingleCamera(src, _leftEyeCamera.Camera);
        }
    }

    private void DrawClippingPlane(Camera camera)
    {
        var plane = new Plane(transform.forward, transform.position);

        float camSign = Mathf.Sign(plane.GetDistanceToPoint(camera.transform.position));

        float distance = camera.nearClipPlane + 0.001f;

        var nearPlaneTransform = PortalNearPlane.transform;

        nearPlaneTransform.position = camera.transform.position + camera.transform.forward * distance;
        nearPlaneTransform.rotation = camera.transform.rotation;

        var portalExtents = Size * 0.5f;
        var portalV1 = new Vector3(-portalExtents.x, -portalExtents.y);
        var portalV2 = new Vector3(portalExtents.x, -portalExtents.y);
        var portalV3 = new Vector3(portalExtents.x, portalExtents.y);
        var portalV4 = new Vector3(-portalExtents.x, portalExtents.y);

        float minX = Mathf.Min(portalV1.x, portalV2.x, portalV3.x, portalV4.x);
        float minY = Mathf.Min(portalV1.y, portalV2.y, portalV3.y, portalV4.y);
        float maxX = Mathf.Max(portalV1.x, portalV2.x, portalV3.x, portalV4.x);
        float maxY = Mathf.Max(portalV1.y, portalV2.y, portalV3.y, portalV4.y);

        var camInPortal = transform.InverseTransformPoint(nearPlaneTransform.position);

        if (camInPortal.x > maxX || camInPortal.x < minX || camInPortal.y > maxY || camInPortal.y < minY)
        {
            PortalNearPlane.enabled = false;
            return;
        }

        var dimensions = NearPlaneDimensions(camera, distance);

        var v1 = nearPlaneTransform.TransformPoint(new Vector3(dimensions.xMin, dimensions.yMin, 0f));
        var v2 = nearPlaneTransform.TransformPoint(new Vector3(dimensions.xMax, dimensions.yMin, 0f));
        var v3 = nearPlaneTransform.TransformPoint(new Vector3(dimensions.xMax, dimensions.yMax, 0f));
        var v4 = nearPlaneTransform.TransformPoint(new Vector3(dimensions.xMin, dimensions.yMax, 0f));
        var v5 = v1;

        Vector3[] vertices = new Vector3[] { v1, v2, v3, v4, v5 };
        bool hasDiffSign = false;

        for (var i = 0; i < vertices.Length; i++)
        {
            var vert = vertices[i];

            float sign = Mathf.Sign(plane.GetDistanceToPoint(vert));

            if (sign != camSign)
            {
                hasDiffSign = true;
            }
        }

        PortalNearPlane.enabled = hasDiffSign;

        if (!hasDiffSign)
        {
            return;
        }

        Vector3[] originals = new Vector3[] { v1, v2, v3, v4, v5 };
        _nearPlaneMesh.triangles = new int[] { 2, 1, 0, 3, 2, 0 };

        for (var i = 0; i < 4; i++)
        {
            var prevIndex = Mod(i - 1, 4);
            var nextIndex = Mod(i + 1, 4);

            var prev = originals[prevIndex];
            var next = originals[nextIndex];
            var current = originals[i];

            var prevSign = Mathf.Sign(plane.GetDistanceToPoint(prev));
            var nextSign = Mathf.Sign(plane.GetDistanceToPoint(next));
            var currentSign = Mathf.Sign(plane.GetDistanceToPoint(current));

            if (currentSign != camSign)
            {
                continue;
            }

            bool alreadyMoved = false;

            if (currentSign != prevSign)
            {
                var ray = new Ray(current, (prev - current).normalized);

                if (plane.Raycast(ray, out var prevEnter))
                {
                    vertices[i] = ray.origin + ray.direction * prevEnter;

                    alreadyMoved = true;
                }
            }

            if (currentSign != nextSign)
            {
                var ray = new Ray(current, (next - current).normalized);

                if (plane.Raycast(ray, out var nextEnter))
                {
                    var index = i;

                    if (alreadyMoved)
                    {
                        index = 4;
                        _nearPlaneMesh.triangles = new int[] { 2, 1, 0, 3, 2, 0, i, nextIndex, 4 };
                    }

                    vertices[index] = ray.origin + ray.direction * nextEnter;

                    alreadyMoved = true;
                }
            }

            if (!alreadyMoved)
            {
                var cross = originals[Mod(i + 2, 4)];

                var crossSign = Mathf.Sign(plane.GetDistanceToPoint(cross));

                if (crossSign == currentSign)
                {
                    continue;
                }

                var crossRay = new Ray(current, (cross - current).normalized);

                if (plane.Raycast(crossRay, out var crossEnter))
                {
                    vertices[i] = crossRay.origin + crossRay.direction * crossEnter;
                    vertices[4] = vertices[i];
                }
            }
        }

        v1 = vertices[0];
        v2 = vertices[1];
        v3 = vertices[2];
        v4 = vertices[3];
        v5 = vertices[4];

        v1 = nearPlaneTransform.InverseTransformPoint(v1);
        v2 = nearPlaneTransform.InverseTransformPoint(v2);
        v3 = nearPlaneTransform.InverseTransformPoint(v3);
        v4 = nearPlaneTransform.InverseTransformPoint(v4);
        v5 = nearPlaneTransform.InverseTransformPoint(v5);

        Vector3[] _vertices = new Vector3[] { v1, v2, v3, v4, v5 };

        _nearPlaneMesh.SetVertices(_vertices);

        _nearPlaneMesh.RecalculateBounds();
    }

    private static int Mod(int a, int b)
    {
        return a - b * Mathf.FloorToInt((float)a / (float)b);
    }

    private static Rect NearPlaneDimensions(Camera cam, float distance)
    {
        Rect r = new Rect();
        float a = distance;//get length
        float A = cam.fieldOfView * 0.5f;//get angle
        A = A * Mathf.Deg2Rad;//convert tor radians
        float h = (Mathf.Tan(A) * a);//calc height
        float w = (h / cam.pixelHeight) * cam.pixelWidth;//deduct width

        r.xMin = -w;
        r.xMax = w;
        r.yMin = -h;
        r.yMax = h;
        return r;
    }

    private static Vector3 CalculateScaleDifference(Transform portal, Transform other)
    {
        var selfScale = portal.lossyScale;
        var otherScale = other.lossyScale;

        var scaleDifference = new Vector3(selfScale.x / otherScale.x, selfScale.y / otherScale.y, selfScale.z / otherScale.z);

        return scaleDifference;
    }

    private static Vector4 CalculatePortalClipPlane(Transform otherPortalTransform, Transform portalCameraTransform, Camera portalCamera)
    {
        float sign = Math.Sign(Vector3.Dot(otherPortalTransform.forward, otherPortalTransform.position - portalCameraTransform.position));

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