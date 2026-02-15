using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.NotificationCenter;

public class StartupTask : IScheduledTask
{
    public string Name => "NotificationCenter Startup";
    public string Key => "Jellyfin.Plugin.NotificationCenter.Startup";
    public string Description => "Registers FileTransformation patches";
    public string Category => "Startup Services";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var payload = new JObject
        {
            ["id"] = "eb5d7894-8eef-4b36-aa6f-5d124e828ce1",
            ["fileNamePattern"] = "index.html",
            ["callbackAssembly"] = GetType().Assembly.FullName,
            ["callbackClass"] = typeof(TransformationPatches).FullName,
            ["callbackMethod"] = nameof(TransformationPatches.IndexHtml)
        };

        var assembly = AssemblyLoadContext.All
            .SelectMany(x => x.Assemblies)
            .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation") ?? false);

        if (assembly != null)
        {
            var pluginInterface = assembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            pluginInterface?.GetMethod("RegisterTransformation")?.Invoke(null, new object?[] { payload });
        }

        return Task.CompletedTask;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
    }
}
