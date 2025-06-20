using LabFusion.SDK.Modules;

namespace PortalsModule;

public static class ModuleLoader
{
    public static void LoadModule()
    {
        ModuleManager.RegisterModule<PortalsModule>();
    }
}
