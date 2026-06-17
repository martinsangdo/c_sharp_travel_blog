using Microsoft.AspNetCore.Mvc;
using TravelBlog.Filters;
using TravelBlog.Services;
using TravelBlog.ViewModels;
using TravelBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace TravelBlog.Controllers
{
    [RequireAdmin]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IBlogService _blogService;
        private readonly ApplicationDbContext _db;

        public AdminController(IUserService userService, IBlogService blogService, ApplicationDbContext db)
        {
            _userService = userService;
            _blogService = blogService;
            _db = db;
        }

        // GET /Admin
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => now.Date.AddDays(-6 + i))
                .ToList();

            var rangeStart = last7Days.First();
            var dailyCounts = await _db.Blogs
                .Where(b => !b.IsDeleted && b.PublishedAt >= rangeStart)
                .GroupBy(b => b.PublishedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var topUsers = await _db.Blogs
                .Where(b => !b.IsDeleted && b.Author != null)
                .GroupBy(b => new { b.Author!.FullName, b.Author.Username })
                .Select(g => new { Name = g.Key.FullName ?? g.Key.Username, Views = g.Sum(b => b.ViewCount) })
                .OrderByDescending(x => x.Views)
                .Take(10)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(u => !u.IsDeleted),
                TotalBlogs = await _db.Blogs.CountAsync(b => !b.IsDeleted),
                TotalComments = await _db.Comments.CountAsync(c => !c.IsDeleted),
                PublicBlogs = await _db.Blogs.CountAsync(b => !b.IsDeleted && b.IsPublic && b.Status == Models.BlogStatus.Published),
                NewUsersThisMonth = await _db.Users.CountAsync(u => !u.IsDeleted && u.CreatedAt >= monthStart),
                RecentBlogs = (await _blogService.GetAllBlogsAdminAsync(1, 5, null)).Blogs,
                RecentUsers = await _userService.GetAllUsersAsync(1, 5, null),
                BlogChartLabels = last7Days.Select(d => d.ToString("MMM d")).ToList(),
                BlogChartData = last7Days.Select(d => dailyCounts.FirstOrDefault(x => x.Date == d)?.Count ?? 0).ToList(),
                TopUsersLabels = topUsers.Select(x => x.Name).ToList(),
                TopUsersViewCounts = topUsers.Select(x => x.Views).ToList()
            };
            return View(vm);
        }

        // GET /Admin/Users
        public async Task<IActionResult> Users(int page = 1, string? search = null)
        {
            var users = await _userService.GetAllUsersAsync(page, 15, search);
            var total = await _userService.CountUsersAsync(search);
            ViewBag.Users = users;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / 15);
            ViewBag.Search = search;
            ViewBag.TotalCount = total;
            return View();
        }

        // POST /Admin/ToggleUser/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId.HasValue && id == currentUserId.Value)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }
            await _userService.ToggleUserActiveAsync(id);
            TempData["Success"] = "User status updated.";
            return RedirectToAction(nameof(Users));
        }

        // POST /Admin/DeleteUser/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId.HasValue && id == currentUserId.Value)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }
            try
            {
                await _userService.DeleteUserAsync(id);
                TempData["Success"] = "User deleted.";
            }
            catch (KeyNotFoundException) { TempData["Error"] = "User not found."; }
            return RedirectToAction(nameof(Users));
        }

        // GET /Admin/Blogs
        public async Task<IActionResult> Blogs(int page = 1, string? search = null)
        {
            var vm = await _blogService.GetAllBlogsAdminAsync(page, 15, search);
            return View(vm);
        }

        // GET /Admin/BlogComments/5
        public async Task<IActionResult> BlogComments(int id)
        {
            var blog = await _db.Blogs.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (blog == null) return NotFound();

            var comments = await _db.Comments
                .Where(c => c.BlogId == id)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Blog = blog;
            return View(comments);
        }

        // POST /Admin/DeleteComment/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int blogId)
        {
            var comment = await _db.Comments.FindAsync(commentId);
            if (comment != null)
            {
                comment.IsDeleted = true;
                comment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Comment deleted.";
            }
            return RedirectToAction(nameof(BlogComments), new { id = blogId });
        }

        // POST /Admin/DeleteBlog/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            try
            {
                await _blogService.DeleteAsync(id, 0, isAdmin: true);
                TempData["Success"] = "Blog post deleted.";
            }
            catch (KeyNotFoundException) { TempData["Error"] = "Blog not found."; }
            return RedirectToAction(nameof(Blogs));
        }

        // GET /Admin/CreateAdmin
        public IActionResult CreateAdmin() => View(new CreateAdminViewModel());

        // POST /Admin/CreateAdmin
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userService.GetByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email already in use.");
                return View(model);
            }

            await _userService.CreateAdminAsync(model);
            TempData["Success"] = $"Admin account '{model.Username}' created successfully.";
            return RedirectToAction(nameof(Users));
        }
    }
}
