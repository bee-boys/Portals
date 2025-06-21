using BoneLib;

using MelonLoader;

using Portals.Rendering;

using Unity.XR.MockHMD;

using UnityEngine.XR.Management;

using System.Reflection;

namespace Portals;

public class PortalsMod : MelonMod
{
    public const string Version = "1.6.0";

    public static MelonLogger.Instance Logger { get; private set; }

    public static bool IsMockHMD { get; private set; } = false;

    public static Assembly PortalsAssembly { get; private set; }

    public static bool HasFusion { get; private set; } = false;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        PortalsAssembly = MelonAssembly.Assembly;

        Hooking.OnLevelLoaded += OnLevelLoaded;

        CheckFusion();

        PortalPreferences.SetupPreferences();
    }

    private static void CheckFusion()
    {
        if (FindMelon("LabFusion", "Lakatrazz") != null)
        {
            HasFusion = true;

            EmbeddedResource.LoadAssemblyFromAssembly(PortalsAssembly, "Portals.resources.PortalsModule.dll")
                .GetType("PortalsModule.ModuleLoader")
                .GetMethod("LoadModule")
                .Invoke(null, null);
        }
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