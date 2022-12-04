using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.ServerApi.Clients.Interfaces;
using PlexNotifierr.Core.Config;
using PlexNotifierr.Core.Models;

namespace PlexNotifierr.Worker.Jobs
{
    /// <summary>
    /// A job to get history of all the user.
    /// </summary>
    public class GetUsersHistoryJob
    {
        private readonly PlexNotifierrDbContext _dbContext;
        private readonly IPlexServerClient _serverClient;
        private readonly IProgressBarFactory _progressBarFactory;
        private readonly string _url;
        private readonly string _authToken;
        private readonly ILogger _logger;

        public GetUsersHistoryJob(PlexNotifierrDbContext dbContext, IPlexServerClient plexServerClient, IProgressBarFactory progressBarFactory, IOptions<PlexConfig> plexConfig, ILogger<GetUsersHistoryJob> logger)
        {
            _dbContext = dbContext;
            _serverClient = plexServerClient;
            _progressBarFactory = progressBarFactory;
            _url = plexConfig.Value.ServerUrl;
            _authToken = plexConfig.Value.AccessToken;
            _logger = logger;
        }

        [JobDisplayName("GetUsersHistory")]
        public async Task ExecuteAsync()
        {
            var users = await _dbContext.Users.Include(u => u.Medias).Include(us => us.Medias).ToListAsync();
            var mediasRatingKey = _dbContext.Medias.Select(m => m.RatingKey).ToHashSet();

            const int limit = 300;
            _logger.LogInformation("{UsersCount} users to process", users.Count);
            var progressBar = _progressBarFactory.Create();
            foreach (var user in users.WithProgress(progressBar))
            {
                var offset = user.HistoryPosition;
                var isLastPage = false;
                while (!isLastPage)
                {
                    try
                    {
                        var history = await _serverClient.GetPlayHistory(_authToken, _url, offset, limit, accountId: user.PlexId);
                        isLastPage = history.Size < limit;
                        offset += limit;
                        if (history.HistoryMetadata is null) continue;
                        foreach (var historyMetadata in history.HistoryMetadata.Where(historyMetadata => historyMetadata?.Type == "episode"))
                        {
                            var grandparentKey = historyMetadata.GrandParentKey;
                            var grandparentRatingKey = grandparentKey?.Split('/').LastOrDefault() ?? string.Empty;
                            if (!int.TryParse(grandparentRatingKey, out var grandParentRatingKey)) continue;
                            if (!mediasRatingKey.Contains(grandParentRatingKey))
                            {
                                var grandParentMediaContainer = await _serverClient.GetMediaMetadataAsync(_authToken, _url, grandparentRatingKey);
                                var grandParentMetadata = grandParentMediaContainer.Media.FirstOrDefault();
                                if (grandParentMetadata is null) continue;
                                var posters = await _serverClient.GetMediaPostersAsync(_authToken, _url, grandparentRatingKey);
                                var media = new Media()
                                {
                                    RatingKey = grandParentRatingKey,
                                    Title = grandParentMetadata.Title ?? "",
                                    ThumbUrl = posters.Media.FirstOrDefault(x => !x.Key.StartsWith("/"))?.Key ?? "",
                                    Summary = grandParentMetadata.Summary ?? "",
                                };
                                _dbContext.Add(media);
                                mediasRatingKey.Add(grandParentRatingKey);
                                _dbContext.UserSubscriptions.Add(new UserSubscription() { Media = media, User = user });
                                _logger.LogInformation("New media {MediaTitle} added", media.Title);
                            }
                            else
                            {
                                var userSubscription = user.Medias.FirstOrDefault(x => x.RatingKey == grandParentRatingKey);
                                if (userSubscription is not null) continue;
                                var media = _dbContext.Medias.FirstOrDefault(x => x.RatingKey == grandParentRatingKey);
                                if (media is null) continue;
                                _dbContext.UserSubscriptions.Add(new UserSubscription() { Media = media, User = user });
                                _logger.LogInformation("New subscription from {UserPlexName} to {MediaTitle}", user.PlexName, media.Title);
                            }
                        }
                        user.HistoryPosition += history.Size;
                        _ = await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
            }
        }
    }
}