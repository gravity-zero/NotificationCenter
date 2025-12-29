using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.NotificationCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NotificationCenter.Api;

/// <summary>
/// Notifications API controller.
/// </summary>
[ApiController]
[Route("NotificationCenter")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsController"/> class.
    /// </summary>
    public NotificationsController(ILogger<NotificationsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all notifications for the current user.
    /// </summary>
    /// <param name="unreadOnly">Only return unread notifications.</param>
    /// <returns>List of notifications.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var repository = NotificationCenterPlugin.Instance?.Repository;
            if (repository == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Notification repository not initialized");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Jellyfin-UserId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user");
            }

            var notifications = await repository.GetNotificationsByUserAsync(userId, unreadOnly);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving notifications");
        }
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="notificationId">The notification ID.</param>
    /// <returns>Success status.</returns>
    [HttpPost("{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead([FromRoute, Required] Guid notificationId)
    {
        try
        {
            var repository = NotificationCenterPlugin.Instance?.Repository;
            if (repository == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Notification repository not initialized");
            }

            await repository.MarkAsReadAsync(notificationId);
            _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error marking notification as read");
        }
    }

    /// <summary>
    /// Gets the count of unread notifications for the current user.
    /// </summary>
    /// <returns>Count of unread notifications.</returns>
    [HttpGet("unread/count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var repository = NotificationCenterPlugin.Instance?.Repository;
            if (repository == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Notification repository not initialized");
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Jellyfin-UserId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user");
            }

            var notifications = await repository.GetNotificationsByUserAsync(userId, unreadOnly: true);
            return Ok(notifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error getting unread count");
        }
    }
}
