using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using Sustain.Utilities.Constants;
using Sustain.Utilities.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sustain.ViewModels.SubscriptionVMs;

namespace Sustain.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(SustainDbContext context, ILogger<SubscriptionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ShowSubscriptionForm(int planId)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null)
            {
                TempData["error"] = "Subscription plan not found.";
                return RedirectToAction("Home", "Home");
            }

            return View("Checkout", plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSubscription(ProcessSubscriptionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("ShowSubscriptionForm", new { planId = model.SubscriptionPlanId });
            }

            var plan = await _context.SubscriptionPlans.FindAsync(model.SubscriptionPlanId);
            if (plan == null)
            {
                TempData["error"] = "Subscription plan not found.";
                return RedirectToAction("Home", "Home");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create Organization
                var organization = new Organization
                {
                    OrganizationName = model.OrganizationName,
                    OrganizationIndustry = model.OrganizationIndustry,
                    OrganizationCity = model.OrganizationCity
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                // Create User
                var user = new User
                {
                    OrganizationId = organization.OrganizationId,
                    RoleId = Roles.ADMINISTRATOR,
                    UserFname = model.UserFname,
                    UserLname = model.UserLname,
                    UserEmail = model.UserEmail,
                    UserPhone = model.UserPhone,
                    UserPassword = BCrypt.Net.BCrypt.HashPassword(model.UserPassword),
                    UserStatus = "Active"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Subscription
                var startDate = DateTime.Now;
                var endDate = startDate.AddYears(1);

                var subscription = new Subscription
                {
                    OrganizationId = organization.OrganizationId,
                    SubscriptionPlanId = plan.SubscriptionPlanId,
                    SubscriptionStartDate = DateOnly.FromDateTime(startDate),
                    SubscriptionEndDate = DateOnly.FromDateTime(endDate),
                    SubscriptionStatus = "Active"
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                // Create Invoice and Payment if paid plan
                if (plan.SubscriptionPlanType == "paid" && plan.SubscriptionPlanPrice > 0)
                {
                    var invoice = new Invoice
                    {
                        SubscriptionId = subscription.SubscriptionId,
                        InvoiceTotalAmount = plan.SubscriptionPlanPrice ?? 0,
                        InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
                        InvoiceTime = TimeOnly.FromDateTime(DateTime.Now),
                        InvoiceStatus = "Paid"
                    };

                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    var payment = new Payment
                    {
                        InvoiceId = invoice.InvoiceId,
                        PaymentAmount = plan.SubscriptionPlanPrice ?? 0,
                        PaymentDate = DateOnly.FromDateTime(DateTime.Now),
                        PaymentTime = TimeOnly.FromDateTime(DateTime.Now),
                        PaymentStatus = "Completed",
                        PaymentMethod = "Credit Card"
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Store plan in session
                HttpContext.Session.SetInt32("SubscribedPlanId", plan.SubscriptionPlanId);

                // Login user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.UserEmail),
                    new Claim(ClaimTypes.Name, $"{user.UserFname} {user.UserLname}"),
                    new Claim(ClaimTypes.Role, Roles.GetName(Roles.ADMINISTRATOR)),
                    new Claim("OrganizationId", user.OrganizationId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                HttpContext.Session.SetString("LastActivityTime", DateTime.UtcNow.ToString("o"));

                return RedirectToAction(nameof(Success));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing subscription");
                TempData["error"] = "Subscription failed: " + ex.Message;
                return RedirectToAction("ShowSubscriptionForm", new { planId = model.SubscriptionPlanId });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Success()
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            var subscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.OrganizationId == organizationId)
                .OrderByDescending(s => s.SubscriptionStartDate)
                .FirstOrDefaultAsync();

            var plan = subscription?.SubscriptionPlan;

            return View(plan);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> FreeTrialSuccess()
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            var subscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.OrganizationId == organizationId)
                .OrderByDescending(s => s.SubscriptionStartDate)
                .FirstOrDefaultAsync();

            var plan = subscription?.SubscriptionPlan;

            return View(plan);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ShowBilling()
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (organizationId == null)
            {
                TempData["error"] = "Organization not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                TempData["error"] = "Organization not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            var subscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.OrganizationId == organizationId && s.SubscriptionStatus == "Active")
                .OrderByDescending(s => s.SubscriptionStartDate)
                .FirstOrDefaultAsync();

            var invoices = await _context.Invoices
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.SubscriptionPlan)
                .Include(i => i.Payments)
                .Where(i => i.Subscription.OrganizationId == organizationId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            // Get paid plans for the renewal modal
            var paidPlans = await _context.SubscriptionPlans
                .Where(p => p.SubscriptionPlanType == "paid")
                .ToListAsync();

            // Get usage counts
            var userCount = await _context.Users.CountAsync(u => u.OrganizationId == organizationId);
            var factoryCount = await _context.Factories.CountAsync(f => f.OrganizationId == organizationId);
            var emissionRecordsCount = await _context.EmissionRecords.CountAsync(er => er.OrganizationId == organizationId);

            ViewBag.PaidPlans = paidPlans;
            ViewBag.UserCount = userCount;
            ViewBag.FactoryCount = factoryCount;
            ViewBag.EmissionRecordsCount = emissionRecordsCount;

            var viewModel = new BillingViewModel
            {
                Subscription = subscription,
                Invoices = invoices ?? new List<Invoice>(),
                Organization = organization
            };

            return View("~/Views/Billing/Index.cshtml", viewModel);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRenewal(ProcessRenewalViewModel model)
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please provide valid payment information.";
                return RedirectToAction(nameof(ShowBilling));
            }

            var plan = await _context.SubscriptionPlans.FindAsync(model.SubscriptionPlanId);
            if (plan == null)
            {
                TempData["error"] = "Subscription plan not found.";
                return RedirectToAction(nameof(ShowBilling));
            }

            var currentSubscription = await _context.Subscriptions.FindAsync(model.RenewSubscriptionId);
            if (currentSubscription == null)
            {
                TempData["error"] = "Current subscription not found.";
                return RedirectToAction(nameof(ShowBilling));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Expire current subscription
                currentSubscription.SubscriptionStatus = "Expired";
                await _context.SaveChangesAsync();

                // Create new subscription
                var startDate = DateTime.Now;
                var endDate = plan.SubscriptionPlanType == "free"
                    ? startDate.AddMonths(1)
                    : startDate.AddYears(1);

                var newSubscription = new Subscription
                {
                    OrganizationId = organizationId.Value,
                    SubscriptionPlanId = plan.SubscriptionPlanId,
                    SubscriptionStartDate = DateOnly.FromDateTime(startDate),
                    SubscriptionEndDate = DateOnly.FromDateTime(endDate),
                    SubscriptionStatus = "Active"
                };

                _context.Subscriptions.Add(newSubscription);
                await _context.SaveChangesAsync();

                // Create Invoice
                var invoice = new Invoice
                {
                    SubscriptionId = newSubscription.SubscriptionId,
                    InvoiceTotalAmount = plan.SubscriptionPlanPrice ?? 0,
                    InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
                    InvoiceTime = TimeOnly.FromDateTime(DateTime.Now),
                    InvoiceStatus = "Paid"
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Create Payment
                var payment = new Payment
                {
                    InvoiceId = invoice.InvoiceId,
                    PaymentAmount = plan.SubscriptionPlanPrice ?? 0,
                    PaymentDate = DateOnly.FromDateTime(DateTime.Now),
                    PaymentTime = TimeOnly.FromDateTime(DateTime.Now),
                    PaymentStatus = "Completed",
                    PaymentMethod = "Credit Card"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["success"] = "Subscription renewed successfully!";
                return RedirectToAction(nameof(ShowBilling));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error renewing subscription");
                TempData["error"] = "Renewal failed: " + ex.Message;
                return RedirectToAction(nameof(ShowBilling));
            }
        }

        private bool ProcessPayment(ProcessRenewalViewModel model)
        {
            var cardNumber = model.CardNumber.Replace(" ", "");
            if (cardNumber.Length < 16 || !cardNumber.All(char.IsDigit))
            {
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(model.ExpiryDate, @"^(0[1-9]|1[0-2])\/\d{2}$"))
            {
                return false;
            }

            if (model.Cvc.Length < 3 || !model.Cvc.All(char.IsDigit))
            {
                return false;
            }

            return true;
        }
    }
}