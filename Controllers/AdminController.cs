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

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalBlogs = await _db.Blogs.CountAsync(b => !b.IsDeleted),
                TotalComments = await _db.Comments.CountAsync(c => !c.IsDeleted),
                PublicBlogs = await _db.Blogs.CountAsync(b => !b.IsDeleted && b.IsPublic && b.Status == Models.BlogStatus.Published),
                NewUsersThisMonth = await _db.Users.CountAsync(u => u.CreatedAt >= monthStart),
                RecentBlogs = (await _blogService.GetAllBlogsAdminAsync(1, 5, null)).Blogs,
                RecentUsers = await _userService.GetAllUsersAsync(1, 5, null)
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
            await _userService.ToggleUserActiveAsync(id);
            TempData["Success"] = "User status updated.";
            return RedirectToAction(nameof(Users));
        }

        // GET /Admin/Blogs
        public async Task<IActionResult> Blogs(int page = 1, string? search = null)
        {
            var vm = await _blogService.GetAllBlogsAdminAsync(page, 15, search);
            return View(vm);
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
