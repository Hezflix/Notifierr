using Microsoft.EntityFrameworkCore;

namespace PlexNotifierr.Core.Models
{
    public sealed class PlexNotifierrDbContext : DbContext
    {
        public PlexNotifierrDbContext()
        {
            Database.EnsureCreated();
            ChangeTracker.LazyLoadingEnabled = false;
        }

        public PlexNotifierrDbContext(DbContextOptions<PlexNotifierrDbContext> options) : base(options)
        {
            Database.EnsureCreated();
            ChangeTracker.LazyLoadingEnabled = false;
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

        public DbSet<Media> Medias => Set<Media>();

        public DbSet<User> Users => Set<User>();

        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    }
}