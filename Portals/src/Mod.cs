using BoneLib;

using MelonLoader;

using Portals.Rendering;
using Portals.Fusion;

using Unity.XR.MockHMD;

using UnityEngine.XR.Management;

using System.Reflection;

namespace Portals;

public class PortalsMod : MelonMod
{
    public const string Version = "1.1.0";

    public static MelonLogger.Instance Logger { get; private set; }

    public static bool IsMockHMD { get; private set; } = false;

    public static Assembly PortalsAssembly { get; private set; }

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        PortalsAssembly = MelonAssembly.Assembly;

        Hooking.OnLevelLoaded += OnLevelLoaded;

        PortalPreferences.SetupPreferences();

        if (FindMelon("LabFusion", "Lakatrazz") != null)
        {
            LoadModule();
        }
    }

    private static void LoadModule()
    {
        LabFusion.SDK.Modules.ModuleManager.RegisterModule<PortalsModule>();
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