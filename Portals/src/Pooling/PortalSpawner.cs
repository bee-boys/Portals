using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

using Portals.AssetWarehouse;
using Portals.MonoBehaviours;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Portals.Pooling;

public static class PortalSpawner
{
    public class PortalSpawnInfo
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public Vector2 Size { get; set; } = PortalConstants.DefaultSize;

        public PortalShape Shape { get; set; } = PortalShape.ELLIPSE;


        public bool Primary { get; set; } = true;
        public int? ID { get; set; } = null;
        public bool OneSided { get; set; } = true;

        public Action<Portal> SpawnCallback { get; set; } = null;
    }

    public class PortalFizzleInfo
    {
        public bool Primary { get; set; } = false;
        public bool Secondary { get; set; } = false;
        public int ID { get; set; } = 0;
    }

    public class PortalPair
    {
        public Portal PrimaryPortal { get; set; } = null;
        public Portal SecondaryPortal { get; set; } = null;
    }

    public static readonly Dictionary<int, PortalPair> PortalLookup = new();

    public static bool Fizzle(PortalFizzleInfo info)
    {
        if (!PortalLookup.TryGetValue(info.ID, out var pair))
        {
            return false;
        }

        bool fizzled = false;

        if (info.Primary && pair.PrimaryPortal)
        {
            pair.PrimaryPortal.Fizzle();
            fizzled = true;
        }

        if (info.Secondary && pair.SecondaryPortal)
        {
            pair.SecondaryPortal.Fizzle();
            fizzled = true;
        }

        PortalLookup.Remove(info.ID);

        return fizzled;
    }

    public static void Spawn(PortalSpawnInfo info)
    {
        if (info.ID.HasValue && PortalLookup.TryGetValue(info.ID.Value, out var pair))
        {
            var existingPortal = info.Primary ? pair.PrimaryPortal : pair.SecondaryPortal;

            if (existingPortal != null)
            {
                existingPortal.Close();
            }
        }

        SpawnableCrateReference crateReference = info.Shape switch
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

        var scale = new Vector3(info.Size.x, info.Size.y, 1f);

        var spawnTask = AssetSpawner.SpawnAsync(spawnable, info.Position, info.Rotation, new(scale), null, false, new(0), null, null);
        var awaiter = spawnTask.GetAwaiter();

        var continuation = () =>
        {
            var result = awaiter.GetResult();

            if (result == null)
            {
                return;
            }

            var portal = result.GetComponentInChildren<Portal>();

            portal.Primary = info.Primary;
            portal.OneSided = info.OneSided;

            if (info.ID.HasValue)
            {
                portal.ID = info.ID.Value;

                if (!PortalLookup.TryGetValue(info.ID.Value, out var portalPair))
                {
                    portalPair = new PortalPair();
                }

                if (info.Primary)
                {
                    portalPair.PrimaryPortal = portal;
                }
                else
                {
                    portalPair.SecondaryPortal = portal;
                }

                PortalLookup[info.ID.Value] = portalPair;

                if (portalPair.PrimaryPortal && portalPair.SecondaryPortal)
                {
                    portalPair.PrimaryPortal.OtherPortal = portalPair.SecondaryPortal;
                    portalPair.SecondaryPortal.OtherPortal = portalPair.PrimaryPortal;
                }
            }

            info.SpawnCallback?.Invoke(portal);
        };
        awaiter.OnCompleted(continuation);
    }
}
