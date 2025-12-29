using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NotificationCenter.Services;

/// <summary>
/// Analyzes user watch history to determine notification relevance.
/// </summary>
public class UserHistoryAnalyzer
{
    private readonly IUserDataManager _userDataManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<UserHistoryAnalyzer> _logger;

    public UserHistoryAnalyzer(
        IUserDataManager userDataManager,
        ILibraryManager libraryManager,
        IUserManager userManager,
        ILogger<UserHistoryAnalyzer> logger)
    {
        _userDataManager = userDataManager;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has watched any episode of a series.
    /// </summary>
    public bool HasWatchedSeries(Guid userId, Series series)
    {
        try
        {
            var user = _userManager.GetUserById(userId);
            if (user == null) return false;

            var episodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                AncestorIds = new[] { series.Id },
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                Recursive = true
            });

            return episodes.Any(episode =>
            {
                var userData = _userDataManager.GetUserData(user, episode);
                return userData?.Played == true || userData?.PlaybackPositionTicks > 0;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking watch history for series {SeriesId}", series.Id);
            return false;
        }
    }

    /// <summary>
    /// Check if user is currently watching a series (has progress on at least one episode).
    /// </summary>
    public bool IsCurrentlyWatchingSeries(Guid userId, Series series)
    {
        try
        {
            var user = _userManager.GetUserById(userId);
            if (user == null) return false;

            var episodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                AncestorIds = new[] { series.Id },
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                Recursive = true
            });

            return episodes.Any(episode =>
            {
                var userData = _userDataManager.GetUserData(user, episode);
                return userData?.PlaybackPositionTicks > 0 && userData.PlaybackPositionTicks < episode.RunTimeTicks;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking current watch status for series {SeriesId}", series.Id);
            return false;
        }
    }

    /// <summary>
    /// Get user's favorite genres based on watch history.
    /// </summary>
    public HashSet<string> GetUserFavoriteGenres(Guid userId, int minWatchCount = 3)
    {
        try
        {
            var user = _userManager.GetUserById(userId);
            if (user == null) return new HashSet<string>();

            var watchedItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Episode },
                Recursive = true
            }).Where(item =>
            {
                var userData = _userDataManager.GetUserData(user, item);
                return userData?.Played == true;
            });

            var genreCounts = new Dictionary<string, int>();
            foreach (var item in watchedItems)
            {
                foreach (var genre in item.Genres)
                {
                    if (!genreCounts.ContainsKey(genre))
                    {
                        genreCounts[genre] = 0;
                    }
                    genreCounts[genre]++;
                }
            }

            return genreCounts
                .Where(kvp => kvp.Value >= minWatchCount)
                .Select(kvp => kvp.Key)
                .ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing favorite genres for user {UserId}", userId);
            return new HashSet<string>();
        }
    }

    /// <summary>
    /// Calculate movie relevance score based on genre matching.
    /// </summary>
    public int CalculateMovieRelevanceScore(Guid userId, Movie movie)
    {
        var favoriteGenres = GetUserFavoriteGenres(userId);
        if (favoriteGenres.Count == 0) return 0;

        var matchingGenres = movie.Genres.Count(g => favoriteGenres.Contains(g));
        return matchingGenres;
    }
}
