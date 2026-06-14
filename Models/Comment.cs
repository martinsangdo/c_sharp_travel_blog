using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBlog.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required, MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Foreign keys
        public int BlogId { get; set; }
        public int UserId { get; set; }

        [ForeignKey("BlogId")]
        public virtual Blog? Blog { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    public class BlogImage
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Caption { get; set; }

        public int SortOrder { get; set; } = 0;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public int BlogId { get; set; }

        [ForeignKey("BlogId")]
        public virtual Blog? Blog { get; set; }
    }

    public class BlogTag
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Foreign key
        public int BlogId { get; set; }

        [ForeignKey("BlogId")]
        public virtual Blog? Blog { get; set; }
    }
}
