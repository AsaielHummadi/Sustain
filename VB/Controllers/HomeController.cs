using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;

namespace Sustain.Controllers
{
    public class HomeController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(SustainDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Home()
        {
            var plans = await _context.SubscriptionPlans
                .Where(p => p.SubscriptionPlanType == "paid")
                .OrderBy(p => p.SubscriptionPlanPrice)
                .ToListAsync();

            return View(plans);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Home));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}