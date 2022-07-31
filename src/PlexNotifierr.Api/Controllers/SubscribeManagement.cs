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

        [HttpPost("/subscription")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
        {
            var user = _dbContext.Users.FirstOrDefault(user => user.DiscordId == req.DiscordId);
            if (user is null)
            {
                if (req.PlexName is null)
                {
                    return NotFound();
                }
                user = _dbContext.Users.FirstOrDefault(user => user.PlexName == req.PlexName);
                if (user is null)
                {
                    return NotFound();
                }
                user.DiscordId = req.DiscordId;
            }
            user.Active = true;
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("/subscription")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest req)
        {
            var user = _dbContext.Users.FirstOrDefault(user => user.DiscordId == req.DiscordId);
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
