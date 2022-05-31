namespace PlexNotifierr.Api
{
    public class PlexConfig
    {
        public string? Product { get; set; }

        public string? DeviceName { get; set; }

        public string? ClientId { get; set; }

        public string ServerUrl { get; set; } = String.Empty;

        public string AccessToken { get; set; } = String.Empty;
    }
}
