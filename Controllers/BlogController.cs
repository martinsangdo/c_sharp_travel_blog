using Microsoft.AspNetCore.Mvc;
using TravelBlog.Filters;
using TravelBlog.Helpers;
using TravelBlog.Services;
using TravelBlog.ViewModels;

namespace TravelBlog.Controllers
{
    public class BlogController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly IAIService _aiService;

        public BlogController(IBlogService blogService, IAIService aiService)
        {
            _blogService = blogService;
            _aiService = aiService;
        }

        // GET /Blog
        public async Task<IActionResult> Index(int page = 1, string? search = null,
            string? destination = null, string? sortBy = "newest")
        {
            var vm = await _blogService.GetPublicBlogsAsync(page, 9, search, destination, sortBy);
            return View(vm);
        }

        // GET /Blog/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var userId = AuthHelper.GetUserId(HttpContext.Session);
                var vm = await _blogService.GetDetailAsync(id, userId);

                // Only show public blogs to non-authors
                if (!vm.Blog.IsPublic && vm.Blog.AuthorId != userId && !AuthHelper.IsAdmin(HttpContext.Session))
                    return NotFound();

                await _blogService.IncrementViewCountAsync(id);
                return View(vm);
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        // GET /Blog/Create
        [RequireLogin]
        public IActionResult Create() => View(new CreateBlogViewModel());

        // POST /Blog/Create
        [HttpPost, ValidateAntiForgeryToken, RequireLogin]
        public async Task<IActionResult> Create(CreateBlogViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            try
            {
                var blog = await _blogService.CreateAsync(model, userId);
                TempData["Success"] = "Blog post created successfully!";
                return RedirectToAction(nameof(Detail), new { id = blog.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        // GET /Blog/Edit/5
        [RequireLogin]
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _blogService.GetByIdAsync(id, includeAll: true);
            if (blog == null) return NotFound();

            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            if (blog.AuthorId != userId && !AuthHelper.IsAdmin(HttpContext.Session))
                return Forbid();

            var vm = new EditBlogViewModel
            {
                Id = blog.Id,
                Title = blog.Title,
                ShortDescription = blog.ShortDescription,
                LongDescription = blog.LongDescription,
                Destination = blog.Destination,
                IsPublic = blog.IsPublic,
                Tags = string.Join(", ", blog.Tags?.Select(t => t.Name) ?? []),
                ExistingCoverImageUrl = blog.CoverImageUrl,
                ExistingImages = blog.Images?.ToList() ?? new()
            };
            return View(vm);
        }

        // POST /Blog/Edit/5
        [HttpPost, ValidateAntiForgeryToken, RequireLogin]
        public async Task<IActionResult> Edit(int id, EditBlogViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            try
            {
                await _blogService.UpdateAsync(id, model, userId);
                TempData["Success"] = "Blog post updated successfully!";
                return RedirectToAction(nameof(Detail), new { id });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(model); }
        }

        // POST /Blog/Delete/5
        [HttpPost, ValidateAntiForgeryToken, RequireLogin]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            try
            {
                await _blogService.DeleteAsync(id, userId);
                TempData["Success"] = "Blog post deleted.";
                return RedirectToAction("Profile", "Account");
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        // POST /Blog/AddComment
        [HttpPost, ValidateAntiForgeryToken, RequireLogin]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Detail), new { id = model.BlogId });
            }

            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            await _blogService.AddCommentAsync(model, userId);
            TempData["Success"] = "Comment posted!";
            return RedirectToAction(nameof(Detail), new { id = model.BlogId });
        }

        // POST /Blog/DeleteComment/5
        [HttpPost, ValidateAntiForgeryToken, RequireLogin]
        public async Task<IActionResult> DeleteComment(int commentId, int blogId)
        {
            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            await _blogService.DeleteCommentAsync(commentId, userId, AuthHelper.IsAdmin(HttpContext.Session));
            TempData["Success"] = "Comment removed.";
            return RedirectToAction(nameof(Detail), new { id = blogId });
        }

        // POST /Blog/ToggleVisibility/5
        [HttpPost, ValidateAntiForgeryToken, RequireLogin]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var userId = AuthHelper.GetUserId(HttpContext.Session)!.Value;
            await _blogService.ToggleVisibilityAsync(id, userId);
            TempData["Success"] = "Visibility updated.";
            return RedirectToAction("Profile", "Account");
        }

        // ── AI API endpoints ──────────────────────────────────

        // POST /Blog/AiIdeas
        [HttpPost, RequireLogin]
        public async Task<IActionResult> AiIdeas([FromBody] AiRequest req)
        {
            var result = await _aiService.GetBlogIdeasAsync(req.Destination ?? "any destination", req.Topic);
            return Json(new { success = true, content = result });
        }

        // POST /Blog/AiOutline
        [HttpPost, RequireLogin]
        public async Task<IActionResult> AiOutline([FromBody] AiRequest req)
        {
            var result = await _aiService.GetContentOutlineAsync(req.Title ?? "Travel Blog", req.Destination ?? "unknown");
            return Json(new { success = true, content = result });
        }

        // POST /Blog/AiAssist
        [HttpPost, RequireLogin]
        public async Task<IActionResult> AiAssist([FromBody] AiRequest req)
        {
            var result = await _aiService.GetWritingAssistanceAsync(req.Prompt ?? "Help me write a travel blog.");
            return Json(new { success = true, content = result });
        }
    }

    public class AiRequest
    {
        public string? Destination { get; set; }
        public string? Topic { get; set; }
        public string? Title { get; set; }
        public string? Prompt { get; set; }
    }
}
