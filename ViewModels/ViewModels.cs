using System.ComponentModel.DataAnnotations;
using TravelBlog.Models;

namespace TravelBlog.ViewModels
{
    // ── Auth ──────────────────────────────────────────────────
    public class RegisterViewModel
    {
        [Required, MaxLength(50)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    // ── Blog ──────────────────────────────────────────────────
    public class CreateBlogViewModel
    {
        [Required, MaxLength(200)]
        [Display(Name = "Blog Title")]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        [Display(Name = "Short Description")]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Content")]
        public string LongDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Travel Destination")]
        public string? Destination { get; set; }

        [Display(Name = "Tags (comma-separated)")]
        public string? Tags { get; set; }

        [Display(Name = "Make Public")]
        public bool IsPublic { get; set; } = true;

        [Display(Name = "Cover Image")]
        public IFormFile? CoverImage { get; set; }

        [Display(Name = "Additional Images")]
        public List<IFormFile>? AdditionalImages { get; set; }
    }

    public class EditBlogViewModel : CreateBlogViewModel
    {
        public int Id { get; set; }
        public string? ExistingCoverImageUrl { get; set; }
        public List<BlogImage> ExistingImages { get; set; } = new();
        public List<int> DeleteImageIds { get; set; } = new();
    }

    public class BlogListViewModel
    {
        public List<BlogCardViewModel> Blogs { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public string? SearchQuery { get; set; }
        public string? Destination { get; set; }
        public string? SortBy { get; set; }
        public List<string> PopularDestinations { get; set; } = new();
    }

    public class BlogCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string? Destination { get; set; }
        public string? CoverImageUrl { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }
        public DateTime PublishedAt { get; set; }
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsPublic { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class BlogDetailViewModel
    {
        public Blog Blog { get; set; } = null!;
        public List<CommentViewModel> Comments { get; set; } = new();
        public bool CanEdit { get; set; }
        public string NewComment { get; set; } = string.Empty;
        public List<BlogCardViewModel> RelatedBlogs { get; set; } = new();
    }

    // ── Comment ───────────────────────────────────────────────
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatar { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanDelete { get; set; }
    }

    public class AddCommentViewModel
    {
        public int BlogId { get; set; }

        [Required, MinLength(10, ErrorMessage = "Comment must be at least 10 characters."), MaxLength(1000)]
        [Display(Name = "Your Comment")]
        public string Content { get; set; } = string.Empty;
    }

    // ── Admin ─────────────────────────────────────────────────
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalBlogs { get; set; }
        public int TotalComments { get; set; }
        public int PublicBlogs { get; set; }
        public int NewUsersThisMonth { get; set; }
        public List<BlogCardViewModel> RecentBlogs { get; set; } = new();
        public List<UserSummaryViewModel> RecentUsers { get; set; } = new();
    }

    public class UserSummaryViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int BlogCount { get; set; }
    }

    public class CreateAdminViewModel
    {
        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ── Profile ───────────────────────────────────────────────
    public class ProfileViewModel
    {
        public User User { get; set; } = null!;
        public List<BlogCardViewModel> MyBlogs { get; set; } = new();
        public int TotalBlogs { get; set; }
        public int TotalComments { get; set; }
        public int TotalViews { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? Avatar { get; set; }

        public string? ExistingAvatarUrl { get; set; }
    }
}
