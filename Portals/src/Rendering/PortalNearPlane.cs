using Il2CppInterop.Runtime.InteropTypes.Arrays;

using UnityEngine;

namespace Portals.Rendering;

public class PortalNearPlane
{
    public MeshFilter MeshFilter { get; set; }

    public MeshRenderer MeshRenderer { get; set; }

    public GameObject GameObject { get; set; }

    public Transform Transform { get; set; }

    public Mesh Mesh { get; set; }

    public Material Material { get; set; }

    private Il2CppStructArray<Vector3> _frustumCorners = new Vector3[4];

    private Vector3[] _vertices = new Vector3[5];

    public PortalNearPlane(Shader shader)
    {
        GameObject = new GameObject("Portal Near Plane");
        Transform = GameObject.transform;

        MeshFilter = GameObject.AddComponent<MeshFilter>();
        MeshRenderer = GameObject.AddComponent<MeshRenderer>();

        Mesh = new Mesh();

        MeshFilter.sharedMesh = Mesh;
        Material = new Material(shader);

        MeshRenderer.sharedMaterial = Material;
    }

    public void Destroy()
    {
        GameObject.Destroy(GameObject);
    }

    public void Hide()
    {
        MeshRenderer.enabled = false;
    }

    public void Render(Portal portal, Camera camera, Camera.MonoOrStereoscopicEye eye)
    {
        var portalTransform = portal.transform;
        var cameraTransform = camera.transform;

        var plane = new Plane(portalTransform.forward, portalTransform.position);

        float camSign = Mathf.Sign(plane.GetDistanceToPoint(cameraTransform.position));

        float distance = camera.nearClipPlane + 0.01f;

        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), distance, Camera.MonoOrStereoscopicEye.Mono, _frustumCorners);

        var viewMatrix = camera.worldToCameraMatrix;

        if (eye == Camera.MonoOrStereoscopicEye.Left)
        {
            viewMatrix = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
        }
        else if (eye == Camera.MonoOrStereoscopicEye.Right)
        {
            viewMatrix = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
        }

        var viewToWorldMatrix = viewMatrix.inverse;

        var bottomLeft = _frustumCorners[0];
        var bottomRight = _frustumCorners[3];
        var topRight = _frustumCorners[2];
        var topLeft = _frustumCorners[1];

        bottomLeft.z = -bottomLeft.z;
        bottomRight.z = -bottomRight.z;
        topRight.z = -topRight.z;
        topLeft.z = -topLeft.z;

        var v1 = viewToWorldMatrix.MultiplyPoint(bottomLeft);
        var v2 = viewToWorldMatrix.MultiplyPoint(bottomRight);
        var v3 = viewToWorldMatrix.MultiplyPoint(topRight);
        var v4 = viewToWorldMatrix.MultiplyPoint(topLeft);
        var v5 = v1;

        var center = (v1 + v2 + v3 + v4) / 4f;

        Transform.position = center;
        Transform.rotation = cameraTransform.rotation;

        var portalExtents = portal.Size * 0.5f;
        var portalV1 = new Vector3(-portalExtents.x, -portalExtents.y);
        var portalV2 = new Vector3(portalExtents.x, -portalExtents.y);
        var portalV3 = new Vector3(portalExtents.x, portalExtents.y);
        var portalV4 = new Vector3(-portalExtents.x, portalExtents.y);

        float minX = Mathf.Min(portalV1.x, portalV2.x, portalV3.x, portalV4.x);
        float minY = Mathf.Min(portalV1.y, portalV2.y, portalV3.y, portalV4.y);
        float maxX = Mathf.Max(portalV1.x, portalV2.x, portalV3.x, portalV4.x);
        float maxY = Mathf.Max(portalV1.y, portalV2.y, portalV3.y, portalV4.y);

        var camInPortal = portalTransform.InverseTransformPoint(Transform.position);

        if (camInPortal.x > maxX || camInPortal.x < minX || camInPortal.y > maxY || camInPortal.y < minY)
        {
            MeshRenderer.enabled = false;
            return;
        }

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

        MeshRenderer.enabled = hasDiffSign;

        if (!hasDiffSign)
        {
            return;
        }

        Vector3[] originals = new Vector3[] { v1, v2, v3, v4, v5 };
        int[] triangles = new int[] { 2, 1, 0, 3, 2, 0 };

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
                        triangles = new int[] { 2, 1, 0, 3, 2, 0, i, nextIndex, 4 };
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

        v1 = Transform.InverseTransformPoint(v1);
        v2 = Transform.InverseTransformPoint(v2);
        v3 = Transform.InverseTransformPoint(v3);
        v4 = Transform.InverseTransformPoint(v4);
        v5 = Transform.InverseTransformPoint(v5);

        _vertices[0] = v1;
        _vertices[1] = v2;
        _vertices[2] = v3;
        _vertices[3] = v4;
        _vertices[4] = v5;

        Mesh.SetVertices(_vertices);
        Mesh.triangles = triangles;

        Mesh.RecalculateBounds();
    }

    private static int Mod(int a, int b)
    {
        return a - b * Mathf.FloorToInt((float)a / (float)b);
    }
}
