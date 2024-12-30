using System;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow.Interaction;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class TeleportableEntity : Teleportable
{
    public TeleportableEntity(IntPtr intPtr) : base(intPtr) { }

    protected override void OnTeleportableAwake()
    {
        CreateClone(gameObject);

        var marrowEntity = GetComponent<MarrowEntity>();

        SetupEntity(marrowEntity);
    }

    private void LateUpdate()
    {
        if (!HasPortals)
        {
            return;
        }

        UpdateClone();

        var newSign = GetPortalSign(_inPortal);

        if (PassedThrough(_initialSign, newSign) && InBounds(_inPortal, GetAnchor()) && !IsGrabbed())
        {
            Teleport(_inPortal, _outPortal);

            SetPortals(_outPortal, _inPortal);

            UpdateClone();
        }
    }

    public override void Teleport(Portal inPortal, Portal outPortal)
    {
        var inTransform = inPortal.transform;
        var outTransform = outPortal.transform;

        var newMatrix = CalculateTeleportedMatrix(transform.localToWorldMatrix, inTransform, outTransform);

        transform.position = newMatrix.GetPosition();
        transform.rotation = newMatrix.rotation;

        var oldScale = transform.localScale;
        var newScale = newMatrix.lossyScale;

        var scaleFactor = Mathf.Abs((newScale.x / oldScale.x) * (newScale.y / oldScale.y) * (newScale.z / oldScale.z));

        transform.localScale = newScale;

        foreach (var body in MarrowEntity.Bodies)
        {
            if (!body.HasRigidbody)
            {
                continue;
            }

            var rigidbody = body._rigidbody;

            rigidbody.mass *= scaleFactor;
            rigidbody.inertiaTensor *= scaleFactor;

            rigidbody.velocity = outTransform.TransformVector(inTransform.InverseTransformVector(rigidbody.velocity));
            rigidbody.angularVelocity = outTransform.TransformDirection(inTransform.InverseTransformDirection(rigidbody.angularVelocity));
        }
    }

    public override Vector3 GetAnchor()
    {
        Vector3 center = Vector3.zero;
        float mass = 0f;

        foreach (var body in MarrowEntity.Bodies)
        {
            center += body.CenterOfMassInWorld * body.Mass;
            mass += body.Mass;
        }

        center /= mass;

        return center;
    }
}
