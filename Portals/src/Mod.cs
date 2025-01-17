using BoneLib;

using MelonLoader;

using Portals.Rendering;

using Unity.XR.MockHMD;
using UnityEngine.XR.Management;

namespace Portals;

public class PortalsMod : MelonMod
{
    public const string Version = "1.0.0";

    public static MelonLogger.Instance Logger { get; private set; }

    public static bool IsMockHMD { get; private set; } = false;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        Hooking.OnLevelLoaded += OnLevelLoaded;

        PortalPreferences.SetupPreferences();
    }

    public override void OnLateInitializeMelon()
    {
        IsMockHMD = XRGeneralSettings.Instance.Manager.loaders.Find((Il2CppSystem.Predicate<XRLoader>)((loader) => loader.TryCast<MockHMDLoader>() != null));
    }

    public override void OnPreferencesLoaded()
    {
        PortalPreferences.LoadPreferences();
    }

    private void OnLevelLoaded(LevelInfo info)
    {
        CloneCreator.OnLevelLoaded();
    }
}