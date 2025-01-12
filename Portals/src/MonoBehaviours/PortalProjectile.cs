using System;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Interaction;

using Portals.Pooling;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class PortalProjectile : MonoBehaviour
{
    public PortalProjectile(IntPtr intPtr) : base(intPtr) { }

    public static readonly LayerMask HitMask = ~(1 << (int)MarrowLayers.Football);

    public struct ProjectileData
    {
        public Vector3 Position { get; set; }

        public Vector3 Direction { get; set; }

        public Vector3 Velocity { get; set; }

        public Color Color { get; set; }

        public PortalSpawner.PortalSpawnInfo SpawnInfo { get; set; }

        public float MaxTime { get; set; }
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

        if (Physics.Linecast(startPosition, endPosition, out var hitInfo, HitMask, QueryTriggerInteraction.Ignore))
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

        if (!Physics.Raycast(startPosition, direction, out var hitInfo, float.PositiveInfinity, HitMask, QueryTriggerInteraction.Ignore))
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
            Stop();
            return;
        }

        var collider = hitInfo.collider;

        if (collider.GetComponentInParent<Portal>() || collider.GetComponent<PortalBlocker>())
        {
            OnInvalidHit(position, rotation);
            Stop();
            return;
        }

        var spawnInfo = _projectileData.SpawnInfo;
        spawnInfo.Position = position;
        spawnInfo.Rotation = rotation;

        PortalSpawner.Spawn(spawnInfo);

        Stop();
        return;
    }

    private void OnInvalidHit(Vector3 position, Quaternion rotation)
    {
        PortalEffectSpawner.PlayImpactEffect(position, rotation, _projectileData.Color);
    }
}
