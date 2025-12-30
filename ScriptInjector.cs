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

    public ScriptInjector(ILogger<ScriptInjector> logger)
    {
        _logger = logger;
        _indexHtmlPath = GetIndexHtmlPath();
    }

    /// <summary>
    /// Determines the correct path to index.html across different installation types.
    /// </summary>
    private string GetIndexHtmlPath()
    {
        // Try multiple common paths
        var possiblePaths = new[]
        {
            // Docker path
            "/jellyfin/jellyfin-web/index.html",
            
            // Linux paths
            "/usr/share/jellyfin/web/index.html",
            "/usr/lib/jellyfin/bin/jellyfin-web/index.html",
            
            // Relative to application directory (works for Windows and Linux)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jellyfin-web", "index.html"),
            
            // Windows typical path
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Jellyfin", "Server", "jellyfin-web", "index.html"
            )
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogInformation("Found index.html at {Path}", path);
                return path;
            }
        }

        _logger.LogWarning("index.html not found in any expected location. Defaulting to Docker path.");
        return "/jellyfin/jellyfin-web/index.html";
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
                _logger.LogInformation("Successfully injected notification client script into index.html at {Path}", _indexHtmlPath);
            }
            else
            {
                _logger.LogWarning("Could not find closing body tag in index.html");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied writing to {Path}. The web server may need write access.", _indexHtmlPath);
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
