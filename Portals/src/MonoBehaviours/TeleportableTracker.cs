using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using MelonLoader;

using Il2CppSLZ.Marrow;

namespace Portals.MonoBehaviours;

[RegisterTypeInIl2Cpp]
public class TeleportableTracker : MonoBehaviour
{
    public TeleportableTracker(IntPtr intPtr) : base(intPtr) { }

    public InteractableHost Host => _host;

    public bool HasHost => _hasHost;

    private InteractableHost _host = null;
    private bool _hasHost = false;

    public event Action<Collider> OnTriggerEnterEvent, OnTriggerExitEvent;

    private void Awake()
    {
        _host = GetComponent<InteractableHost>();
        _hasHost = _host != null;
    }

    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterEvent?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        OnTriggerExitEvent?.Invoke(other);
    }
}
