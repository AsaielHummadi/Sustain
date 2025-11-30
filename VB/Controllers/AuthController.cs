using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using Sustain.Utilities.Constants;
using Sustain.ViewModels.AuthVMs;
using System.Security.Claims;

namespace Sustain.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(SustainDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Auth/ShowLoginForm
        [HttpGet]
        public IActionResult ShowLoginForm()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View("Login");
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Organization)
                    .FirstOrDefaultAsync(u => u.UserEmail == model.UserEmail);

                if (user == null)
                {
                    ModelState.AddModelError("UserEmail", "The provided credentials do not match our records.");
                    return View("Login", model);
                }

                if (!BCrypt.Net.BCrypt.Verify(model.UserPassword, user.UserPassword))
                {
                    ModelState.AddModelError("UserEmail", "The provided credentials do not match our records.");
                    return View("Login", model);
                }

                if (user.UserStatus != "Active")
                {
                    ModelState.AddModelError("UserEmail", "Your account is not active.");
                    return View("Login", model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.UserEmail),
                    new Claim(ClaimTypes.Name, $"{user.UserFname} {user.UserLname}"),
                    new Claim(ClaimTypes.Role, user.Role.RoleName),
                    new Claim("OrganizationId", user.OrganizationId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                HttpContext.Session.SetString("LastActivityTime", DateTime.UtcNow.ToString("o"));

                return Redirect(RedirectTo(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError(string.Empty, "An error occurred during login.");
                return View("Login", model);
            }
        }

        // GET: Auth/ShowRegistrationForm
        [HttpGet]
        public async Task<IActionResult> ShowRegistrationForm()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var plans = await _context.SubscriptionPlans
                .Where(p => p.SubscriptionPlanType == "free")
                .FirstOrDefaultAsync();

            var viewModel = new RegisterViewModel
            {
                SubscriptionPlanId = plans?.SubscriptionPlanId ?? 0
            };

            return View("Register", viewModel);
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Register", model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserEmail == model.UserEmail);

                if (existingUser != null)
                {
                    ModelState.AddModelError("UserEmail", "A user with this email already exists.");
                    return View("Register", model);
                }

                var organization = new Organization
                {
                    OrganizationName = model.OrganizationName,
                    OrganizationIndustry = model.OrganizationIndustry,
                    OrganizationCity = model.OrganizationCity
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

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

                var plan = await _context.SubscriptionPlans.FindAsync(model.SubscriptionPlanId);
                if (plan == null)
                {
                    throw new Exception("Subscription plan not found");
                }

                var startDate = DateTime.Now;
                var endDate = plan.SubscriptionPlanType == "free"
                    ? startDate.AddMonths(1)
                    : startDate.AddYears(1);

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

                await transaction.CommitAsync();

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

                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                return View("Register", model);
            }
        }

        // POST: Auth/Logout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Home", "Home");
        }

        private string RedirectTo(User user)
        {
            if (user.RoleId == Roles.ADMINISTRATOR)
            {
                return "/Dashboard/AdminDashboard";
            }
            else if (user.RoleId == Roles.SUSTAINABILITY_OFFICER)
            {
                return "/Dashboard/SustainabilityOfficerDashboard";
            }
            else if (user.RoleId == Roles.FACTORY_OPERATOR)
            {
                return "/Dashboard/FactoryOperatorDashboard";
            }
            return "/Dashboard";
        }
    }
}