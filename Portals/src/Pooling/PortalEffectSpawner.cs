using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;

using Portals.AssetWarehouse;
using Portals.MonoBehaviours;

using UnityEngine;

namespace Portals.Pooling;

public static class PortalEffectSpawner
{
    public static void ShootProjectile(PortalProjectile.ProjectileData data)
    {
        var crateReference = PortalCrateReferences.PortalProjectileReference;

        var spawnable = new Spawnable()
        {
            crateRef = crateReference,
            policyData = null,
        };

        AssetSpawner.Register(spawnable);

        var spawnTask = AssetSpawner.SpawnAsync(spawnable, data.Position, Quaternion.LookRotation(data.Direction), new(Vector3.one), null, false, new(0), null, null);
        var awaiter = spawnTask.GetAwaiter();

        var continuation = () =>
        {
            var result = awaiter.GetResult();

            if (result == null)
            {
                return;
            }

            var projectile = result.GetComponent<PortalProjectile>();

            projectile.Fire(data);
        };
        awaiter.OnCompleted(continuation);
    }

    public static void PlayImpactEffect(Vector3 position, Quaternion rotation, Color color)
    {
        var crateReference = PortalCrateReferences.PortalImpactReference;

        var spawnable = new Spawnable()
        {
            crateRef = crateReference,
            policyData = null,
        };

        AssetSpawner.Register(spawnable);

        var spawnTask = AssetSpawner.SpawnAsync(spawnable, position, rotation, new(Vector3.one), null, false, new(0), null, null);
        var awaiter = spawnTask.GetAwaiter();

        var continuation = () =>
        {
            var result = awaiter.GetResult();

            if (result == null)
            {
                return;
            }

            var particleSystem = result.GetComponentInParent<ParticleSystem>();

            particleSystem.startColor = color;
            particleSystem.Play();
        };
        awaiter.OnCompleted(continuation);
    }
}
