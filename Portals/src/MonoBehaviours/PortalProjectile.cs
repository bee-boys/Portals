using System;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Audio;

using Il2CppInterop.Runtime.Attributes;

using Portals.Pooling;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class PortalProjectile : MonoBehaviour
{
    public PortalProjectile(IntPtr intPtr) : base(intPtr) { }

    public struct ProjectileData
    {
        public Vector3 Position { get; set; }

        public Vector3 Direction { get; set; }

        public Vector3 Velocity { get; set; }

        public Color Color { get; set; }

        public PortalSpawner.PortalSpawnInfo SpawnInfo { get; set; }

        public float MaxTime { get; set; }

        public AudioClip[] InvalidSounds { get; set; }
    }

    private ProjectileData _projectileData = default;
    private Poolee _poolee = null;

    private bool _shot = false;
    private float _travelTimer = 0f;
    private float _despawnTimer = 0f;

    private ParticleSystem[] _particleSystems = null;
    private TrailRenderer[] _trailRenderers = null;

    private void Awake()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
        _trailRenderers = GetComponentsInChildren<TrailRenderer>();
    }

    private void Start()
    {
        _poolee = GetComponent<Poolee>();
    }

    private void OnDisable()
    {
        _shot = false;
        _travelTimer = 0f;
        _despawnTimer = 0f;
    }

    public void Fire(ProjectileData data)
    {
        _projectileData = data;

        transform.position = data.Position;
        transform.rotation = Quaternion.LookRotation(data.Direction);

        _travelTimer = data.MaxTime;

        var color = data.Color;

        var intensity = (color.r + color.g + color.b) / 3f;

        color = new Color(color.r / intensity, color.g / intensity, color.b / intensity, color.a);

        foreach (var particleSystem in _particleSystems)
        {
            particleSystem.startColor = color;

            particleSystem.Clear();
            particleSystem.Play();
        }

        foreach (var trailRenderer in _trailRenderers)
        {
            trailRenderer.startColor = color;
            trailRenderer.endColor = Color.clear;

            trailRenderer.time = 0.5f;

            trailRenderer.Clear();
        }

        _shot = true;
    }

    public void Stop()
    {
        _shot = false;

        foreach (var particleSystem in _particleSystems)
        {
            particleSystem.Stop();
        }

        foreach (var trailRenderer in _trailRenderers)
        {
            trailRenderer.time = 0.01f;
        }

        _despawnTimer = 1f;
    }

    public void FixedUpdate()
    {
        var deltaTime = Time.fixedDeltaTime;

        if (!_shot)
        {
            _despawnTimer -= deltaTime;

            if (_despawnTimer <= 0f )
            {
                Despawn();
            }

            return;
        }

        _travelTimer -= deltaTime;

        if (_travelTimer <= 0f)
        {
            EndingRaycast();
            return;
        }

        var startPosition = transform.position;
        var endPosition = startPosition + _projectileData.Velocity * deltaTime;

        transform.position = endPosition;

        if (Physics.Linecast(startPosition, endPosition, out var hitInfo, PortalConstants.HitMask, QueryTriggerInteraction.Ignore))
        { 
            CheckHit(hitInfo);
            return;
        }
    }

    public void Despawn()
    {
        if (_poolee)
        {
            _poolee.Despawn();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void EndingRaycast()
    {
        var startPosition = transform.position;
        var direction = _projectileData.Direction;

        if (!Physics.Raycast(startPosition, direction, out var hitInfo, float.PositiveInfinity, PortalConstants.HitMask, QueryTriggerInteraction.Ignore))
        {
            Stop();
            return;
        }

        CheckHit(hitInfo);
    }

    private void CheckHit(RaycastHit hitInfo)
    {
        var normal = hitInfo.normal;
        var uphill = Vector3.Cross(normal, Vector3.Cross(normal, Vector3.down));

        var position = hitInfo.point + normal * 0.001f;

        var up = uphill;

        float dot = Vector3.Dot(normal, Vector3.up);

        if (dot > 0.99f || dot < -0.99f)
        {
            up = _projectileData.Direction;
        }

        var rotation = Quaternion.LookRotation(normal, up);

        if (hitInfo.rigidbody)
        {
            OnInvalidHit(position, rotation);
            return;
        }

        var collider = hitInfo.collider;

        if (collider.GetComponentInParent<Portal>() || collider.GetComponent<PortalBlocker>())
        {
            OnInvalidHit(position, rotation);
            return;
        }

        TrySpawn(position, rotation);
        return;
    }

    private void OnInvalidHit(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        PortalEffectSpawner.PlayImpactEffect(position, rotation, _projectileData.Color);

        var invalidSounds = _projectileData.InvalidSounds;

        if (invalidSounds != null)
        {
            Audio3dManager.PlayAtPoint(invalidSounds, position, Audio3dManager.hardInteraction, 1f, 1f, new(0f), new(0.4f), new(1f));
        }

        Stop();
    }
    
    private unsafe void TrySpawn(Vector3 position, Quaternion rotation)
    {
        var normal = rotation * Vector3.forward;

        var spawnInfo = _projectileData.SpawnInfo;

        var extents = spawnInfo.Size * 0.5f;
        var offsetExtents = new Vector3(extents.x, extents.y, 0.01f);

        var points = stackalloc Vector3[9]
        {
            // Sides
            new Vector3(-offsetExtents.x, 0f, offsetExtents.z),
            new Vector3(offsetExtents.x, 0f, offsetExtents.z),
            new Vector3(0f, offsetExtents.y, offsetExtents.z),
            new Vector3(0f, -offsetExtents.y, offsetExtents.z),

            // Corners
            new Vector3(-offsetExtents.x, -offsetExtents.y, offsetExtents.z),
            new Vector3(-offsetExtents.x, offsetExtents.y, offsetExtents.z),
            new Vector3(offsetExtents.x, offsetExtents.y, offsetExtents.z),
            new Vector3(offsetExtents.x, -offsetExtents.y, offsetExtents.z),

            // Center
            new Vector3(0f, 0f, offsetExtents.z),
        };

        Vector3 newPosition = position;

        MoveToAvailableArea(ref newPosition, rotation, points, 9);

        if (!CheckSurface(newPosition, rotation, points, 9))
        {
            OnInvalidHit(position, rotation);
            return;
        }

        position = newPosition;

        transform.SetPositionAndRotation(position, rotation);

        spawnInfo.Position = position;
        spawnInfo.Rotation = rotation;

        PortalSpawner.Spawn(spawnInfo);
        Stop();
    }

    private unsafe void MoveToAvailableArea(ref Vector3 position, Quaternion rotation, Vector3* points, int count)
    {
        for (var i = 0; i < count; i++)
        {
            // Count is center
            if (i == count - 1)
            {
                continue;
            }

            var point = points[i];
            var worldPoint = position + rotation * point;

            var oppositePoint = point;
            oppositePoint.z = -oppositePoint.z;

            var worldOppositePoint = position + rotation * oppositePoint;

            var flatPoint = point;
            flatPoint.z = 0f;

            var worldFlatPoint = position + rotation * flatPoint;

            var center = points[count - 1];
            var worldCenter = position + rotation * center;

            var vector = worldPoint - worldCenter;
            var direction = vector.normalized;

            // Side check
            if (Physics.Linecast(worldCenter, worldPoint, out var hitInfo, PortalConstants.HitMask, QueryTriggerInteraction.Ignore) && !hitInfo.rigidbody && !hitInfo.collider.GetComponentInParent<Portal>())
            {
                float dot = Vector3.Dot(direction, (worldPoint - hitInfo.point).normalized);

                // Needs to go towards the center, not away from it
                if (dot > 0.8f)
                {
                    position += (hitInfo.point - worldPoint) * 1.02f; // Extra space
                    continue;
                }
            }
            
            // Normal check
            if (!Physics.Linecast(worldPoint, worldOppositePoint, PortalConstants.HitMask, QueryTriggerInteraction.Ignore) || CheckForPortal(worldFlatPoint))
            {
                var smallVector = -vector * 0.1f;

                var checkPoint = worldPoint;
                var checkOppositePoint = worldOppositePoint;

                // Step until a surface is found
                for (var j = 0; j < 20; j++)
                {
                    checkPoint += smallVector;
                    checkOppositePoint += smallVector;

                    if (Physics.Linecast(checkPoint, checkOppositePoint, out var normalInfo, PortalConstants.HitMask, QueryTriggerInteraction.Ignore))
                    {
                        if (CheckForPortal(normalInfo.point))
                        {
                            continue;
                        }

                        var offset = normalInfo.point - worldFlatPoint;

                        offset = Quaternion.Inverse(rotation) * offset;
                        offset.z = 0f;
                        offset = rotation * offset;

                        position += offset * 1.1f;
                        break;
                    }
                }
            }
        }
    }

    private unsafe bool CheckSurface(Vector3 position, Quaternion rotation, Vector3* points, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var point = points[i];

            var worldPoint = position + rotation * point;

            var oppositePoint = point;
            oppositePoint.z = -oppositePoint.z;

            var worldOppositePoint = position + rotation * oppositePoint;

            // Wall check
            if (!Physics.Linecast(worldPoint, worldOppositePoint, out var wallInfo, PortalConstants.HitMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Portal check
            if (CheckForPortal(worldPoint))
            {
                return false;
            }

            // Count is center
            if (i == count - 1)
            {
                continue;
            }

            // Side check
            var center = position + rotation * points[count - 1];

            if (Physics.Linecast(center, worldPoint, out var sideInfo, PortalConstants.HitMask, QueryTriggerInteraction.Ignore) && !sideInfo.rigidbody && !sideInfo.collider.GetComponentInParent<Portal>())
            {
                return false;
            }

            if (Physics.Linecast(worldPoint, center, out sideInfo, PortalConstants.HitMask, QueryTriggerInteraction.Ignore) && !sideInfo.rigidbody && !sideInfo.collider.GetComponentInParent<Portal>())
            {
                return false;
            }
        }

        return true;
    }

    private bool CheckForPortal(Vector3 point)
    {
        var portalOverlap = Physics.OverlapSphere(point, 0.01f, 1 << PortalConstants.PortalLayer, QueryTriggerInteraction.Collide);

        return CheckForPortal(portalOverlap);
    }

    [HideFromIl2Cpp]
    private bool CheckForPortal(Collider[] colliders)
    {
        foreach (var collider in colliders)
        {
            var foundPortal = collider.GetComponentInParent<Portal>();

            if (foundPortal == null)
            {
                continue;
            }

            if (!foundPortal.ID.HasValue)
            {
                return true;
            }

            // We can replace a portal of the same type/id, but not other portals
            if (foundPortal.ID.Value != _projectileData.SpawnInfo.ID || foundPortal.Primary != _projectileData.SpawnInfo.Primary)
            {
                return true;
            }
        }

        return false;
    }
}
