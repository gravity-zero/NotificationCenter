namespace Jellyfin.Plugin.NotificationCenter.Models;

/// <summary>
/// Notification relevance levels.
/// </summary>
public enum NotificationLevel
{
    /// <summary>
    /// Disabled - no notifications.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Level 1 - All new media additions.
    /// </summary>
    All = 1,

    /// <summary>
    /// Level 2 - Relevant additions (matching user history).
    /// </summary>
    Relevant = 2,

    /// <summary>
    /// Level 3 - Highly relevant (currently watching).
    /// </summary>
    HighlyRelevant = 3
}
