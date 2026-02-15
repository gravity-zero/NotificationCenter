using System;
using System.Linq;
using System.Reflection;

namespace Jellyfin.Plugin.NotificationCenter;

public static class FileTransformationRegistrar
{
    public static Assembly? FindFileTransformationAssembly()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.FileTransformation");
    }
}
