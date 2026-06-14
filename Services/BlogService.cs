using Microsoft.EntityFrameworkCore;
using TravelBlog.Data;
using TravelBlog.Models;
using TravelBlog.ViewModels;

namespace TravelBlog.Services
{
    public interface IBlogService
    {
        Task<BlogListViewModel> GetPublicBlogsAsync(int page, int pageSize, string? search, string? destination, string? sortBy);
        Task<BlogListViewModel> GetUserBlogsAsync(int userId, int page, int pageSize);
        Task<BlogListViewModel> GetAllBlogsAdminAsync(int page, int pageSize, string? search);
        Task<Blog?> GetByIdAsync(int id, bool includeAll = false);
        Task<BlogDetailViewModel> GetDetailAsync(int id, int? currentUserId);
        Task<Blog> CreateAsync(CreateBlogViewModel model, int authorId);
        Task UpdateAsync(int id, EditBlogViewModel model, int currentUserId);
        Task DeleteAsync(int id, int currentUserId, bool isAdmin = false);
        Task<Comment> AddCommentAsync(AddCommentViewModel model, int userId);
        Task DeleteCommentAsync(int commentId, int userId, bool isAdmin = false);
        Task<List<string>> GetPopularDestinationsAsync(int count = 10);
        Task IncrementViewCountAsync(int blogId);
        Task ToggleVisibilityAsync(int blogId, int userId);
    }

    public class BlogService : IBlogService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileService;

        public BlogService(ApplicationDbContext db, IFileUploadService fileService)
        {
            _db = db;
            _fileService = fileService;
        }

        public async Task<BlogListViewModel> GetPublicBlogsAsync(int page, int pageSize, string? search, string? destination, string? sortBy)
        {
            var query = _db.Blogs
                .Where(b => b.IsPublic && b.Status == BlogStatus.Published)
                .Include(b => b.Author)
                .Include(b => b.Comments)
                .Include(b => b.Tags)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.ShortDescription.Contains(search) || (b.Destination != null && b.Destination.Contains(search)));

            if (!string.IsNullOrWhiteSpace(destination))
                query = query.Where(b => b.Destination != null && b.Destination.Contains(destination));

            query = sortBy switch
            {
                "views" => query.OrderByDescending(b => b.ViewCount),
                "comments" => query.OrderByDescending(b => b.Comments.Count),
                _ => query.OrderByDescending(b => b.PublishedAt)
            };

            var total = await query.CountAsync();
            var blogs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new BlogListViewModel
            {
                Blogs = blogs.Select(MapToCard).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalCount = total,
                SearchQuery = search,
                Destination = destination,
                SortBy = sortBy,
                PopularDestinations = await GetPopularDestinationsAsync()
            };
        }

        public async Task<BlogListViewModel> GetUserBlogsAsync(int userId, int page, int pageSize)
        {
            var query = _db.Blogs
                .Where(b => b.AuthorId == userId)
                .Include(b => b.Author)
                .Include(b => b.Comments)
                .Include(b => b.Tags)
                .OrderByDescending(b => b.PublishedAt);

            var total = await query.CountAsync();
            var blogs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new BlogListViewModel
            {
                Blogs = blogs.Select(MapToCard).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalCount = total
            };
        }

        public async Task<BlogListViewModel> GetAllBlogsAdminAsync(int page, int pageSize, string? search)
        {
            var query = _db.Blogs
                .Include(b => b.Author)
                .Include(b => b.Comments)
                .Include(b => b.Tags)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || (b.Author != null && b.Author.Username.Contains(search)));

            query = query.OrderByDescending(b => b.PublishedAt);

            var total = await query.CountAsync();
            var blogs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new BlogListViewModel
            {
                Blogs = blogs.Select(MapToCard).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalCount = total,
                SearchQuery = search
            };
        }

        public async Task<Blog?> GetByIdAsync(int id, bool includeAll = false)
        {
            if (!includeAll) return await _db.Blogs.FindAsync(id);

            return await _db.Blogs
                .Include(b => b.Author)
                .Include(b => b.Images)
                .Include(b => b.Tags)
                .Include(b => b.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BlogDetailViewModel> GetDetailAsync(int id, int? currentUserId)
        {
            var blog = await GetByIdAsync(id, includeAll: true)
                ?? throw new KeyNotFoundException("Blog not found");

            var related = await _db.Blogs
                .Where(b => b.Id != id && b.IsPublic && b.Status == BlogStatus.Published
                    && b.Destination == blog.Destination)
                .Include(b => b.Author).Include(b => b.Tags)
                .Take(3).ToListAsync();

            return new BlogDetailViewModel
            {
                Blog = blog,
                Comments = blog.Comments
                    .Where(c => !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentViewModel
                    {
                        Id = c.Id,
                        Content = c.Content,
                        AuthorName = c.User?.FullName ?? c.User?.Username ?? "Unknown",
                        AuthorAvatar = c.User?.AvatarUrl,
                        CreatedAt = c.CreatedAt,
                        CanDelete = currentUserId.HasValue && (c.UserId == currentUserId || IsAdmin(currentUserId.Value))
                    }).ToList(),
                CanEdit = currentUserId.HasValue && (blog.AuthorId == currentUserId || IsAdmin(currentUserId.Value)),
                RelatedBlogs = related.Select(MapToCard).ToList()
            };
        }

        public async Task<Blog> CreateAsync(CreateBlogViewModel model, int authorId)
        {
            var blog = new Blog
            {
                Title = model.Title.Trim(),
                ShortDescription = model.ShortDescription.Trim(),
                LongDescription = model.LongDescription,
                Destination = model.Destination?.Trim(),
                IsPublic = model.IsPublic,
                AuthorId = authorId,
                PublishedAt = DateTime.UtcNow
            };

            if (model.CoverImage != null)
                blog.CoverImageUrl = await _fileService.UploadAsync(model.CoverImage, "blogs");

            if (!string.IsNullOrWhiteSpace(model.Tags))
                blog.Tags = ParseTags(model.Tags, blog);

            _db.Blogs.Add(blog);
            await _db.SaveChangesAsync();

            if (model.AdditionalImages?.Any() == true)
            {
                int order = 0;
                foreach (var img in model.AdditionalImages.Where(f => f.Length > 0))
                {
                    var url = await _fileService.UploadAsync(img, "blogs");
                    _db.BlogImages.Add(new BlogImage { BlogId = blog.Id, ImageUrl = url, SortOrder = order++ });
                }
                await _db.SaveChangesAsync();
            }

            return blog;
        }

        public async Task UpdateAsync(int id, EditBlogViewModel model, int currentUserId)
        {
            var blog = await _db.Blogs.Include(b => b.Tags).Include(b => b.Images).FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new KeyNotFoundException("Blog not found");

            if (blog.AuthorId != currentUserId && !IsAdmin(currentUserId))
                throw new UnauthorizedAccessException("You cannot edit this blog.");

            blog.Title = model.Title.Trim();
            blog.ShortDescription = model.ShortDescription.Trim();
            blog.LongDescription = model.LongDescription;
            blog.Destination = model.Destination?.Trim();
            blog.IsPublic = model.IsPublic;
            blog.UpdatedAt = DateTime.UtcNow;

            if (model.CoverImage != null)
            {
                if (!string.IsNullOrEmpty(blog.CoverImageUrl)) _fileService.DeleteFile(blog.CoverImageUrl);
                blog.CoverImageUrl = await _fileService.UploadAsync(model.CoverImage, "blogs");
            }

            // Remove deleted images
            if (model.DeleteImageIds?.Any() == true)
            {
                var toDelete = blog.Images.Where(i => model.DeleteImageIds.Contains(i.Id)).ToList();
                foreach (var img in toDelete) { _fileService.DeleteFile(img.ImageUrl); _db.BlogImages.Remove(img); }
            }

            // Add new images
            if (model.AdditionalImages?.Any() == true)
            {
                int order = blog.Images.Count;
                foreach (var img in model.AdditionalImages.Where(f => f.Length > 0))
                {
                    var url = await _fileService.UploadAsync(img, "blogs");
                    _db.BlogImages.Add(new BlogImage { BlogId = blog.Id, ImageUrl = url, SortOrder = order++ });
                }
            }

            // Update tags
            _db.BlogTags.RemoveRange(blog.Tags);
            if (!string.IsNullOrWhiteSpace(model.Tags))
                blog.Tags = ParseTags(model.Tags, blog);

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, int currentUserId, bool isAdmin = false)
        {
            var blog = await _db.Blogs.Include(b => b.Images).FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new KeyNotFoundException("Blog not found");

            if (!isAdmin && blog.AuthorId != currentUserId)
                throw new UnauthorizedAccessException("You cannot delete this blog.");

            foreach (var img in blog.Images) _fileService.DeleteFile(img.ImageUrl);
            if (!string.IsNullOrEmpty(blog.CoverImageUrl)) _fileService.DeleteFile(blog.CoverImageUrl);

            _db.Blogs.Remove(blog);
            await _db.SaveChangesAsync();
        }

        public async Task<Comment> AddCommentAsync(AddCommentViewModel model, int userId)
        {
            var blog = await _db.Blogs.FindAsync(model.BlogId)
                ?? throw new KeyNotFoundException("Blog not found");

            var comment = new Comment
            {
                BlogId = model.BlogId,
                UserId = userId,
                Content = model.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();
            return comment;
        }

        public async Task DeleteCommentAsync(int commentId, int userId, bool isAdmin = false)
        {
            var comment = await _db.Comments.FindAsync(commentId)
                ?? throw new KeyNotFoundException("Comment not found");

            if (!isAdmin && comment.UserId != userId)
                throw new UnauthorizedAccessException("You cannot delete this comment.");

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<List<string>> GetPopularDestinationsAsync(int count = 10)
            => await _db.Blogs
                .Where(b => b.IsPublic && b.Destination != null)
                .GroupBy(b => b.Destination!)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToListAsync();

        public async Task IncrementViewCountAsync(int blogId)
        {
            var blog = await _db.Blogs.FindAsync(blogId);
            if (blog != null) { blog.ViewCount++; await _db.SaveChangesAsync(); }
        }

        public async Task ToggleVisibilityAsync(int blogId, int userId)
        {
            var blog = await _db.Blogs.FindAsync(blogId)
                ?? throw new KeyNotFoundException("Blog not found");
            if (blog.AuthorId != userId && !IsAdmin(userId))
                throw new UnauthorizedAccessException();
            blog.IsPublic = !blog.IsPublic;
            blog.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ── helpers ───────────────────────────────────────────

        private static BlogCardViewModel MapToCard(Blog b) => new()
        {
            Id = b.Id,
            Title = b.Title,
            ShortDescription = b.ShortDescription,
            Destination = b.Destination,
            CoverImageUrl = b.CoverImageUrl,
            AuthorName = b.Author?.FullName ?? b.Author?.Username ?? "Unknown",
            AuthorAvatar = b.Author?.AvatarUrl,
            PublishedAt = b.PublishedAt,
            CommentCount = b.Comments?.Count(c => !c.IsDeleted) ?? 0,
            ViewCount = b.ViewCount,
            IsPublic = b.IsPublic,
            Tags = b.Tags?.Select(t => t.Name).ToList() ?? new()
        };

        private static ICollection<BlogTag> ParseTags(string raw, Blog blog)
            => raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .Take(10)
                .Select(t => new BlogTag { Name = t.ToLower(), Blog = blog })
                .ToList();

        private bool IsAdmin(int userId)
            => _db.Users.Any(u => u.Id == userId && u.Role == UserRole.Admin);
    }
}
