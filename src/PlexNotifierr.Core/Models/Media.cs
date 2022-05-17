using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlexNotifierr.Core.Models
{
    [Table("medias")]
    public class Media
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("rating_key")]
        public int RatingKey { get; set; }

        [Column("title")]
        public string Title { get; set; } = "";

        [Column("summary")]
        public string Summary { get; set; } = "";

        [Column("thumb")]
        public string ThumbUrl { get; set; } = "";

        [Column("last_notified")]
        public DateTime LastNotified { get; set; } = DateTime.MinValue;

        public ICollection<UserSubscription> Users { get; set; } = new List<UserSubscription>();
    }
}
