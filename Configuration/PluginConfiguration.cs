using Jellyfin.Plugin.NotificationCenter.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NotificationCenter.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        RetentionDays = 7;
        MovieNotificationLevel = NotificationLevel.All;
        SeriesNotificationLevel = NotificationLevel.All;
        MusicNotificationLevel = NotificationLevel.Disabled;
        BookNotificationLevel = NotificationLevel.Disabled;
    }

    /// <summary>
    /// Gets or sets the retention days for notifications.
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Gets or sets the notification level for movies.
    /// </summary>
    public NotificationLevel MovieNotificationLevel { get; set; }

    /// <summary>
    /// Gets or sets the notification level for TV series (episodes/seasons).
    /// </summary>
    public NotificationLevel SeriesNotificationLevel { get; set; }

    /// <summary>
    /// Gets or sets the notification level for music albums.
    /// </summary>
    public NotificationLevel MusicNotificationLevel { get; set; }

    /// <summary>
    /// Gets or sets the notification level for books.
    /// </summary>
    public NotificationLevel BookNotificationLevel { get; set; }
}