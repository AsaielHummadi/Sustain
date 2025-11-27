using Microsoft.AspNetCore.Mvc;
using Sustain.Data;
using Sustain.Utilities.Helpers;
using System.Security.Claims;

namespace Sustain.Utilities.ViewComponents
{
    /// <summary>
    /// View component to display subscription status and limits
    /// </summary>
    public class SubscriptionStatusViewComponent : ViewComponent
    {
        private readonly SustainDbContext _context;

        public SubscriptionStatusViewComponent(SustainDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.GetUserId();

            if (userId == null)
            {
                return View(new SubscriptionStatusViewModel());
            }

            var limits = await SubscriptionHelper.GetSubscriptionLimitsAsync(_context, null, userId);

            var viewModel = new SubscriptionStatusViewModel
            {
                CurrentUsers = limits.Users,
                MaxUsers = limits.MaxUsers,
                CurrentFactories = limits.Factories,
                MaxFactories = limits.MaxFactories,
                IsUserLimitReached = limits.IsUserLimitReached,
                IsFactoryLimitReached = limits.IsFactoryLimitReached
            };

            return View(viewModel);
        }
    }

    /// <summary>
    /// View model for subscription status
    /// </summary>
    public class SubscriptionStatusViewModel
    {
        public int CurrentUsers { get; set; }
        public int MaxUsers { get; set; }
        public int CurrentFactories { get; set; }
        public int MaxFactories { get; set; }
        public bool IsUserLimitReached { get; set; }
        public bool IsFactoryLimitReached { get; set; }

        public int UserPercentage => MaxUsers > 0 ? (CurrentUsers * 100) / MaxUsers : 0;
        public int FactoryPercentage => MaxFactories > 0 ? (CurrentFactories * 100) / MaxFactories : 0;
    }
}
