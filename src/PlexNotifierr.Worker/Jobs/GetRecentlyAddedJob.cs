using Microsoft.EntityFrameworkCore;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.Enums;
using PlexNotifierr.Core.Messaging;
using PlexNotifierr.Core.Models;
using Quartz;

namespace PlexNotifierr.Worker.Jobs
{
    public class GetRecentlyAddedJob : IJob
    {
        private readonly PlexNotifierrDbContext _dbContext;
        private readonly IPlexServerClient _serverClient;
        private readonly INotificationSender _notificationSender;
        private readonly string _url;
        private readonly string _authToken;

        public GetRecentlyAddedJob(PlexNotifierrDbContext dbContext, IPlexServerClient plexServerClient, INotificationSender notificationSender, string url, string authToken)
        {
            _dbContext = dbContext;
            _serverClient = plexServerClient;
            _notificationSender = notificationSender;
            _url = url;
            _authToken = authToken;
        }


        public async Task Execute(IJobExecutionContext context)
        {
            var libraries = await _serverClient.GetLibrariesAsync(_authToken, _url);
            foreach (var library in libraries.Libraries.Where(library => library.Type == "show"))
            {
                var recentlyAddedEpisodes = await _serverClient.GetLibraryRecentlyAddedAsync(_authToken, _url, SearchType.Episode, library.Key, 0, 100);
                if (recentlyAddedEpisodes?.Media is null) continue;
                foreach (var recentlyAddedShow in recentlyAddedEpisodes.Media.GroupBy(x => x.GrandparentRatingKey)
                         .Select(x => new { RatingKey = x.Key, MaxOriginalityAvailableAt = x.MaxBy(x => x.OriginallyAvailableAt)?.OriginallyAvailableAt }))
                {
                    if (!DateTime.TryParse(recentlyAddedShow.MaxOriginalityAvailableAt, out var originallyAvailableAt)
                        || !int.TryParse(recentlyAddedShow.RatingKey, out var grandParentRatingKey)) continue;
                    var show = _dbContext.Medias.Include(x => x.Users).ThenInclude(y => y.User).FirstOrDefault(x => x.RatingKey == grandParentRatingKey);
                    if (show is null || originallyAvailableAt < show.LastNotified) continue;
                    var discordIds = show.Users.Select(x => x.User.DiscordId);
                    var success = true;
                    foreach (var discordId in discordIds)
                    {
                        if (discordId is null) continue;
                        var successfulSend = _notificationSender.TrySendMessage(discordId, show);
                        if (success && !successfulSend) success = false;
                    }
                    if (success) show.LastNotified = DateTime.Now;
                }
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
