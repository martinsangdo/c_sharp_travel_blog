using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBlog.Models
{
    public class Blog
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string LongDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Destination { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public bool IsPublic { get; set; } = true;

        public BlogStatus Status { get; set; } = BlogStatus.Published;

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int ViewCount { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // Foreign key
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; }

        // Navigation
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<BlogImage> Images { get; set; } = new List<BlogImage>();
        public virtual ICollection<BlogTag> Tags { get; set; } = new List<BlogTag>();
    }

    public enum BlogStatus
    {
        Draft = 0,
        Published = 1,
        Hidden = 2
    }
}
