using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.ServerApi.Clients.Interfaces;
using Plex.ServerApi.Enums;
using PlexNotifierr.Core.Config;
using PlexNotifierr.Core.Messaging;
using PlexNotifierr.Core.Models;

namespace PlexNotifierr.Worker.Jobs
{
    /// <summary>
    /// A job to get recently added episode of show and notify user.
    /// </summary>
    public class GetRecentlyAddedJob
    {
        private readonly PlexNotifierrDbContext _dbContext;
        private readonly IPlexServerClient _serverClient;
        private readonly INotificationSender _notificationSender;
        private readonly IProgressBarFactory _progressBarFactory;
        private readonly string _url;
        private readonly string _authToken;
        private readonly ILogger _logger;


        public GetRecentlyAddedJob(PlexNotifierrDbContext dbContext, IPlexServerClient plexServerClient, INotificationSender notificationSender, IProgressBarFactory progressBarFactory, IOptions<PlexConfig> plexConfig, ILogger<GetUsersJob> logger)
        {
            _dbContext = dbContext;
            _serverClient = plexServerClient;
            _notificationSender = notificationSender;
            _progressBarFactory = progressBarFactory;
            _url = plexConfig.Value.ServerUrl;
            _authToken = plexConfig.Value.AccessToken;
            _logger = logger;
        }

        [JobDisplayName("GetRecentlyAdded")]
        public async Task ExecuteAsync()
        {
            var libraries = await _serverClient.GetLibrariesAsync(_authToken, _url);
            _logger.LogInformation("{LibrariesCount} to process", libraries.Libraries.Count);
            foreach (var library in libraries.Libraries.Where(library => library.Type == "show").ToList())
            {
                _logger.LogInformation("Start library {LibraryTitle}", library.Title);
                var progressBar = _progressBarFactory.Create();
                var recentlyAddedEpisodes = await _serverClient.GetLibraryRecentlyAddedAsync(_authToken, _url, SearchType.Episode, library.Key, 0, 100);
                if (recentlyAddedEpisodes?.Media is null) continue;
                foreach (var recentlyAddedShow in recentlyAddedEpisodes.Media.GroupBy(x => x.GrandparentRatingKey)
                                                                       .Select(x => new
                                                                        {
                                                                            RatingKey = x.Key,
                                                                            LastEpisode = x.MaxBy(y => y.OriginallyAvailableAt),
                                                                        }).ToList().WithProgress(progressBar))
                {
                    if (!DateTime.TryParse(recentlyAddedShow.LastEpisode?.OriginallyAvailableAt, out var originallyAvailableAt)
                        || !int.TryParse(recentlyAddedShow.RatingKey, out var grandParentRatingKey)) continue;
                    var show = _dbContext.Medias.Include(x => x.Users).ThenInclude(y => y.User).FirstOrDefault(x => x.RatingKey == grandParentRatingKey);
                    var addedAt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(recentlyAddedShow.LastEpisode.AddedAt).ToUniversalTime();
                    var minDateShow = new DateTime(Math.Min(originallyAvailableAt.Ticks, addedAt.Ticks));
                    if (show is null || minDateShow < show.LastNotified) continue;
                    var users = show.Users.Where(x => x.User.Active).Select(x => x.User);
                    var success = true;
                    foreach (var user in users)
                    {
                        if (user.DiscordId is null) continue;
                        _logger.LogInformation("Notification send for user {UserPlexName} on show {ShowTitle}", user.PlexName, show.Title);
                        var successfulSend = _notificationSender.TrySendMessage(user.DiscordId, show, recentlyAddedShow.LastEpisode);
                        if (success && !successfulSend) success = false;
                    }
                    if (success) show.LastNotified = DateTime.Now;
                }
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}