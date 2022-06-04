namespace PlexNotifierr.Api.Models
{
    public class SubscribeRequest
    {
        public string discordId { get; set; } = "";
        public string? plexName { get; set; }
    }
}
