using Microsoft.EntityFrameworkCore;
using Sustain.Data;

namespace Sustain.Utilities.Helpers
{
    public static class SubscriptionHelper
    {
        public static async Task<bool> CanCreateUserAsync(SustainDbContext context, int? organizationId = null, int? currentUserId = null)
        {
            if (currentUserId == null)
                return false;

            var user = await context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.UserId == currentUserId);

            if (user == null)
                return false;

            int orgId = organizationId ?? user.OrganizationId;

            var currentSubscription = await context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.OrganizationId == orgId && s.SubscriptionStatus == "Active")
                .FirstOrDefaultAsync();

            if (currentSubscription == null)
                return false;

            // Handle null values by providing defaults
            int maxUsers = currentSubscription.SubscriptionPlan.SubscriptionPlanUserMax ?? 0;
            int currentUsers = await context.Users.CountAsync(u => u.OrganizationId == orgId);

            return currentUsers < maxUsers;
        }

        public static async Task<bool> CanCreateFactoryAsync(SustainDbContext context, int? organizationId = null, int? currentUserId = null)
        {
            if (currentUserId == null)
                return false;

            var user = await context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.UserId == currentUserId);

            if (user == null)
                return false;

            int orgId = organizationId ?? user.OrganizationId;

            var currentSubscription = await context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.OrganizationId == orgId && s.SubscriptionStatus == "Active")
                .FirstOrDefaultAsync();

            if (currentSubscription == null)
                return false;

            // Handle null values by providing defaults
            int maxFactories = currentSubscription.SubscriptionPlan.SubscriptionPlanFactoryMax ?? 0;
            int currentFactories = await context.Factories.CountAsync(f => f.OrganizationId == orgId);

            return currentFactories < maxFactories;
        }

        public static async Task<SubscriptionLimits> GetSubscriptionLimitsAsync(SustainDbContext context, int? organizationId = null, int? currentUserId = null)
        {
            var defaultLimits = new SubscriptionLimits
            {
                Users = 0,
                Factories = 0,
                MaxUsers = 0,
                MaxFactories = 0
            };

            if (currentUserId == null)
                return defaultLimits;

            var user = await context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.UserId == currentUserId);

            if (user == null)
                return defaultLimits;

            int orgId = organizationId ?? user.OrganizationId;

            var currentSubscription = await context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.OrganizationId == orgId && s.SubscriptionStatus == "Active")
                .FirstOrDefaultAsync();

            if (currentSubscription == null)
                return defaultLimits;

            // Handle null values by providing defaults
            int maxUsers = currentSubscription.SubscriptionPlan.SubscriptionPlanUserMax ?? 0;
            int maxFactories = currentSubscription.SubscriptionPlan.SubscriptionPlanFactoryMax ?? 0;
            int currentUsers = await context.Users.CountAsync(u => u.OrganizationId == orgId);
            int currentFactories = await context.Factories.CountAsync(f => f.OrganizationId == orgId);

            return new SubscriptionLimits
            {
                Users = currentUsers,
                Factories = currentFactories,
                MaxUsers = maxUsers,
                MaxFactories = maxFactories
            };
        }
    }

    public class SubscriptionLimits
    {
        public int Users { get; set; }
        public int Factories { get; set; }
        public int MaxUsers { get; set; }
        public int MaxFactories { get; set; }

        public bool IsUserLimitReached => Users >= MaxUsers;
        public bool IsFactoryLimitReached => Factories >= MaxFactories;
    }
}