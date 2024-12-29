using Portals.MonoBehaviours;

using UnityEngine;

namespace Portals.Rendering;

public static class CloneCreator
{
    public static Transform TempCloningTransform => _tempCloningTransform;

    private static GameObject _tempCloningParent = null;
    private static Transform _tempCloningTransform = null;

    public static void OnLevelLoaded()
    {
        ValidateTempParent();
    }

    private static void ValidateTempParent()
    {
        if (_tempCloningParent != null)
        {
            return;
        }

        _tempCloningParent = new GameObject("Temp Cloning Parent");
        _tempCloningParent.SetActive(false);

        _tempCloningTransform = _tempCloningParent.transform;
    }

    public static CloneRenderer CreateClone(GameObject root)
    {
        ValidateTempParent();

        var clone = GameObject.Instantiate(root, _tempCloningTransform);
        clone.SetActive(false);
        clone.transform.parent = null;

        Strip(clone);

        var renderer = clone.AddComponent<CloneRenderer>();
        renderer.Initialize(root);

        return renderer;
    }

    private static void Strip(GameObject root)
    {
        // Multiple times to make sure no dependent scripts are missed
        for (var i = 0; i < 8; i++)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component.TryCast<Transform>())
                {
                    continue;
                }

                GameObject.DestroyImmediate(component);
            }
        }
    }
}
