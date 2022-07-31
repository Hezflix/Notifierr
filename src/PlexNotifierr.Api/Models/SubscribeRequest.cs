namespace PlexNotifierr.Api.Models
{
    public class SubscribeRequest
    {
        public string DiscordId { get; set; } = "";
        public string? PlexName { get; set; }
    }
}