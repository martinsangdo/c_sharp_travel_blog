using Microsoft.AspNetCore.Mvc;
using TravelBlog.Services;

namespace TravelBlog.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBlogService _blogService;

        public HomeController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public async Task<IActionResult> Index()
        {
            var featured = await _blogService.GetPublicBlogsAsync(1, 6, null, null, "views");
            var latest = await _blogService.GetPublicBlogsAsync(1, 6, null, null, "newest");
            ViewBag.FeaturedBlogs = featured.Blogs;
            ViewBag.LatestBlogs = latest.Blogs;
            ViewBag.PopularDestinations = await _blogService.GetPopularDestinationsAsync(8);
            return View();
        }

        public IActionResult Privacy() => View();
        public IActionResult About() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
