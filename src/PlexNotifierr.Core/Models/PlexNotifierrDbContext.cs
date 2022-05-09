using Microsoft.EntityFrameworkCore;

namespace PlexNotifierr.Core.Models
{
    public class PlexNotifierrDbContext : DbContext
    {
        public PlexNotifierrDbContext() : base()
        {
            this.ChangeTracker.LazyLoadingEnabled = false;
        }

        public PlexNotifierrDbContext(DbContextOptions<PlexNotifierrDbContext> options) : base(options)
        {
            this.ChangeTracker.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            UserSubscription.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured == false)
                options.UseSqlite("PlexNotifierr.db");
        }

        public DbSet<Media> Medias
        {
            get
            {
                return this.Set<Media>();
            }
        }

        public DbSet<User> Users
        {
            get
            {
                return this.Set<User>();
            }
        }

        public DbSet<UserSubscription> UserSubscriptions
        {
            get
            {
                return this.Set<UserSubscription>();
            }
        }
    }
}
