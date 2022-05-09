using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlexNotifierr.Core.Models
{
    [Table("user_subscriptions")]
    public class UserSubscription
    {
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("rating_key")]
        public int RatingKey { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        public User User { get; set; } = null!;

        public Media Media { get; set; } = null!;

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<UserSubscription>()
                        .HasKey(us => new { us.UserId, us.RatingKey });

            _ = modelBuilder.Entity<UserSubscription>()
                            .HasOne(us => us.User)
                            .WithMany(u => u.Medias)
                            .HasForeignKey(us => us.UserId);

            _ = modelBuilder.Entity<UserSubscription>()
                            .HasOne(us => us.Media)
                            .WithMany(m => m.Users)
                            .HasForeignKey(m => m.RatingKey);
        }
    }
}
