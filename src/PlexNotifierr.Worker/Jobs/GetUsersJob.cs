using Microsoft.EntityFrameworkCore;
using Plex.Library.ApiModels.Accounts;
using PlexNotifierr.Core.Models;
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

        public GetUsersJob(PlexNotifierrDbContext dbContext, PlexAccount account)
        {
            _dbContext = dbContext;
            _account = account;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            var users = await _account.Friends();
            var usersDb = await _dbContext.Users.ToListAsync();
            foreach (var user in users)
            {
                var userDb = usersDb.Where(u => u.PlexId == user.Id).FirstOrDefault();
                if (userDb != null)
                {
                    userDb.PlexName = user.Username;
                }
                else
                {
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
