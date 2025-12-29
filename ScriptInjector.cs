using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NotificationCenter;

/// <summary>
/// Handles JavaScript injection into Jellyfin web interface.
/// </summary>
public class ScriptInjector
{
    private readonly ILogger<ScriptInjector> _logger;
    private readonly string _indexHtmlPath;
    private const string ScriptTag = "<script src=\"/NotificationCenter/client.js\" defer></script>";

    public ScriptInjector(string webPath, ILogger<ScriptInjector> logger)
    {
        _logger = logger;
        _indexHtmlPath = Path.Combine(webPath, "index.html");
    }

    /// <summary>
    /// Injects the notification client script into index.html.
    /// </summary>
    public void InjectScript()
    {
        try
        {
            if (!File.Exists(_indexHtmlPath))
            {
                _logger.LogError("index.html not found at {Path}", _indexHtmlPath);
                return;
            }

            var contents = File.ReadAllText(_indexHtmlPath);

            // Check if already injected
            if (contents.Contains("NotificationCenter/client.js"))
            {
                _logger.LogDebug("Script already injected in index.html");
                return;
            }

            // Inject before closing body tag
            if (contents.Contains("</body>"))
            {
                contents = contents.Replace("</body>", $"{ScriptTag}\n</body>");
                File.WriteAllText(_indexHtmlPath, contents);
                _logger.LogInformation("Successfully injected notification client script into index.html");
            }
            else
            {
                _logger.LogWarning("Could not find closing body tag in index.html");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied writing to index.html. In Docker, map index.html as volume.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject script into index.html");
        }
    }

    /// <summary>
    /// Removes the injected script from index.html.
    /// </summary>
    public void RemoveScript()
    {
        try
        {
            if (!File.Exists(_indexHtmlPath))
            {
                return;
            }

            var contents = File.ReadAllText(_indexHtmlPath);

            if (contents.Contains(ScriptTag))
            {
                contents = contents.Replace(ScriptTag + "\n", "").Replace(ScriptTag, "");
                File.WriteAllText(_indexHtmlPath, contents);
                _logger.LogInformation("Removed notification client script from index.html");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove script from index.html");
        }
    }
}
