using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Jellyfin.Plugin.NotificationCenter;

public static class FileTransformationRegistrar
{
    public static Assembly? FindFileTransformationAssembly()
    {
        return AssemblyLoadContext.All
            .SelectMany(x => x.Assemblies)
            .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation") ?? false);
    }
}
