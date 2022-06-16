using Microsoft.AspNetCore.Mvc;
using PlexNotifierr.Core.Models;
using PlexNotifierr.Api.Models;

namespace PlexNotifierr.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SubscribeManagement : ControllerBase
    {
        private readonly ILogger<SubscribeManagement> _logger;
        private readonly PlexNotifierrDbContext _dbContext;

        public SubscribeManagement(PlexNotifierrDbContext dbContext, ILogger<SubscribeManagement> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpPost("/subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
        {
            var user = _dbContext.Users.FirstOrDefault(user => user.DiscordId == req.discordId);
            if (user is null)
            {
                if (req.plexName is null)
                {
                    return NotFound();
                }
                user = _dbContext.Users.FirstOrDefault(user => user.PlexName == req.plexName);
                if (user is null)
                {
                    return NotFound();
                }
                user.DiscordId = req.discordId;
            }
            user.Active = true;
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("/unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest req)
        {
            var user = _dbContext.Users.FirstOrDefault(user => user.DiscordId == req.discordId);
            if (user is null)
            {
                return NotFound();
            }
            user.Active = false;
            await _dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
