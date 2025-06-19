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
        var marrowEntity = GetComponent<MarrowEntity>();

        SetupEntity(marrowEntity);
    }

    protected override void OnEnsureClone()
    {
        CreateClone(gameObject);
    }

    private void LateUpdate()
    {
        if (!HasPortals)
        {
            return;
        }

        UpdateClone();

        var inPortal = _inPortal;
        var outPortal = _outPortal;

        var newSign = GetPortalSign(inPortal);

        if (PassedThrough(_initialSign, newSign) && InBounds(inPortal, GetAnchor()) && !IsGrabbed())
        {
            TryTeleport(inPortal, outPortal);
        }
    }

    protected override void OnTeleport(Portal inPortal, Portal outPortal)
    {
        var inMatrix = inPortal.PortalEnterMatrix;
        var inMatrixInverse = inMatrix.inverse;

        var outMatrix = outPortal.PortalExitMatrix;

        var newMatrix = CalculateTeleportedMatrix(transform.localToWorldMatrix, inMatrix, outMatrix);

        transform.position = newMatrix.GetPosition();
        transform.rotation = newMatrix.rotation;

        var oldScale = transform.localScale;
        var newScale = newMatrix.lossyScale;

        var xScale = newScale.x / oldScale.x;
        var yScale = newScale.y / oldScale.y;
        var zScale = newScale.z / oldScale.z;

        var scaleFactor = Math.Abs(xScale * yScale * zScale);
        var scaleVector = new Vector3(xScale, yScale, zScale);

        bool changedScale = !Mathf.Approximately(scaleFactor, 1f);

        transform.localScale = newScale;

        foreach (var body in MarrowEntity.Bodies)
        {
            body._cachedRigidbodyInfo.mass *= scaleFactor;
            body._cachedRigidbodyInfo.inertiaTensor *= scaleFactor;

            if (!body.HasRigidbody)
            {
                continue;
            }

            var rigidbody = body._rigidbody;

            rigidbody.mass *= scaleFactor;
            rigidbody.inertiaTensor *= scaleFactor;

            rigidbody.velocity = outMatrix.MultiplyVector(inMatrixInverse.MultiplyVector(rigidbody.velocity - inPortal.Velocity)) + outPortal.Velocity;
            rigidbody.angularVelocity = outMatrix.rotation * (inMatrixInverse.rotation * (rigidbody.angularVelocity - inPortal.AngularVelocity)) + outPortal.AngularVelocity;

            rigidbody.centerOfMass = Vector3.Scale(rigidbody.centerOfMass, scaleVector);
        }

        UpdateThrowing(inMatrixInverse, outMatrix);

        if (changedScale)
        {
            UpdateJoints();
        }
    }

    public void Scale(float factor)
    {
        transform.localScale = transform.localScale * factor;

        float massFactor = factor * factor * factor;

        foreach (var body in MarrowEntity.Bodies)
        {
            body._cachedRigidbodyInfo.mass *= massFactor;
            body._cachedRigidbodyInfo.inertiaTensor *= massFactor;

            if (!body.HasRigidbody)
            {
                continue;
            }

            var rigidbody = body._rigidbody;

            rigidbody.mass *= massFactor;
            rigidbody.inertiaTensor *= massFactor;

            rigidbody.centerOfMass *= factor;
        }

        UpdateJoints();
    }

    public override Vector3 GetAnchor()
    {
        Vector3 center = Vector3.zero;
        float mass = 0f;

        foreach (var body in Bodies)
        {
            center += body.GetAnchor() * body.Mass;
            mass += body.Mass;
        }

        center /= mass;

        return center;
    }

    private void UpdateJoints()
    {
        foreach (var joint in MarrowEntity.Joints)
        {
            if (!joint.HasConfigJoint)
            {
                continue;
            }

            var defaultJointInfo = joint._defaultConfigJointInfo;

            var configJoint = joint._configurableJoint;
            var swapBodies = configJoint.swapBodies;

            var anchor = swapBodies ? defaultJointInfo.connectedAnchor : defaultJointInfo.anchor;
            var connectedAnchor = swapBodies ? defaultJointInfo.anchor : defaultJointInfo.connectedAnchor;

            // Don't change it unless necessary, as it will change the joint space
            // Thankfully when it has to change MarrowJoint has a method for writing the original
            if (configJoint.autoConfigureConnectedAnchor)
            {
                configJoint.autoConfigureConnectedAnchor = false;

                joint.WriteJointSpace();
            }

            configJoint.anchor = anchor;
            configJoint.connectedAnchor = connectedAnchor;
        }
    }

    private void UpdateThrowing(Matrix4x4 inMatrixInverse, Matrix4x4 outMatrix)
    {
        foreach (var body in Bodies)
        {
            if (!body.HasHost)
            {
                continue;
            }

            foreach (var grip in body.Host._grips)
            {
                for (var i = 0; i < grip.velocityHistory.Length; i++)
                {
                    grip.velocityHistory[i] = outMatrix.MultiplyVector(inMatrixInverse.MultiplyVector(grip.velocityHistory[i]));
                    grip.angVelocityHistory[i] = outMatrix.rotation * (inMatrixInverse.rotation * grip.angVelocityHistory[i]);
                }
            }
        }
    }
}
