using UnityEngine;

using MelonLoader;

using System.Collections.Generic;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class CloneRenderer : MonoBehaviour
{
    public Transform CloneTransform => _cloneTransform;
    public Transform OriginalTransform => _originalTransform;

    private Transform _cloneTransform = null;
    private Transform _originalTransform = null;

    private Dictionary<LODGroup, LODGroup> _originalToCloneLodGroup = new();

    private List<SkinnedMeshRenderer> _originalSkinnedRenderers = new();
    private Dictionary<SkinnedMeshRenderer, MeshRenderer> _skinnedToCloneRenderer = new();
    private Dictionary<SkinnedMeshRenderer, MeshFilter> _skinnedToCloneFilter = new();

    private List<MeshRenderer> _originalMeshRenderers = new();
    private Dictionary<MeshRenderer, MeshRenderer> _originalToCloneMeshRenderer = new();

    public void Initialize(GameObject original)
    {
        _cloneTransform = transform;
        _originalTransform = original.transform;

        GetRenderers(transform, original.transform);
    }

    private void GetRenderers(Transform clone, Transform original)
    {
        var meshFilter = original.GetComponent<MeshFilter>();
        var meshRenderer = original.GetComponent<MeshRenderer>();

        if (meshFilter && meshRenderer)
        {
            CopyMeshRenderer(clone, meshRenderer, meshFilter);
        }

        var skinnedMeshRenderer = original.GetComponent<SkinnedMeshRenderer>();

        if (skinnedMeshRenderer)
        {
            CopySkinnedMeshRenderer(clone, skinnedMeshRenderer);
        }

        for (var i = 0; i < clone.childCount; i++)
        {
            var childClone = clone.GetChild(i);
            var childOriginal = original.GetChild(i);

            GetRenderers(childClone, childOriginal);
        }
    }

    private void CopyMeshRenderer(Transform clone, MeshRenderer renderer, MeshFilter filter)
    {
        var cloneFilter = clone.gameObject.AddComponent<MeshFilter>();

        cloneFilter.sharedMesh = filter.sharedMesh;

        var cloneRenderer = clone.gameObject.AddComponent<MeshRenderer>();

        cloneRenderer.shadowCastingMode = renderer.shadowCastingMode;
        cloneRenderer.sharedMaterials = renderer.sharedMaterials;

        _originalMeshRenderers.Add(renderer);
        _originalToCloneMeshRenderer[renderer] = cloneRenderer;
    }

    private void CopySkinnedMeshRenderer(Transform clone, SkinnedMeshRenderer renderer)
    {
        var cloneFilter = clone.gameObject.AddComponent<MeshFilter>();
        var cloneRenderer = clone.gameObject.AddComponent<MeshRenderer>();

        cloneRenderer.sharedMaterials = renderer.sharedMaterials;
        cloneFilter.sharedMesh = new Mesh();

        _originalSkinnedRenderers.Add(renderer);
        _skinnedToCloneFilter[renderer] = cloneFilter;
        _skinnedToCloneRenderer[renderer] = cloneRenderer;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnCloneUpdate()
    {
        OnUpdateMeshRenderers();

        OnUpdateSkinnedRenderers();
    }

    private void OnUpdateMeshRenderers()
    {
        foreach (var renderer in _originalMeshRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            var cloneRenderer = _originalToCloneMeshRenderer[renderer];

            var cloneTransform = cloneRenderer.transform;
            var originalTransform = renderer.transform;

            var newPosition = _cloneTransform.TransformPoint(_originalTransform.InverseTransformPoint(originalTransform.position));
            var newRotation = _cloneTransform.rotation * (Quaternion.Inverse(_originalTransform.rotation) * originalTransform.rotation);

            cloneTransform.SetPositionAndRotation(newPosition, newRotation);
        }
    }

    private void OnUpdateSkinnedRenderers()
    {
        foreach (var renderer in _originalSkinnedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            var meshFilter = _skinnedToCloneFilter[renderer];

            var cloneTransform = meshFilter.transform;
            var originalTransform = renderer.transform;

            var newPosition = _cloneTransform.TransformPoint(_originalTransform.InverseTransformPoint(originalTransform.position));
            var newRotation = _cloneTransform.rotation * (Quaternion.Inverse(_originalTransform.rotation) * originalTransform.rotation);

            cloneTransform.SetPositionAndRotation(newPosition, newRotation);

            renderer.BakeMesh(meshFilter.mesh, true);
        }
    }
}