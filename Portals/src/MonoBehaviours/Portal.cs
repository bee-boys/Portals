using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using MelonLoader;

using System;

using Il2CppInterop.Runtime.InteropTypes.Fields;
using Il2CppInterop.Runtime.Attributes;

using Il2CppSLZ.Marrow.Audio;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Interaction;

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

    private Vector2 _size = Vector2.zero;

    private Matrix4x4 _cachedEnterMatrix = Matrix4x4.identity;
    private Matrix4x4 _cachedEnterMatrixInverse = Matrix4x4.identity;

    private Matrix4x4 _cachedExitMatrix = Matrix4x4.identity;
    private Matrix4x4 _cachedExitMatrixInverse = Matrix4x4.identity;
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
    public Vector2 Size => _size;

    [HideFromIl2Cpp]
    public Matrix4x4 PortalEnterMatrix => _cachedEnterMatrix;

    [HideFromIl2Cpp]
    public Matrix4x4 PortalEnterMatrixInverse => _cachedEnterMatrixInverse;

    [HideFromIl2Cpp]
    public Matrix4x4 PortalExitMatrix => _cachedExitMatrix;

    [HideFromIl2Cpp]
    public Matrix4x4 PortalExitMatrixInverse => _cachedExitMatrixInverse;

    [HideFromIl2Cpp]
    public Vector3 Velocity => ParentBody && ParentBody.HasRigidbody ? ParentBody._rigidbody.GetPointVelocity(transform.position) : Vector3.zero;

    [HideFromIl2Cpp]
    public Vector3 AngularVelocity => ParentBody && ParentBody.HasRigidbody ? ParentBody._rigidbody.angularVelocity : Vector3.zero;

    [HideFromIl2Cpp]
    public List<Collider> WallColliders { get; set; } = new();

    [HideFromIl2Cpp]
    public event Action OnClosedEvent;

    [HideFromIl2Cpp]
    public AudioClip[] FizzleSounds { get; set; } = null;

    [HideFromIl2Cpp]
    public Poolee Poolee { get; set; } = null;

    [HideFromIl2Cpp]
    public MarrowBody ParentBody { get; set; } = null;

    [HideFromIl2Cpp]
    public int? ID { get; set; } = null;

    [HideFromIl2Cpp]
    public bool Primary { get; set; } = false;

    [HideFromIl2Cpp]
    public bool OneSided { get; set; } = true;
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

        _size = size.Get();

        Expander.ToggleCollision(false);
    }

    private void Start()
    {
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            if (collider.isTrigger)
            {
                collider.gameObject.layer = PortalConstants.PortalLayer;
            }
        }

        _leftEyeCamera = new PortalCamera(this, Camera.StereoscopicEye.Left);
        _rightEyeCamera = new PortalCamera(this, Camera.StereoscopicEye.Right);

        _leftEyeNearPlane = new PortalNearPlane(Surface.AlwaysVisibleShader);
        _rightEyeNearPlane = new PortalNearPlane(Surface.AlwaysVisibleShader);

        _leftEyeNearPlane.Material.SetFloat(PortalShaderConstants.TargetEyeId, 0f);
        _rightEyeNearPlane.Material.SetFloat(PortalShaderConstants.TargetEyeId, 1f);

        _leftEyeCamera.OnTargetTextureChanged += OnLeftEyeTextureChanged;
        _rightEyeCamera.OnTargetTextureChanged += OnRightEyeTextureChanged;

        OnLeftEyeTextureChanged(_leftEyeCamera.TargetTexture);
        OnRightEyeTextureChanged(_rightEyeCamera.TargetTexture);

        Surface.CorridorRenderer.enabled = false;

        RenderingHooks.PreBeginCameraRendering += OnPreBeginCameraRendering;
        RenderingHooks.BeginCameraRendering += OnBeginCameraRendering;

        Poolee = GetComponentInParent<Poolee>();
    }

    public void CollectWallColliders()
    {
        WallColliders.Clear();

        // If this portal is attached to a rigidbody, then just use the rigidbody's colliders
        if (ParentBody != null)
        {
            foreach (var collider in ParentBody.Colliders)
            {
                if (collider != null)
                {
                    WallColliders.Add(collider);
                }
            }

            return;
        }

        var portalPosition = transform.position;
        var portalRotation = transform.rotation;
        var portalScale = transform.lossyScale;

        var size = new Vector2(Size.x * portalScale.x, Size.y * portalScale.y);

        var overlapBox = Physics.OverlapBox(portalPosition, new Vector3(size.x * 0.5f, size.y * 0.5f, 2f), portalRotation, PortalConstants.HitMask, QueryTriggerInteraction.Ignore);

        foreach (var hit in overlapBox)
        {
            if (hit.attachedRigidbody)
            {
                continue;
            }

            if (hit.GetComponentInParent<Portal>())
            {
                continue;
            }

            WallColliders.Add(hit);
        }
    }

    private void LateUpdate()
    {
        var position = transform.position;
        var rotation = transform.rotation;
        var scale = GetFlatScale(transform.lossyScale);

        // Update matrices
        _cachedEnterMatrix = Matrix4x4.TRS(position, rotation, scale);
        _cachedEnterMatrixInverse = _cachedEnterMatrix.inverse;

        _cachedExitMatrix = Matrix4x4.TRS(position, Quaternion.AngleAxis(180f, transform.up) * rotation, scale);
        _cachedExitMatrixInverse = _cachedExitMatrix.inverse;
    }

    private void OnLeftEyeTextureChanged(RenderTexture texture)
    {
        Surface.SurfaceMaterial.SetTexture(PortalShaderConstants.LeftEyeTextureId, texture);
        _leftEyeNearPlane.Material.SetTexture(PortalShaderConstants.MainTextureId, texture);
    }

    private void OnRightEyeTextureChanged(RenderTexture texture)
    {
        Surface.SurfaceMaterial.SetTexture(PortalShaderConstants.RightEyeTextureId, texture);
        _rightEyeNearPlane.Material.SetTexture(PortalShaderConstants.MainTextureId, texture);
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

        RenderingHooks.PreBeginCameraRendering -= OnPreBeginCameraRendering;
        RenderingHooks.BeginCameraRendering -= OnBeginCameraRendering;
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
        Surface.BackRenderer.enabled = !OneSided;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext src, Camera cam)
    {
        if (!PortalPreferences.RenderView)
        {
            Surface.SurfaceMaterial.SetFloat(PortalShaderConstants.OpenId, 0f);
            return;
        }

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

        // Check for render distance
        float openMultiplier = 1f;

        if (PortalPreferences.LimitDistance)
        {
            var scale = transform.lossyScale;
            var size = new Vector2(scale.x * Size.x, scale.y * Size.y);

            var radius = Math.Max(size.x, size.y);

            var distance = (cam.transform.position - transform.position).magnitude;

            openMultiplier = 1f - Math.Clamp(Math.Max(0f, distance - radius) / Math.Max(radius, PortalPreferences.RenderDistance - radius), 0f, 1f);

            // If the portal isn't open, don't bother rendering
            if (openMultiplier <= 0f)
            {
                Surface.SurfaceMaterial.SetFloat(PortalShaderConstants.OpenId, 0f);
                return;
            }
        }

        int iterations = PortalPreferences.MaxRecursion;
        int initialValue = iterations - 1;
        float openPercent = openMultiplier * Surface.OpenPercent;

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

            Surface.SurfaceMaterial.SetInt(PortalShaderConstants.ForceEyeId, 1);

            var (left, right) = GetEyes();

            var scaleDifference = CalculateScaleDifference(PortalEnterMatrix, OtherPortal.PortalExitMatrix);

            var offsetMatrix = Matrix4x4.TRS(newPosition, newRotation, Matrix4x4.Inverse(Matrix4x4.Scale(scaleDifference)) * mainCameraTransform.lossyScale);

            var leftEyeWorld = offsetMatrix.MultiplyPoint(left);
            var rightEyeWorld = offsetMatrix.MultiplyPoint(right);

            _leftEyeCamera.Transform.SetPositionAndRotation(leftEyeWorld, newRotation);
            _leftEyeCamera.Camera.projectionMatrix = CalculateEyeProjectionMatrix(mainCamera, Camera.StereoscopicEye.Left, centerSign, otherPortalTransform, _leftEyeCamera.Transform, _leftEyeCamera.Camera);

            Surface.SurfaceMaterial.SetFloat(PortalShaderConstants.EyeOverrideId, 0f);

            UniversalRenderPipeline.RenderSingleCamera(src, _leftEyeCamera.Camera);

            _rightEyeCamera.Transform.SetPositionAndRotation(rightEyeWorld, newRotation);
            _rightEyeCamera.Camera.projectionMatrix = CalculateEyeProjectionMatrix(mainCamera, Camera.StereoscopicEye.Right, centerSign, otherPortalTransform, _rightEyeCamera.Transform, _rightEyeCamera.Camera);

            Surface.SurfaceMaterial.SetFloat(PortalShaderConstants.EyeOverrideId, 1f);

            UniversalRenderPipeline.RenderSingleCamera(src, _rightEyeCamera.Camera);

            Surface.SurfaceMaterial.SetInt(PortalShaderConstants.ForceEyeId, 0);

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
        to.cullingMask = from.cullingMask;
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
            var forward = transform.forward;
            var position = transform.position;

            float centerSign = Math.Sign(Vector3.Dot(forward, position - camera.transform.position));

            if (centerSign <= 0f)
            {
                Surface.CorridorPivot.localRotation = Quaternion.identity;
            }
            else
            {
                Surface.CorridorPivot.localRotation = Quaternion.AngleAxis(180f, Vector3.up);
            }

            var (left, right) = GetEyes();

            var cameraMatrix = camera.transform.localToWorldMatrix;

            float leftEyeSign = Math.Sign(Vector3.Dot(forward, position - cameraMatrix.MultiplyPoint3x4(left)));
            float rightEyeSign = Math.Sign(Vector3.Dot(forward, position - cameraMatrix.MultiplyPoint3x4(right)));

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