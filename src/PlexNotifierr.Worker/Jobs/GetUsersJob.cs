using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Plex.Library.ApiModels.Accounts;
using PlexNotifierr.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.Api.Factories;
using PlexNotifierr.Core.Config;

namespace PlexNotifierr.Worker.Jobs
{
    /// <summary>
    /// A job to get all the friend of a Plex account.
    /// </summary>
    public class GetUsersJob
    {
        private readonly PlexAccount _account;
        private readonly PlexNotifierrDbContext _dbContext;
        private readonly IProgressBarFactory _progressBarFactory;
        private readonly ILogger _logger;

        public GetUsersJob(PlexNotifierrDbContext dbContext, IPlexFactory plexFactory, IProgressBarFactory progressBarFactory, IOptions<PlexConfig> plexConfig, ILogger<GetUsersJob> logger)
        {
            _dbContext = dbContext;
            _progressBarFactory = progressBarFactory;
            _account = plexFactory.GetPlexAccount(plexConfig.Value.AccessToken);
            _logger = logger;
        }

        [JobDisplayName("GetUsers")]
        public async Task ExecuteAsync()
        {
            var users = await _account.Friends();
            _logger.LogInformation("{UsersCount} users to proceed.", users.Count);
            var usersDb = await _dbContext.Users.ToListAsync();
            var ownerUser = usersDb.FirstOrDefault(u => u.PlexId == 1);
            if (ownerUser == null)
            {
                _ = await _dbContext.Users.AddAsync(new User()
                {
                    PlexId = 1,
                    PlexName = _account.Username,
                    Active = false,
                });
            }
            var progressBar = _progressBarFactory.Create();
            foreach (var user in users.WithProgress(progressBar))
            {
                var userDb = usersDb.FirstOrDefault(u => u.PlexId == user.Id);
                if (userDb != null)
                {
                    userDb.PlexName = user.Username;
                }
                else
                {
                    _logger.LogInformation("New user {UserUsername} added to system", user.Username);
                    _ = await _dbContext.Users.AddAsync(new User()
                    {
                        PlexId = user.Id,
                        PlexName = user.Username,
                        Active = false,
                    });
                }
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}