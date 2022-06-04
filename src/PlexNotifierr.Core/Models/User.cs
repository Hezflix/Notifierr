using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlexNotifierr.Core.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("plex_id")]
        public int PlexId { get; set; }

        [Column("plex_name")]
        public string PlexName { get; set; } = "";

        [Column("active")]
        public bool Active { get; set; }

        [Column("discord_id")]
        public string? DiscordId { get; set; }

        [Column("history_position")]
        public int HistoryPosition { get; set; } = 0;

        public ICollection<UserSubscription> Medias { get; set; } = new List<UserSubscription>();
    }
}
