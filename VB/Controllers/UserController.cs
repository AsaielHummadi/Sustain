using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using Sustain.Utilities.Constants;
using Sustain.Utilities.Helpers;
using Sustain.ViewModels.EmailVMs;
using Sustain.ViewModels.UserVMs;

namespace Sustain.Controllers
{
    [Authorize(Policy = "Administrator")]
    public class UserController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(SustainDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var organizationId = User.GetOrganizationId();

            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.OrganizationId == organizationId && u.RoleId != Roles.ADMINISTRATOR)
                .ToListAsync();

            var factories = await _context.Factories
                .Where(f => f.OrganizationId == organizationId)
                .ToListAsync();

            // Fetch pending invitations
            var pendingInvitations = await _context.Invitations
                .Where(i => i.OrganizationId == organizationId && i.InvitationStatus == "Pending")
                .ToListAsync();

            var viewModel = new UserIndexViewModel
            {
                Users = users,
                Factories = factories,
                PendingInvitations = pendingInvitations
            };

            ViewBag.CanCreateUser = await SubscriptionHelper.CanCreateUserAsync(_context, organizationId, User.GetUserId());

            return View(viewModel);
        }

        public async Task<IActionResult> PendingInvitations()
        {
            var organizationId = User.GetOrganizationId();

            var pendingInvitations = await _context.Invitations
                .Include(i => i.Role)
                .Include(i => i.Factory)
                .Where(i => i.OrganizationId == organizationId && i.InvitationStatus == "Pending")
                .OrderByDescending(i => i.InvitationSentAt)
                .ToListAsync();

            var factories = await _context.Factories
                .Where(f => f.OrganizationId == organizationId)
                .ToListAsync();

            var viewModel = new PendingInvitationsViewModel
            {
                PendingInvitations = pendingInvitations,
                Factories = factories
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvitation(SendInvitationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                var existingUser = await _context.Users
                    .Where(u => u.UserEmail == model.InvitedEmail && u.OrganizationId == organizationId)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    TempData["error"] = "User with this email already exists in your organization.";
                    return RedirectToAction(nameof(Index));
                }

                var existingInvitation = await _context.Invitations
                    .Where(i => i.InvitedEmail == model.InvitedEmail &&
                               i.OrganizationId == organizationId &&
                               i.InvitationStatus == "Pending")
                    .FirstOrDefaultAsync();

                if (existingInvitation != null)
                {
                    TempData["error"] = "Pending invitation already exists for this email.";
                    return RedirectToAction(nameof(Index));
                }

                var invitation = new Invitation
                {
                    OrganizationId = organizationId.Value,
                    RoleId = model.RoleId,
                    FactoryId = model.FactoryId,
                    InvitedEmail = model.InvitedEmail,
                    InvitationStatus = "Pending",
                    InvitationSentAt = DateTime.Now,
                    InvitationExpiration = DateTime.Now.AddDays(7),
                    InvitationToken = Guid.NewGuid().ToString("N")
                };

                _context.Invitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Send invitation email
                await SendInvitationEmail(invitation);

                TempData["success"] = "Invitation sent successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invitation");
                TempData["error"] = "Error sending invitation: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, UpdateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                var user = await _context.Users
                    .Where(u => u.UserId == id &&
                               u.OrganizationId == organizationId &&
                               u.RoleId != Roles.ADMINISTRATOR)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if email is already taken
                var existingUser = await _context.Users
                    .Where(u => u.UserEmail == model.UserEmail && u.UserId != id)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    TempData["error"] = "Email is already taken by another user.";
                    return RedirectToAction(nameof(Index));
                }

                user.UserFname = model.UserFname;
                user.UserLname = model.UserLname;
                user.UserEmail = model.UserEmail;
                user.UserPhone = model.UserPhone;
                user.UserStatus = model.UserStatus;
                user.RoleId = model.RoleId;

                // For Factory Operators, we need to update their invitation with the factory
                if (model.RoleId == Roles.FACTORY_OPERATOR && model.FactoryId.HasValue)
                {
                    // Validate that the factory belongs to the organization
                    var factory = await _context.Factories
                        .Where(f => f.FactoryId == model.FactoryId.Value && f.OrganizationId == organizationId)
                        .FirstOrDefaultAsync();

                    if (factory == null)
                    {
                        TempData["error"] = "Selected factory does not belong to your organization.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Update or create invitation for this user with the factory
                    var invitation = await _context.Invitations
                        .Where(i => i.UserId == id && i.OrganizationId == organizationId)
                        .FirstOrDefaultAsync();

                    if (invitation != null)
                    {
                        invitation.FactoryId = model.FactoryId;
                    }
                    else
                    {
                        // Create a new invitation record for the factory assignment
                        var newInvitation = new Invitation
                        {
                            OrganizationId = organizationId.Value,
                            UserId = id,
                            RoleId = model.RoleId,
                            FactoryId = model.FactoryId,
                            InvitedEmail = user.UserEmail,
                            InvitationStatus = "Accepted", // Since user already exists
                            InvitationSentAt = DateTime.Now,
                            InvitationExpiration = DateTime.Now.AddYears(1), // Long expiration
                            InvitationToken = Guid.NewGuid().ToString("N")
                        };
                        _context.Invitations.Add(newInvitation);
                    }
                }
                else if (model.RoleId != Roles.FACTORY_OPERATOR)
                {
                    // If user is no longer a Factory Operator, remove factory assignment from invitation
                    var invitation = await _context.Invitations
                        .Where(i => i.UserId == id && i.OrganizationId == organizationId)
                        .FirstOrDefaultAsync();

                    if (invitation != null)
                    {
                        invitation.FactoryId = null;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["success"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                TempData["error"] = "Error updating user: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Destroy(int id)
        {
            try
            {
                var organizationId = User.GetOrganizationId();

                var user = await _context.Users
                    .Include(u => u.EmissionRecords)
                    .Include(u => u.Goals)
                    .Where(u => u.UserId == id &&
                               u.OrganizationId == organizationId &&
                               u.RoleId != Roles.ADMINISTRATOR)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (user.EmissionRecords.Any() || user.Goals.Any())
                {
                    TempData["error"] = "Cannot delete user with existing records. You can deactivate instead.";
                    return RedirectToAction(nameof(Index));
                }

                // Also delete any invitations associated with this user
                var invitations = await _context.Invitations
                    .Where(i => i.UserId == id && i.OrganizationId == organizationId)
                    .ToListAsync();

                _context.Invitations.RemoveRange(invitations);
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                TempData["success"] = "User deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                TempData["error"] = "Error deleting user: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelInvitation(int id)
        {
            try
            {
                var organizationId = User.GetOrganizationId();

                var invitation = await _context.Invitations
                    .Where(i => i.InvitationId == id && i.OrganizationId == organizationId)
                    .FirstOrDefaultAsync();

                if (invitation == null)
                {
                    TempData["error"] = "Invitation not found.";
                    return RedirectToAction(nameof(Index));
                }

                invitation.InvitationStatus = "Cancelled";
                await _context.SaveChangesAsync();

                TempData["success"] = "Invitation cancelled successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling invitation");
                TempData["error"] = "Error cancelling invitation: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendInvitation(int id)
        {
            try
            {
                var organizationId = User.GetOrganizationId();

                var invitation = await _context.Invitations
                    .Where(i => i.InvitationId == id &&
                               i.OrganizationId == organizationId &&
                               i.InvitationStatus == "Pending")
                    .FirstOrDefaultAsync();

                if (invitation == null)
                {
                    TempData["error"] = "Invitation not found.";
                    return RedirectToAction(nameof(PendingInvitations));
                }

                if (invitation.InvitationExpiration < DateTime.Now)
                {
                    invitation.InvitationExpiration = DateTime.Now.AddDays(7);
                    invitation.InvitationSentAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Send invitation email
                await SendInvitationEmail(invitation);

                TempData["success"] = "Invitation resent successfully!";
                return RedirectToAction(nameof(PendingInvitations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending invitation");
                TempData["error"] = "Error resending invitation: " + ex.Message;
                return RedirectToAction(nameof(PendingInvitations));
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("ShowLoginForm", "Auth");
            }

            var viewModel = new EditProfileViewModel
            {
                UserFname = user.UserFname,
                UserLname = user.UserLname,
                UserEmail = user.UserEmail,
                UserPhone = user.UserPhone
            };

            return View("EditProfile", viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EditProfile", model);
            }

            try
            {
                var userId = User.GetUserId();

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return RedirectToAction("ShowLoginForm", "Auth");
                }

                // Check if email is already taken
                var existingUser = await _context.Users
                    .Where(u => u.UserEmail == model.UserEmail && u.UserId != userId)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    ModelState.AddModelError("UserEmail", "This email is already in use by another user.");
                    return View("EditProfile", model);
                }

                user.UserFname = model.UserFname;
                user.UserLname = model.UserLname;
                user.UserEmail = model.UserEmail;
                user.UserPhone = model.UserPhone;

                if (!string.IsNullOrWhiteSpace(model.UserPassword))
                {
                    user.UserPassword = BCrypt.Net.BCrypt.HashPassword(model.UserPassword);
                }

                await _context.SaveChangesAsync();

                TempData["success"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(EditProfile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["error"] = "Error updating profile. Please try again.";
                return View("EditProfile", model);
            }
        }

        private async Task SendInvitationEmail(Invitation invitation)
        {
            try
            {
                var organization = await _context.Organizations.FindAsync(invitation.OrganizationId);
                var role = await _context.Roles.FindAsync(invitation.RoleId);
                var factory = invitation.FactoryId.HasValue ?
                    await _context.Factories.FindAsync(invitation.FactoryId.Value) : null;

                var invitationLink = Url.Action("Accept", "Invitation", new { token = invitation.InvitationToken }, Request.Scheme);

                // Create email model
                var emailModel = new InvitationEmailViewModel
                {
                    Organization = organization,
                    Invitation = new InvitationEmailViewModel.InvitationInfo
                    {
                        InvitedEmail = invitation.InvitedEmail,
                        Role = new InvitationEmailViewModel.RoleInfo
                        {
                            RoleName = role?.RoleName ?? "Unknown Role"
                        },
                        Factory = factory != null ? new InvitationEmailViewModel.FactoryInfo
                        {
                            FactoryName = factory.FactoryName
                        } : null
                    },
                    InvitationLink = invitationLink
                };

                // TODO: Implement your email service
                // This is a placeholder - you'll need to implement your email service
                // await _emailService.SendInvitationEmailAsync(invitation.InvitedEmail, emailModel);

                _logger.LogInformation("Invitation email would be sent to {Email} with link: {Link}",
                    invitation.InvitedEmail, invitationLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invitation email to {Email}", invitation.InvitedEmail);
                // Don't throw - we don't want email failures to break the invitation process
            }
        }

        // Helper method to get user's assigned factory (for views)
        private async Task<Factory> GetUserFactoryAsync(int userId, int organizationId)
        {
            var invitation = await _context.Invitations
                .Include(i => i.Factory)
                .Where(i => i.UserId == userId && i.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            return invitation?.Factory;
        }
    }
}