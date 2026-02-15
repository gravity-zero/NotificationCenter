using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NotificationCenter;

public class StartupTask
{
    private readonly ILogger<StartupTask> _logger;

    public StartupTask(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<StartupTask>();
    }

    public Task RunAsync()
    {
        var assembly = FileTransformationRegistrar.FindFileTransformationAssembly();
        if (assembly != null)
        {
            _logger.LogInformation("FileTransformation plugin found, registering patches");
        }
        else
        {
            _logger.LogWarning("FileTransformation plugin not found, script injection disabled");
        }

        return Task.CompletedTask;
    }
}
