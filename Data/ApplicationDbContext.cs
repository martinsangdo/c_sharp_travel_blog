using Microsoft.EntityFrameworkCore;
using TravelBlog.Models;

namespace TravelBlog.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<BlogImage> BlogImages { get; set; }
        public DbSet<BlogTag> BlogTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.Username).IsUnique();
                e.Property(u => u.Role).HasConversion<string>();
            });

            // Blog
            modelBuilder.Entity<Blog>(e =>
            {
                e.HasOne(b => b.Author)
                    .WithMany(u => u.Blogs)
                    .HasForeignKey(b => b.AuthorId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(b => b.Status).HasConversion<string>();
                e.HasIndex(b => b.Destination);
                e.HasIndex(b => b.PublishedAt);
                e.HasIndex(b => b.IsPublic);
            });

            // Comment
            modelBuilder.Entity<Comment>(e =>
            {
                e.HasOne(c => c.Blog)
                    .WithMany(b => b.Comments)
                    .HasForeignKey(c => c.BlogId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // BlogImage
            modelBuilder.Entity<BlogImage>(e =>
            {
                e.HasOne(i => i.Blog)
                    .WithMany(b => b.Images)
                    .HasForeignKey(i => i.BlogId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BlogTag
            modelBuilder.Entity<BlogTag>(e =>
            {
                e.HasOne(t => t.Blog)
                    .WithMany(b => b.Tags)
                    .HasForeignKey(t => t.BlogId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
