using Microsoft.AspNetCore.Mvc;
using TravelBlog.Filters;
using TravelBlog.Helpers;
using TravelBlog.Services;
using TravelBlog.ViewModels;

namespace TravelBlog.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IBlogService _blogService;

        public AccountController(IUserService userService, IBlogService blogService)
        {
            _userService = userService;
            _blogService = blogService;
        }

        // GET /Account/Register
        public IActionResult Register()
        {
            if (AuthHelper.IsLoggedIn(HttpContext.Session)) return RedirectToAction("Index", "Home");
            return View();
        }

        // POST /Account/Register
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userService.GetByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(model);
            }
            if (await _userService.GetByUsernameAsync(model.Username) != null)
            {
                ModelState.AddModelError("Username", "Username is already taken.");
                return View(model);
            }

            var user = await _userService.CreateAsync(model);
            AuthHelper.SetUserSession(HttpContext.Session, user);
            TempData["Success"] = "Welcome to TravelBlog! Your account has been created.";
            return RedirectToAction("Index", "Home");
        }

        // GET /Account/Login
        public IActionResult Login(string? returnUrl)
        {
            if (AuthHelper.IsLoggedIn(HttpContext.Session)) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST /Account/Login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userService.GetByEmailAsync(model.Email);
            if (user == null || !await _userService.ValidatePasswordAsync(user, model.Password))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }
            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Please contact support.");
                return View(model);
            }

            AuthHelper.SetUserSession(HttpContext.Session, user);
            TempData["Success"] = $"Welcome back, {user.FullName ?? user.Username}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.Role == Models.UserRole.Admin
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // GET /Account/Logout
        public IActionResult Logout()
        {
            AuthHelper.ClearSession(HttpContext.Session);
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // GET /Account/Profile
        [RequireLogin]
        public async Task<IActionResult> Profile()
        {
            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return RedirectToAction("Logout");

            var blogsVm = await _blogService.GetUserBlogsAsync(userId, 1, 20);

            var vm = new ProfileViewModel
            {
                User = user,
                MyBlogs = blogsVm.Blogs,
                TotalBlogs = blogsVm.TotalCount,
                TotalComments = user.Comments.Count,
                TotalViews = blogsVm.Blogs.Sum(b => b.ViewCount)
            };
            return View(vm);
        }

        // GET /Account/EditProfile
        [RequireLogin]
        public async Task<IActionResult> EditProfile()
        {
            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return RedirectToAction("Logout");

            return View(new EditProfileViewModel
            {
                FullName = user.FullName ?? "",
                Bio = user.Bio,
                ExistingAvatarUrl = user.AvatarUrl
            });
        }

        // POST /Account/EditProfile
        [HttpPost, ValidateAntiForgeryToken, RequireLogin]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            await _userService.UpdateProfileAsync(userId, model);

            // Refresh session name
            var user = await _userService.GetByIdAsync(userId);
            if (user != null) AuthHelper.SetUserSession(HttpContext.Session, user);

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        public IActionResult AccessDenied() => View();
    }
}
