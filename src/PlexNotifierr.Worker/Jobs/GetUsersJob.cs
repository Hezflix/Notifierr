using Microsoft.EntityFrameworkCore;
using Plex.Library.ApiModels.Accounts;
using PlexNotifierr.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plex.Api.Factories;
using PlexNotifierr.Core.Config;
using Quartz;

namespace PlexNotifierr.Worker.Jobs
{
    /// <summary>
    /// A job to get all the friend of a Plex account.
    /// </summary>
    [DisallowConcurrentExecution]
    public class GetUsersJob : IJob
    {
        private readonly PlexAccount _account;

        private readonly PlexNotifierrDbContext _dbContext;

        private readonly ILogger _logger;

        public GetUsersJob(PlexNotifierrDbContext dbContext, IPlexFactory plexFactory, IOptions<PlexConfig> plexConfig, ILogger<GetUsersJob> logger)
        {
            _dbContext = dbContext;
            _account = plexFactory.GetPlexAccount(plexConfig.Value.AccessToken);
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var users = await _account.Friends();
            _logger.LogInformation($"{users.Count + 1} users to proceed.");
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
            foreach (var user in users)
            {
                var userDb = usersDb.FirstOrDefault(u => u.PlexId == user.Id);
                if (userDb != null)
                {
                    userDb.PlexName = user.Username;
                }
                else
                {
                    _logger.LogInformation($"New user {user.Username} added to system");
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