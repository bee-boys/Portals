using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Il2CppSLZ.Marrow;

using UnityEngine;

namespace Portals.Patching;

[HarmonyPatch(typeof(Projectile))]
public static class ProjectilePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Projectile.Update))]
    public static void UpdatePrefix(Projectile __instance, ref Vector3 __state)
    {
        __state = __instance.transform.position;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Projectile.Update))]
    public static void UpdatePostfix(Projectile __instance, ref Vector3 __state)
    {
        var start = __state;
        var end = __instance.transform.position;

        var lineCast = Physics.Linecast(start, end, out var hitInfo, ~0, QueryTriggerInteraction.Collide);

        if (lineCast)
        {
            var portal = hitInfo.collider.GetComponentInParent<Portal>();

            if (portal != null && portal.OtherPortal != null)
            {
                var inMatrix = portal.transform.localToWorldMatrix;
                var outMatrix = portal.OtherPortal.transform.localToWorldMatrix;

                var matrix = Matrix4x4.TRS(end, Quaternion.LookRotation(__instance._direction), Vector3.one);

                matrix = outMatrix * (inMatrix.inverse * matrix);

                __instance.transform.position = matrix.GetPosition();
                __instance._direction = matrix.rotation * Vector3.forward;

                var scale = matrix.lossyScale;

                __instance.Mass *= scale.x * scale.y * scale.z;
                __instance.currentSpeed *= scale.z;

                __instance.trail.Clear();
            }
        }
    }
}
