using luke_site_mvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace luke_site_mvc.Data
{
    public class SubredditContext : DbContext
    {
        public SubredditContext(DbContextOptions<SubredditContext> options)
            : base(options)
        {
        }

        public DbSet<RedditComment> RedditComments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RedditComment>()
                .HasIndex(c => new { c.Subreddit, c.YoutubeLinkId })
                .IsUnique();
        }
    }
}
