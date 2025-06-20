using MelonLoader;

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Portals;

public static class EmbeddedResource
{
    public static byte[] LoadBytesFromAssembly(Assembly assembly, string name)
    {
        string[] manifestResources = assembly.GetManifestResourceNames();

        if (!manifestResources.Contains(name))
        {
            MelonLogger.Warning($"Failed to locate an embedded resource with path {name}!");

            return null;
        }

        MelonLogger.Msg($"Loading embedded resource data {name}...", ConsoleColor.DarkCyan);

        using Stream str = assembly.GetManifestResourceStream(name);
        using MemoryStream memoryStream = new();

        str.CopyTo(memoryStream);

        MelonLogger.Msg("Done!", ConsoleColor.DarkCyan);

        return memoryStream.ToArray();
    }

    public static Assembly LoadAssemblyFromAssembly(Assembly assembly, string name)
    {
        var rawAssembly = LoadBytesFromAssembly(assembly, name);

        if (rawAssembly == null)
        {
            return null;
        }

        return Assembly.Load(rawAssembly);
    }
}