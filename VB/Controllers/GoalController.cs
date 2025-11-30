using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using Sustain.Utilities.Constants;
using Sustain.Utilities.Helpers;
using Sustain.ViewModels.GoalVMs;

namespace Sustain.Controllers
{
    [Authorize]
    public class GoalController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<GoalController> _logger;

        public GoalController(SustainDbContext context, ILogger<GoalController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            var organization = await _context.Organizations.FindAsync(organizationId);

            List<Goal> goals;
            if (User.HasRole(Roles.GetName(Roles.ADMINISTRATOR)) || User.HasRole(Roles.GetName(Roles.SUSTAINABILITY_OFFICER)))
            {
                goals = await _context.Goals
                    .Include(g => g.EmissionSource)
                    .Include(g => g.User)
                    .Where(g => g.OrganizationId == organizationId)
                    .OrderByDescending(g => g.GoalGoalStartDate)
                    .ToListAsync();
            }
            else
            {
                goals = await _context.Goals
                    .Include(g => g.EmissionSource)
                    .Include(g => g.User)
                    .Where(g => g.OrganizationId == organizationId && g.UserId == userId)
                    .OrderByDescending(g => g.GoalGoalStartDate)
                    .ToListAsync();
            }

            var emissionSources = await _context.EmissionSources
                .Where(es => es.EmissionSourceIsActive == true)
                .ToListAsync();

            var viewModel = new GoalIndexViewModel
            {
                Goals = goals,
                EmissionSources = emissionSources,
                Organization = organization
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Show(int id)
        {
            var organizationId = User.GetOrganizationId();

            var goal = await _context.Goals
                .Include(g => g.EmissionSource)
                .Include(g => g.User)
                .Include(g => g.Organization)
                .Where(g => g.OrganizationId == organizationId)
                .FirstOrDefaultAsync(g => g.GoalId == id);

            if (goal == null)
            {
                TempData["error"] = "Goal not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(goal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Store(CreateGoalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the errors below.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = User.GetUserId();
                var organizationId = User.GetOrganizationId();

                var goal = new Goal
                {
                    OrganizationId = organizationId.Value,
                    UserId = userId.Value,
                    EmissionSourceId = model.EmissionSourceId,
                    GoalTitle = model.GoalTitle,
                    GoalDescription = model.GoalDescription,
                    GoalStatus = "Active",
                    GoalGoalValue = model.GoalGoalValue,
                    GoalGoalPeriod = model.GoalGoalPeriod,
                    GoalGoalStartDate = model.GoalGoalStartDate,
                    GoalGoalEndDate = model.GoalGoalEndDate
                };

                _context.Goals.Add(goal);
                await _context.SaveChangesAsync();

                TempData["success"] = "Goal created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal");
                TempData["error"] = "Failed to create goal. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, UpdateGoalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the errors below.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                var goal = await _context.Goals
                    .Where(g => g.OrganizationId == organizationId)
                    .FirstOrDefaultAsync(g => g.GoalId == id);

                if (goal == null)
                {
                    TempData["error"] = "Goal not found.";
                    return RedirectToAction(nameof(Index));
                }

                goal.EmissionSourceId = model.EmissionSourceId;
                goal.GoalTitle = model.GoalTitle;
                goal.GoalDescription = model.GoalDescription;
                goal.GoalStatus = model.GoalStatus;
                goal.GoalGoalValue = model.GoalGoalValue;
                goal.GoalGoalPeriod = model.GoalGoalPeriod;
                goal.GoalGoalStartDate = model.GoalGoalStartDate;
                goal.GoalGoalEndDate = model.GoalGoalEndDate;

                await _context.SaveChangesAsync();

                TempData["success"] = "Goal updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal");
                TempData["error"] = "Failed to update goal. Please try again.";
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

                var goal = await _context.Goals
                    .Where(g => g.OrganizationId == organizationId)
                    .FirstOrDefaultAsync(g => g.GoalId == id);

                if (goal == null)
                {
                    TempData["error"] = "Goal not found.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Goals.Remove(goal);
                await _context.SaveChangesAsync();

                TempData["success"] = "Goal deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal");
                TempData["error"] = "Failed to delete goal. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, UpdateGoalStatusViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid status value.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                var goal = await _context.Goals
                    .Where(g => g.OrganizationId == organizationId)
                    .FirstOrDefaultAsync(g => g.GoalId == id);

                if (goal == null)
                {
                    TempData["error"] = "Goal not found.";
                    return RedirectToAction(nameof(Index));
                }

                goal.GoalStatus = model.GoalStatus;
                await _context.SaveChangesAsync();

                TempData["success"] = "Goal status updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal status");
                TempData["error"] = "Failed to update goal status. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}