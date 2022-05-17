using Microsoft.EntityFrameworkCore;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.PlexModels.Media;
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

        public GetUsersHistoryJob(PlexNotifierrDbContext dbContext, IPlexServerClient plexServerClient, string url, string authToken)
        {
            _dbContext = dbContext;
            _serverClient = plexServerClient;
            _url = url;
            _authToken = authToken;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var users = _dbContext.Users.Include(u => u.Medias).Include(us => us.Medias).ToList();
            var mediasRatingKey = _dbContext.Medias.Select(m => m.RatingKey).ToHashSet();

            //Do it per user when option available in Plex.Api
            var serverOwner = users.First(x => x.PlexId == 1);
            var offset = serverOwner.HistoryPosition;
            var limit = 100;
            var isLastPage = false;
            while (!isLastPage)
            {
                var history = await _serverClient.GetPlayHistory(_authToken, _url, offset, limit);
                isLastPage = history.Size < limit;
                offset += limit;
                foreach (var historyMetadata in history.HistoryMetadata)
                {
                    if (historyMetadata.Type != "episode") continue;
                    //Get grandparentKey directly for HistoryMetada when field available in Plex.Api
                    MediaContainer? episodeMediaContainer = await _serverClient.GetMediaMetadataAsync(_authToken, _url, historyMetadata.RatingKey);
                    var episodeMetadata = episodeMediaContainer?.Media.FirstOrDefault();
                    var grandparentRatingKey = episodeMetadata?.GrandparentRatingKey;
                    var userConcerned = users.FirstOrDefault(x => x.PlexId == historyMetadata.AccountId);
                    if (string.IsNullOrWhiteSpace(grandparentRatingKey) || episodeMetadata == null || userConcerned == null) continue;
                    if (!mediasRatingKey.Contains(int.Parse(grandparentRatingKey)))
                    {
                        var grandParentMediaContainer = await _serverClient.GetMediaMetadataAsync(_authToken, _url, grandparentRatingKey);
                        var grandParentMetadata = grandParentMediaContainer.Media.FirstOrDefault();
                        var media = new Media()
                        {
                            RatingKey = int.Parse(grandparentRatingKey),
                            Title = episodeMetadata.GrandparentTitle,
                            Summary = grandParentMetadata?.Summary ?? "",
                            ThumbUrl = grandParentMetadata?.Thumb ?? "",
                        };
                        _dbContext.Add(media);
                        mediasRatingKey.Add(int.Parse(grandparentRatingKey));
                        userConcerned.Medias.Add(new UserSubscription()
                        {
                            Media = media,
                            Active = true,
                        });
                    }
                    else
                    {
                        var userSubscription = userConcerned.Medias.FirstOrDefault(x => x.RatingKey == int.Parse(grandparentRatingKey));
                        if (userSubscription == null) continue;
                        var media = _dbContext.Medias.FirstOrDefault(x => x.RatingKey == int.Parse(grandparentRatingKey));
                        if (media == null) continue;
                        userConcerned.Medias.Add(new UserSubscription()
                        {
                            Media = media,
                            Active = true,
                        });
                    }
                    serverOwner.HistoryPosition = offset;
                }
                _ = _dbContext.SaveChangesAsync();
            }


        }
    }
}
