using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.NotificationCenter.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NotificationCenter.Data;

/// <summary>
/// Repository for notification storage using SQLite.
/// </summary>
public class NotificationRepository : IDisposable
{
    private readonly ILogger<NotificationRepository> _logger;
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRepository"/> class.
    /// </summary>
    public NotificationRepository(IApplicationPaths appPaths, ILogger<NotificationRepository> logger)
    {
        _logger = logger;
        _dbPath = System.IO.Path.Combine(appPaths.DataPath, "notificationcenter.db");

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();

            var createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Notifications (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Message TEXT,
                    ItemId TEXT,
                    CreatedAt TEXT NOT NULL,
                    DeliveredAt TEXT,
                    ReadAt TEXT,
                    ExpiresAt TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_user_created 
                ON Notifications(UserId, CreatedAt DESC);

                CREATE INDEX IF NOT EXISTS idx_user_read 
                ON Notifications(UserId, ReadAt);
            ";

            using var command = new SqliteCommand(createTableQuery, _connection);
            command.ExecuteNonQuery();

            _logger.LogInformation("Notification database initialized at {DbPath}", _dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize notification database");
            throw;
        }
    }

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    public async Task CreateNotificationAsync(Notification notification)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database connection is not initialized");
        }

        var query = @"
            INSERT INTO Notifications (Id, UserId, Type, Title, Message, ItemId, CreatedAt, DeliveredAt, ReadAt, ExpiresAt)
            VALUES (@Id, @UserId, @Type, @Title, @Message, @ItemId, @CreatedAt, @DeliveredAt, @ReadAt, @ExpiresAt)
        ";

        using var command = new SqliteCommand(query, _connection);
        command.Parameters.AddWithValue("@Id", notification.Id.ToString());
        command.Parameters.AddWithValue("@UserId", notification.UserId.ToString());
        command.Parameters.AddWithValue("@Type", notification.Type.ToString());
        command.Parameters.AddWithValue("@Title", notification.Title);
        command.Parameters.AddWithValue("@Message", notification.Message ?? string.Empty);
        command.Parameters.AddWithValue("@ItemId", notification.ItemId?.ToString() ?? string.Empty);
        command.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@DeliveredAt", notification.DeliveredAt?.ToString("O") ?? string.Empty);
        command.Parameters.AddWithValue("@ReadAt", notification.ReadAt?.ToString("O") ?? string.Empty);
        command.Parameters.AddWithValue("@ExpiresAt", notification.ExpiresAt.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets all notifications for a user.
    /// </summary>
    public async Task<List<Notification>> GetNotificationsByUserAsync(Guid userId, bool unreadOnly = false)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database connection is not initialized");
        }

        var query = @"
            SELECT * FROM Notifications 
            WHERE UserId = @UserId 
            AND datetime(ExpiresAt) > datetime('now')
        ";

        if (unreadOnly)
        {
            query += " AND ReadAt = ''";
        }

        query += " ORDER BY CreatedAt DESC LIMIT 100";

        using var command = new SqliteCommand(query, _connection);
        command.Parameters.AddWithValue("@UserId", userId.ToString());

        var notifications = new List<Notification>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            notifications.Add(MapNotification(reader));
        }

        return notifications;
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    public async Task MarkAsReadAsync(Guid notificationId)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database connection is not initialized");
        }

        var query = "UPDATE Notifications SET ReadAt = @ReadAt WHERE Id = @Id";

        using var command = new SqliteCommand(query, _connection);
        command.Parameters.AddWithValue("@ReadAt", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@Id", notificationId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Marks a notification as delivered via WebSocket.
    /// </summary>
    public async Task MarkAsDeliveredAsync(Guid notificationId)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database connection is not initialized");
        }

        var query = "UPDATE Notifications SET DeliveredAt = @DeliveredAt WHERE Id = @Id";

        using var command = new SqliteCommand(query, _connection);
        command.Parameters.AddWithValue("@DeliveredAt", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@Id", notificationId.ToString());

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Deletes expired notifications.
    /// </summary>
    public async Task CleanupExpiredNotificationsAsync()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database connection is not initialized");
        }

        var query = "DELETE FROM Notifications WHERE datetime(ExpiresAt) <= datetime('now')";

        using var command = new SqliteCommand(query, _connection);
        var deleted = await command.ExecuteNonQueryAsync();

        if (deleted > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired notifications", deleted);
        }
    }

    private static Notification MapNotification(SqliteDataReader reader)
    {
        return new Notification
        {
            Id = Guid.Parse(reader.GetString(0)),
            UserId = Guid.Parse(reader.GetString(1)),
            Type = Enum.Parse<NotificationType>(reader.GetString(2)),
            Title = reader.GetString(3),
            Message = reader.GetString(4),
            ItemId = string.IsNullOrEmpty(reader.GetString(5)) ? null : Guid.Parse(reader.GetString(5)),
            CreatedAt = DateTime.Parse(reader.GetString(6)),
            DeliveredAt = string.IsNullOrEmpty(reader.GetString(7)) ? null : DateTime.Parse(reader.GetString(7)),
            ReadAt = string.IsNullOrEmpty(reader.GetString(8)) ? null : DateTime.Parse(reader.GetString(8)),
            ExpiresAt = DateTime.Parse(reader.GetString(9))
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection?.Dispose();
    }
}