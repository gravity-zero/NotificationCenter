namespace Jellyfin.Plugin.NotificationCenter.Models;

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// New movie added.
    /// </summary>
    NewMovie = 1,

    /// <summary>
    /// New episode added.
    /// </summary>
    NewEpisode = 2,

    /// <summary>
    /// New season added.
    /// </summary>
    NewSeason = 3,

    /// <summary>
    /// New series added.
    /// </summary>
    NewSeries = 4,

    /// <summary>
    /// New album added.
    /// </summary>
    NewAlbum = 5,

    /// <summary>
    /// Library update.
    /// </summary>
    LibraryUpdate = 6,

    /// <summary>
    /// Custom notification.
    /// </summary>
    Custom = 99
}
