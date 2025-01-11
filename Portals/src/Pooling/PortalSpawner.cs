using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using Portals.AssetWarehouse;
using Portals.MonoBehaviours;

using System;

using UnityEngine;

namespace Portals.Pooling;

public static class PortalSpawner
{
    public static void Spawn(Vector3 position, Quaternion rotation, Vector2 size, PortalShape shape = PortalShape.ELLIPSE, Action<Portal> spawnCallback = null)
    {
        SpawnableCrateReference crateReference = shape switch
        {
            PortalShape.PLANE => PortalCrateReferences.PortalPlaneReference,
            _ => PortalCrateReferences.PortalEllipseReference,
        };

        var spawnable = new Spawnable()
        {
            crateRef = crateReference,
            policyData = null,
        };

        AssetSpawner.Register(spawnable);

        var scale = new Vector3(size.x, size.y, 1f);

        var spawnTask = AssetSpawner.SpawnAsync(spawnable, position, rotation, new(scale), null, false, new(0), null, null);
        var awaiter = spawnTask.GetAwaiter();

        var continuation = () =>
        {
            var result = awaiter.GetResult();

            if (result == null)
            {
                return;
            }

            var portal = result.GetComponentInChildren<Portal>();

            spawnCallback?.Invoke(portal);
        };
        awaiter.OnCompleted(continuation);
    }
}
