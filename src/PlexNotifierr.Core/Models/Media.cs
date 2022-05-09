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

        [Column("media_index")]
        public int? MediaIndex { get; set; }

        [Column("media_type")]
        public MediaType MediaType { get; set; }

        [Column("parent_rating_key")]
        public int? ParentRatingKey { get; set; }

        [Column("parent_media_index")]
        public int? ParentMediaIndex { get; set; }

        [Column("grand_parent_rating_key")]
        public int? GrandParentRatingKey { get; set; }

        public ICollection<UserSubscription> Users { get; set; } = new List<UserSubscription>();
    }
}
