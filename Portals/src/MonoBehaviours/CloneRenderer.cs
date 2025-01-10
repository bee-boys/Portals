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
        // If theres no renderers in the original, why bother looking?
        if (!original.GetComponentInChildren<Renderer>(true)) 
        {
            // Don't DestroyImmediate as this breaks the child check
            Destroy(clone.gameObject);
            return;
        }

        // Check for LODs
        var name = original.name;
        var lodNameIndex = name.LastIndexOf("_LOD");

        if (lodNameIndex != -1)
        {
            var numberIndex = lodNameIndex + 4;

            var substring = name[numberIndex..];

            // Ignore all LODs except for the first one
            if (int.TryParse(substring, out var lodNumber) && lodNumber > 0)
            {
                return;
            }
        }

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

            childClone.gameObject.SetActive(true);

            GetRenderers(childClone, childOriginal);
        }
    }

    private void CopyMeshRenderer(Transform clone, MeshRenderer renderer, MeshFilter filter)
    {
        if (IsMaterialBlacklisted(renderer.sharedMaterial))
        {
            return;
        }

        var cloneFilter = clone.gameObject.AddComponent<MeshFilter>();

        cloneFilter.sharedMesh = filter.sharedMesh;

        var cloneRenderer = clone.gameObject.AddComponent<MeshRenderer>();

        cloneRenderer.shadowCastingMode = renderer.shadowCastingMode;
        cloneRenderer.sharedMaterials = renderer.sharedMaterials;
        cloneRenderer.enabled = renderer.enabled;

        _originalMeshRenderers.Add(renderer);
        _originalToCloneMeshRenderer[renderer] = cloneRenderer;
    }

    private void CopySkinnedMeshRenderer(Transform clone, SkinnedMeshRenderer renderer)
    {
        if (IsMaterialBlacklisted(renderer.sharedMaterial))
        {
            return;
        }

        var cloneFilter = clone.gameObject.AddComponent<MeshFilter>();
        var cloneRenderer = clone.gameObject.AddComponent<MeshRenderer>();

        cloneRenderer.sharedMaterials = renderer.sharedMaterials;
        cloneRenderer.enabled = renderer.enabled;
        cloneRenderer.shadowCastingMode = renderer.shadowCastingMode;

        cloneFilter.sharedMesh = new Mesh();

        _originalSkinnedRenderers.Add(renderer);
        _skinnedToCloneFilter[renderer] = cloneFilter;
        _skinnedToCloneRenderer[renderer] = cloneRenderer;
    }

    private static bool IsMaterialBlacklisted(Material material)
    {
        // Ignore null materials
        if (material == null)
        {
            return false;
        }

        // Don't copy interactable icons
        if (material.shader.name == "SLZ/Icon Billboard")
        {
            return true;
        }

        return false;
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

            if (!CheckActive(cloneRenderer, renderer))
            {
                continue;
            }

            cloneRenderer.shadowCastingMode = renderer.shadowCastingMode;
            cloneRenderer.sharedMaterials = renderer.sharedMaterials;

            // Update positions
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

            var cloneRenderer = _skinnedToCloneRenderer[renderer];

            if (!CheckActive(cloneRenderer, renderer))
            {
                continue;
            }

            var meshFilter = _skinnedToCloneFilter[renderer];

            cloneRenderer.shadowCastingMode = renderer.shadowCastingMode;
            cloneRenderer.sharedMaterials = renderer.sharedMaterials;

            // Update positions
            var cloneTransform = meshFilter.transform;
            var originalTransform = renderer.transform;

            var newPosition = _cloneTransform.TransformPoint(_originalTransform.InverseTransformPoint(originalTransform.position));
            var newRotation = _cloneTransform.rotation * (Quaternion.Inverse(_originalTransform.rotation) * originalTransform.rotation);

            cloneTransform.SetPositionAndRotation(newPosition, newRotation);

            // Bake the skinned mesh onto the mesh renderer
            renderer.BakeMesh(meshFilter.mesh, true);
        }
    }

    private bool CheckActive(Renderer clone, Renderer original)
    {
        bool active = original.gameObject.activeInHierarchy;

        if (!active)
        {
            clone.enabled = false;
            return false;
        }

        bool enabled = original.enabled;

        if (!enabled)
        {
            clone.enabled = false;
            return false;
        }

        clone.enabled = true;
        return true;
    }
}