using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Plugin.NotificationCenter.Configuration;
using Jellyfin.Plugin.NotificationCenter.Data;
using Jellyfin.Plugin.NotificationCenter.Models;
using Jellyfin.Plugin.NotificationCenter.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NotificationCenter.EventHandlers;

public class MediaAddedHandler
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly NotificationRepository _repository;
    private readonly ILogger<MediaAddedHandler> _logger;
    private readonly UserHistoryAnalyzer _historyAnalyzer;
    
    private readonly ConcurrentDictionary<Guid, DateTime> _recentSeriesNotifications = new();
    private readonly TimeSpan _deduplicationWindow = TimeSpan.FromMinutes(5);

    public MediaAddedHandler(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        NotificationRepository repository,
        ILoggerFactory loggerFactory)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _repository = repository;
        _logger = loggerFactory.CreateLogger<MediaAddedHandler>();
        _historyAnalyzer = new UserHistoryAnalyzer(
            userDataManager,
            libraryManager,
            userManager,
            loggerFactory.CreateLogger<UserHistoryAnalyzer>());

        _libraryManager.ItemAdded += OnItemAdded;
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            await HandleItemAddedAsync(e.Item.Id);
        });
    }

    private async Task HandleItemAddedAsync(Guid itemId)
    {
        try
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                _logger.LogWarning("Item {ItemId} not found after delay", itemId);
                return;
            }

            var config = NotificationCenterPlugin.Instance?.Configuration;
            if (config == null) return;

            NotificationType? notificationType = null;
            NotificationLevel notificationLevel = NotificationLevel.Disabled;
            string title = string.Empty;
            string message = string.Empty;
            Guid? targetItemId = item.Id;

            switch (item)
            {
                case Movie movie:
                    notificationLevel = config.MovieNotificationLevel;
                    if (notificationLevel == NotificationLevel.Disabled) return;
                    
                    notificationType = NotificationType.NewMovie;
                    title = $"New Movie: {movie.Name}";
                    message = $"{movie.Name} ({movie.ProductionYear ?? 0}) has been added to the library.";
                    break;

                case Episode episode:
                    notificationLevel = config.SeriesNotificationLevel;
                    if (notificationLevel == NotificationLevel.Disabled) return;
                    
                    var series = episode.Series;
                    if (series == null)
                    {
                        _logger.LogWarning("Episode {EpisodeName} has no series linked", episode.Name);
                        return;
                    }
                    
                    var seriesName = series.Name;
                    var seasonNum = episode.ParentIndexNumber ?? 0;
                    var episodeNum = episode.IndexNumber ?? 0;
                    
                    if (IsRecentSeriesNotification(series.Id))
                    {
                        _logger.LogDebug("Skipping notification for {SeriesName} - recent bulk add", seriesName);
                        return;
                    }
                    
                    notificationType = NotificationType.NewEpisode;
                    title = $"New Episode: {seriesName}";
                    message = $"{seriesName} S{seasonNum:00}E{episodeNum:00} - {episode.Name} has been added.";
                    
                    _recentSeriesNotifications[series.Id] = DateTime.UtcNow;
                    break;

                case Season season:
                    notificationLevel = config.SeriesNotificationLevel;
                    if (notificationLevel == NotificationLevel.Disabled) return;
                    
                    var seasonSeries = season.Series;
                    if (seasonSeries == null)
                    {
                        _logger.LogWarning("Season has no series linked");
                        return;
                    }
                    
                    if (season.Name?.Contains("Unknown") == true)
                    {
                        _logger.LogDebug("Skipping temporary/unknown season");
                        return;
                    }
                    
                    var seasonSeriesName = seasonSeries.Name;
                    var seasonIndex = season.IndexNumber ?? 0;
                    
                    if (IsRecentSeriesNotification(seasonSeries.Id))
                    {
                        _logger.LogDebug("Skipping season notification - recent bulk add");
                        return;
                    }
                    
                    notificationType = NotificationType.NewSeason;
                    title = $"New Season: {seasonSeriesName}";
                    message = $"{seasonSeriesName} Season {seasonIndex} has been added to the library.";
                    
                    _recentSeriesNotifications[seasonSeries.Id] = DateTime.UtcNow;
                    break;

                case MusicAlbum album:
                    notificationLevel = config.MusicNotificationLevel;
                    if (notificationLevel == NotificationLevel.Disabled) return;
                    
                    var artist = album.AlbumArtists?.FirstOrDefault() ?? "Unknown Artist";
                    notificationType = NotificationType.NewAlbum;
                    title = $"New Album: {album.Name}";
                    message = $"{artist} - {album.Name} ({album.ProductionYear ?? 0}) has been added.";
                    break;
            }

            if (!notificationType.HasValue) return;

            // Notify users based on relevance level
            foreach (var user in _userManager.Users.ToList())
            {
                if (ShouldNotifyUser(user.Id, item, notificationLevel))
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Type = notificationType.Value,
                        Title = title,
                        Message = message,
                        ItemId = targetItemId,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(config.RetentionDays > 0 ? config.RetentionDays : 7)
                    };

                    await _repository.CreateNotificationAsync(notification);

                    _logger.LogInformation(
                        "Created notification {NotificationId} for user {UserId}: {Title} (Level: {Level})",
                        notification.Id,
                        user.Id,
                        title,
                        notificationLevel);
                }
                else
                {
                    _logger.LogDebug(
                        "Skipped notification for user {UserId}: {Title} (Level {Level} not met)",
                        user.Id,
                        title,
                        notificationLevel);
                }
            }
            
            CleanupOldDeduplicationEntries();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling item added event");
        }
    }

    private bool ShouldNotifyUser(Guid userId, BaseItem item, NotificationLevel level)
    {
        if (level == NotificationLevel.All)
        {
            return true;
        }

        switch (item)
        {
            case Movie movie:
                if (level == NotificationLevel.Relevant)
                {
                    var score = _historyAnalyzer.CalculateMovieRelevanceScore(userId, movie);
                    return score > 0;
                }
                if (level == NotificationLevel.HighlyRelevant)
                {
                    var score = _historyAnalyzer.CalculateMovieRelevanceScore(userId, movie);
                    return score >= 2;
                }
                break;

            case Episode episode:
                var series = episode.Series;
                if (series == null) return false;

                if (level == NotificationLevel.Relevant)
                {
                    return _historyAnalyzer.HasWatchedSeries(userId, series);
                }
                if (level == NotificationLevel.HighlyRelevant)
                {
                    return _historyAnalyzer.IsCurrentlyWatchingSeries(userId, series);
                }
                break;

            case Season season:
                var seasonSeries = season.Series;
                if (seasonSeries == null) return false;

                if (level == NotificationLevel.Relevant)
                {
                    return _historyAnalyzer.HasWatchedSeries(userId, seasonSeries);
                }
                if (level == NotificationLevel.HighlyRelevant)
                {
                    return _historyAnalyzer.IsCurrentlyWatchingSeries(userId, seasonSeries);
                }
                break;
        }

        return false;
    }
    
    private bool IsRecentSeriesNotification(Guid seriesId)
    {
        if (_recentSeriesNotifications.TryGetValue(seriesId, out var lastNotification))
        {
            return DateTime.UtcNow - lastNotification < _deduplicationWindow;
        }
        return false;
    }
    
    private void CleanupOldDeduplicationEntries()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var toRemove = _recentSeriesNotifications
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in toRemove)
        {
            _recentSeriesNotifications.TryRemove(key, out _);
        }
    }
}
