using Portals.MonoBehaviours;

using UnityEngine;

namespace Portals.Rendering;

public static class CloneCreator
{
    public static Transform DisabledCloningTransform => _disabledCloningTransform;

    private static GameObject _disabledCloningGameObject = null;
    private static Transform _disabledCloningTransform = null;

    public static void OnLevelLoaded()
    {
        ValidateTempParent();
    }

    private static void ValidateTempParent()
    {
        if (_disabledCloningGameObject != null)
        {
            return;
        }

        _disabledCloningGameObject = new GameObject("Disabled Cloning Parent");
        _disabledCloningGameObject.SetActive(false);

        _disabledCloningTransform = _disabledCloningGameObject.transform;
    }

    public static CloneRenderer CreateCloneRenderer(GameObject reference)
    {
        var clone = CreateCloneGameObject(reference);

        var renderer = clone.AddComponent<CloneRenderer>();
        renderer.Initialize(reference);

        return renderer;
    }

    private static GameObject CreateCloneGameObject(GameObject reference)
    {
        ValidateTempParent();

        var clone = GameObject.Instantiate(reference, DisabledCloningTransform);
        clone.SetActive(false);
        clone.transform.parent = null;

        Strip(clone);

        clone.name = GetCloneName(reference.name);
        return clone;
    }

    private static string GetCloneName(string original) => $"{original} (Portal Renderer)";

    private static void Strip(GameObject root)
    {
        var components = root.GetComponentsInChildren<Component>(true);

        // Multiple times to make sure no dependent scripts are missed
        for (var i = 0; i < 8; i++)
        {
            int count = 0;

            foreach (var component in components)
            {
                if (component == null)
                {
                    continue;
                }

                if (component.TryCast<Transform>())
                {
                    continue;
                }

                count++;

                GameObject.DestroyImmediate(component);
            }

            if (count <= 0)
            {
                break;
            }
        }
    }
}
