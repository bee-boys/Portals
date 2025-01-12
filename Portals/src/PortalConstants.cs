using Il2CppSLZ.Marrow.Interaction;

using UnityEngine;

namespace Portals;

public static class PortalConstants
{
    public static readonly Vector2 DefaultSize = new(1f, 2f);

    private static LayerMask _hitMask = default;
    private static bool _hasHitMask = false;
    public static LayerMask HitMask
    {
        get
        {
            if (!_hasHitMask)
            {
                _hitMask = ~0;

                _hitMask &= ~(1 << (int)MarrowLayers.Football);
                _hitMask &= ~(1 << (int)MarrowLayers.Player);

                _hitMask &= ~(1 << (int)MarrowLayers.EntityTracker);
                _hitMask &= ~(1 << (int)MarrowLayers.EntityTrigger);

                _hitMask &= ~(1 << (int)MarrowLayers.BeingTracker);
                _hitMask &= ~(1 << (int)MarrowLayers.BeingTrigger);

                _hitMask &= ~(1 << (int)MarrowLayers.ObserverTracker);
                _hitMask &= ~(1 << (int)MarrowLayers.ObserverTrigger);

                _hasHitMask = true;
            }

            return _hitMask;
        }
    }

    public const int PortalLayer = (int)MarrowLayers.Socket;

    public const int TrackerLayer = (int)MarrowLayers.Plug;
}
