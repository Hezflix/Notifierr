using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.ServerApi.Clients.Interfaces;
using PlexNotifierr.Core.Config;
using PlexNotifierr.Core.Models;
using Quartz;

namespace PlexNotifierr.Worker.Jobs
{
    [DisallowConcurrentExecution]
    public class GetUsersHistoryJob : IJob
    {
        private readonly PlexNotifierrDbContext _dbContext;

        private readonly IPlexServerClient _serverClient;

        private readonly string _url;

        private readonly string _authToken;

        private readonly ILogger _logger;

        public GetUsersHistoryJob(PlexNotifierrDbContext dbContext, IPlexServerClient plexServerClient, IOptions<PlexConfig> plexConfig, ILogger<GetUsersHistoryJob> logger)
        {
            _dbContext = dbContext;
            _serverClient = plexServerClient;
            _url = plexConfig.Value.ServerUrl;
            _authToken = plexConfig.Value.AccessToken;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var users = _dbContext.Users.Include(u => u.Medias).Include(us => us.Medias).ToList();
            var mediasRatingKey = _dbContext.Medias.Select(m => m.RatingKey).ToHashSet();

            const int limit = 100;
            foreach (var user in users)
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
                                var media = new Media()
                                {
                                    RatingKey = grandParentRatingKey,
                                    Title = grandParentMetadata.Title ?? "",
                                    ThumbUrl = grandParentMetadata.Thumb ?? "",
                                    Summary = grandParentMetadata.Summary ?? "",
                                };
                                _dbContext.Add(media);
                                mediasRatingKey.Add(grandParentRatingKey);
                                _dbContext.UserSubscriptions.Add(new UserSubscription() { Media = media, User = user });
                            }
                            else
                            {
                                var userSubscription = user.Medias.FirstOrDefault(x => x.RatingKey == grandParentRatingKey);
                                if (userSubscription is not null) continue;
                                var media = _dbContext.Medias.Include(m => m.Users).FirstOrDefault(x => x.RatingKey == grandParentRatingKey);
                                if (media is null) continue;
                                _dbContext.UserSubscriptions.Add(new UserSubscription() { Media = media, User = user });
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