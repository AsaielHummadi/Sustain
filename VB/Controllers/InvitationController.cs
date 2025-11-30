using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sustain.ViewModels.InvitationVMs;

namespace Sustain.Controllers
{
    [AllowAnonymous]
    public class InvitationController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<InvitationController> _logger;

        public InvitationController(SustainDbContext context, ILogger<InvitationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Invitation/Accept/{token}
        [HttpGet("Invitation/Accept/{token}")]
        public async Task<IActionResult> Accept(string token)
        {
            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Organization)
                    .Include(i => i.Role)
                    .Include(i => i.Factory)
                    .FirstOrDefaultAsync(i =>
                        i.InvitationToken == token &&
                        i.InvitationStatus == "Pending" &&
                        i.InvitationExpiration > DateTime.Now);

                if (invitation == null)
                {
                    TempData["error"] = "This invitation is invalid or has expired.";
                    return RedirectToAction("ShowLoginForm", "Auth");
                }

                var viewModel = new AcceptInvitationViewModel
                {
                    Token = token,
                    InvitedEmail = invitation.InvitedEmail,
                    OrganizationName = invitation.Organization.OrganizationName,
                    RoleName = invitation.Role.RoleName,
                    FactoryName = invitation.Factory?.FactoryName,
                    UserFname = "", // Initialize empty
                    UserLname = "", // Initialize empty
                    UserPhone = ""  // Initialize empty
                };

                return View("~/Views/Auth/Accept.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invitation with token {Token}", token);
                TempData["error"] = "An error occurred. Please try again.";
                return RedirectToAction("ShowLoginForm", "Auth");
            }
        }

        // POST: Invitation/CompleteRegistration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRegistration(CompleteRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If validation fails, return to the Accept view with the current model data
                var viewModel = new AcceptInvitationViewModel
                {
                    Token = model.Token,
                    InvitedEmail = "", // You might need to retrieve this from the invitation
                    OrganizationName = "", // You might need to retrieve this from the invitation
                    RoleName = "", // You might need to retrieve this from the invitation
                    UserFname = model.UserFname,
                    UserLname = model.UserLname,
                    UserPhone = model.UserPhone
                };
                return View("~/Views/Auth/Accept.cshtml", viewModel);
            }

            try
            {
                var invitation = await _context.Invitations
                    .Include(i => i.Organization)
                    .FirstOrDefaultAsync(i =>
                        i.InvitationToken == model.Token &&
                        i.InvitationStatus == "Pending" &&
                        i.InvitationExpiration > DateTime.Now);

                if (invitation == null)
                {
                    TempData["error"] = "This invitation is invalid or has expired.";
                    return RedirectToAction("Login", "Auth");
                }

                // Check if user with this email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserEmail == invitation.InvitedEmail);

                if (existingUser != null)
                {
                    TempData["error"] = "A user with this email already exists.";
                    return RedirectToAction("Login", "Auth");
                }

                // Create new user
                var user = new User
                {
                    OrganizationId = invitation.OrganizationId,
                    RoleId = invitation.RoleId,
                    UserFname = model.UserFname,
                    UserLname = model.UserLname,
                    UserEmail = invitation.InvitedEmail,
                    UserPhone = model.UserPhone,
                    UserPassword = BCrypt.Net.BCrypt.HashPassword(model.UserPassword),
                    UserStatus = "Active"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Update invitation
                invitation.InvitationStatus = "Accepted";
                invitation.InvitationAcceptedAt = DateTime.Now;
                invitation.UserId = user.UserId;
                await _context.SaveChangesAsync();

                // Log the user in
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

                // Store last activity time in session
                HttpContext.Session.SetString("LastActivityTime", DateTime.UtcNow.ToString("o"));

                TempData["success"] = "Account created successfully! Welcome to Sustain.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing registration for token {Token}", model.Token);
                TempData["error"] = "An error occurred during registration. Please try again.";
                return View("Accept", new AcceptInvitationViewModel { Token = model.Token });
            }
        }
    }
}