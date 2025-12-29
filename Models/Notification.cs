using System;

namespace Jellyfin.Plugin.NotificationCenter.Models;

/// <summary>
/// Represents a notification for a user.
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related item ID (optional).
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Gets or sets when the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the notification expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the notification was delivered (optional).
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets when the notification was read (optional).
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
