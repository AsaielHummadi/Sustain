using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Utilities.Constants;
using Sustain.Utilities.Helpers;
using Sustain.ViewModels.DashboardVMs;

namespace Sustain.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(SustainDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (userId == null || organizationId == null)
            {
                return RedirectToAction("ShowLoginForm", "Auth");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return RedirectToAction("ShowLoginForm", "Auth");
            }

            ViewBag.User = user;
            ViewBag.Organization = user.Organization;
            ViewBag.Role = user.Role.RoleName;

            return View("Admin");
        }

        public async Task<IActionResult> AdminDashboard()
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (!User.HasRole(Roles.GetName(Roles.ADMINISTRATOR)))
            {
                return StatusCode(403);
            }

            var organization = await _context.Organizations
                .Include(o => o.Users).ThenInclude(u => u.Role)
                .Include(o => o.Factories).ThenInclude(f => f.EmissionRecords)
                .FirstOrDefaultAsync(o => o.OrganizationId == organizationId);

            var totalUsers = organization.Users.Count;
            var activeUsers = organization.Users.Count(u => u.UserStatus == "Active");
            var sustainabilityOfficers = organization.Users.Count(u => u.RoleId == Roles.SUSTAINABILITY_OFFICER);
            var factoryOperators = organization.Users.Count(u => u.RoleId == Roles.FACTORY_OPERATOR);
            var totalFactories = organization.Factories.Count;

            var factoriesWithRecords = organization.Factories.Count(f => f.EmissionRecords.Any());

            var currentSubscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.OrganizationId == organizationId && s.SubscriptionStatus == "Active")
                .OrderByDescending(s => s.SubscriptionStartDate)
                .FirstOrDefaultAsync();

            var dataCompliance = totalFactories > 0 ? (factoriesWithRecords * 100.0 / totalFactories) : 0;

            var factoryPerformanceData = new List<double>();
            var factoryPerformanceLabels = new List<string>();

            foreach (var factory in organization.Factories)
            {
                factoryPerformanceLabels.Add(factory.FactoryName);
                var totalEmissions = await _context.EmissionRecords
                    .Include(er => er.EmissionSource)
                    .Where(er => er.FactoryId == factory.FactoryId)
                    .SumAsync(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));
                factoryPerformanceData.Add(Math.Round(totalEmissions, 2));
            }

            var userDistributionData = new List<int> { 1, sustainabilityOfficers, factoryOperators };
            var userDistributionLabels = new List<string> { "Administrators", "Sustainability Officers", "Factory Operators" };

            var recentActivity = new List<RecentActivityItem>();

            var recentUsers = organization.Users.OrderByDescending(u => u.UserId).Take(2);
            foreach (var recentUser in recentUsers)
            {
                recentActivity.Add(new RecentActivityItem
                {
                    Icon = "bi-person-plus",
                    Color = "text-success",
                    Message = $"New user \"{recentUser.UserFname}\" added as {Roles.GetName(recentUser.RoleId)}",
                    Time = "Just now"
                });
            }

            var recentRecords = await _context.EmissionRecords
                .Include(er => er.Factory)
                .Where(er => er.OrganizationId == organizationId)
                .OrderByDescending(er => er.EmissionCreatedAt)
                .Take(1)
                .ToListAsync();

            foreach (var record in recentRecords)
            {
                recentActivity.Add(new RecentActivityItem
                {
                    Icon = "bi-building",
                    Color = "text-primary",
                    Message = $"Factory \"{record.Factory.FactoryName}\" completed monthly report",
                    Time = record.EmissionCreatedAt?.ToString("MMM dd, yyyy") ?? "Recently"
                });
            }

            if (currentSubscription != null)
            {
                var recentInvoices = await _context.Invoices
                    .Where(i => i.SubscriptionId == currentSubscription.SubscriptionId && i.InvoiceStatus == "Paid")
                    .OrderByDescending(i => i.InvoiceDate)
                    .Take(1)
                    .ToListAsync();

                foreach (var invoice in recentInvoices)
                {
                    recentActivity.Add(new RecentActivityItem
                    {
                        Icon = "bi-credit-card",
                        Color = "text-warning",
                        Message = "Subscription payment received",
                        Time = invoice.InvoiceDate.ToString("MMM dd, yyyy")
                    });
                }
            }

            var viewModel = new AdminDashboardViewModel
            {
                User = await _context.Users.Include(u => u.Organization).FirstAsync(u => u.UserId == userId),
                Organization = organization,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalFactories = totalFactories,
                FactoriesWithRecords = factoriesWithRecords,
                CurrentSubscription = currentSubscription,
                DataCompliance = dataCompliance,
                FactoryPerformanceData = factoryPerformanceData,
                FactoryPerformanceLabels = factoryPerformanceLabels,
                UserDistributionData = userDistributionData,
                UserDistributionLabels = userDistributionLabels,
                RecentActivity = recentActivity.Take(3).ToList()
            };

            return View("Admin", viewModel);
        }

        public async Task<IActionResult> SustainabilityOfficerDashboard(int? selected_factory_id)
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (!User.HasRole(Roles.GetName(Roles.SUSTAINABILITY_OFFICER)))
            {
                return StatusCode(403);
            }

            var query = _context.EmissionRecords
                .Include(er => er.EmissionSource)
                .Include(er => er.Factory)
                .Include(er => er.User)
                .Where(er => er.OrganizationId == organizationId);

            if (selected_factory_id.HasValue)
            {
                query = query.Where(er => er.FactoryId == selected_factory_id.Value);
            }

            var emissionRecords = await query
                .OrderByDescending(er => er.EmissionYear)
                .ThenByDescending(er => er.EmissionMonth)
                .ToListAsync();

            var totalEmissions = emissionRecords.Sum(er =>
                (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));

            var scope1Emissions = emissionRecords
                .Where(er => er.EmissionSource.EmissionSourceScope == "Scope 1")
                .Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));

            var scope2Emissions = emissionRecords
                .Where(er => er.EmissionSource.EmissionSourceScope == "Scope 2")
                .Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));

            var emissionsBySource = emissionRecords
                .GroupBy(er => er.EmissionSource.EmissionSourceName)
                .ToDictionary(g => g.Key, g => g.Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity)));

            var emissionsByMonth = emissionRecords
                .GroupBy(er => $"{er.EmissionYear}-{er.EmissionMonth:D2}")
                .ToDictionary(g => g.Key, g => g.Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity)));

            var factoryEmissions = emissionRecords
                .GroupBy(er => er.FactoryId)
                .ToDictionary(g => g.Key, g => g.Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity)));

            var factoryNames = await _context.Factories
                .Where(f => f.OrganizationId == organizationId)
                .ToDictionaryAsync(f => f.FactoryId, f => f.FactoryName);

            var viewModel = new SustainabilityOfficerDashboardViewModel
            {
                User = await _context.Users.Include(u => u.Organization).FirstAsync(u => u.UserId == userId),
                Organization = await _context.Organizations.FirstAsync(o => o.OrganizationId == organizationId),
                EmissionRecords = emissionRecords,
                TotalEmissions = totalEmissions,
                Scope1Emissions = scope1Emissions,
                Scope2Emissions = scope2Emissions,
                ChartData = new Dictionary<string, object>
                {
                    ["emissionsBySource"] = emissionsBySource,
                    ["emissionsByMonth"] = emissionsByMonth,
                    ["factoryEmissions"] = factoryEmissions,
                    ["factoryNames"] = factoryNames
                },
                SelectedFactoryId = selected_factory_id
            };

            return View("SustainabilityOfficer", viewModel);
        }

        public async Task<IActionResult> FactoryOperatorDashboard()
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (!User.HasRole(Roles.GetName(Roles.FACTORY_OPERATOR)))
            {
                return StatusCode(403);
            }

            var invitation = await _context.Invitations
                .Include(i => i.Factory)
                .Where(i => i.UserId == userId && i.FactoryId != null)
                .FirstOrDefaultAsync();

            if (invitation?.Factory == null)
            {
                TempData["error"] = "No factory assigned to your account.";
                return RedirectToAction("Index");
            }

            var factory = invitation.Factory;

            var emissionRecords = await _context.EmissionRecords
                .Include(er => er.EmissionSource)
                .Include(er => er.Factory)
                .Include(er => er.User)
                .Where(er => er.FactoryId == factory.FactoryId)
                .OrderByDescending(er => er.EmissionYear)
                .ThenByDescending(er => er.EmissionMonth)
                .ToListAsync();

            var totalEmissions = emissionRecords.Sum(er =>
                (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));

            var scope1Emissions = emissionRecords
                .Where(er => er.EmissionSource.EmissionSourceScope == "Scope 1")
                .Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));

            var scope2Emissions = emissionRecords
                .Where(er => er.EmissionSource.EmissionSourceScope == "Scope 2")
                .Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));

            var emissionsBySource = emissionRecords
                .GroupBy(er => er.EmissionSource.EmissionSourceName)
                .ToDictionary(g => g.Key, g => g.Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity)));

            var emissionsByMonth = emissionRecords
                .GroupBy(er => $"{er.EmissionYear}-{er.EmissionMonth:D2}")
                .ToDictionary(g => g.Key, g => g.Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity)));

            var factoryEmissions = emissionRecords
                .GroupBy(er => er.FactoryId)
                .ToDictionary(g => g.Key, g => g.Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity)));

            var factoryNames = new Dictionary<int, string> { { factory.FactoryId, factory.FactoryName } };

            var latestRecords = emissionRecords.Take(3).ToList();

            var viewModel = new FactoryOperatorDashboardViewModel
            {
                User = await _context.Users.Include(u => u.Organization).FirstAsync(u => u.UserId == userId),
                Organization = await _context.Organizations.FirstAsync(o => o.OrganizationId == organizationId),
                Factory = factory,
                EmissionRecords = emissionRecords,
                TotalEmissions = totalEmissions,
                Scope1Emissions = scope1Emissions,
                Scope2Emissions = scope2Emissions,
                ChartData = new Dictionary<string, object>
                {
                    ["emissionsBySource"] = emissionsBySource,
                    ["emissionsByMonth"] = emissionsByMonth,
                    ["factoryEmissions"] = factoryEmissions,
                    ["factoryNames"] = factoryNames
                },
                LatestRecords = latestRecords
            };

            return View("FactoryOperator", viewModel);
        }
    }
}