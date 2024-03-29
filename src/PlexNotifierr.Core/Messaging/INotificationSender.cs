using Plex.ServerApi.PlexModels.Media;
using PlexNotifierr.Core.Models;

namespace PlexNotifierr.Core.Messaging
{
    public interface INotificationSender
    {
        public bool TrySendMessage(string discordId, Media media, Metadata episode);
    }
}