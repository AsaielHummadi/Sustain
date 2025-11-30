using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using Sustain.Utilities.Helpers;
using Sustain.ViewModels.EmissionSourceVMs;

namespace Sustain.Controllers
{
    [Authorize]
    public class EmissionSourceController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<EmissionSourceController> _logger;

        public EmissionSourceController(SustainDbContext context, ILogger<EmissionSourceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var organizationId = User.GetOrganizationId();

            // Get both global emission sources (OrganizationId == null) 
            // AND organization-specific ones (OrganizationId == user's org)
            var emissionSources = await _context.EmissionSources
                .Include(es => es.Organization)
                .Where(es => es.OrganizationId == null || es.OrganizationId == organizationId)
                .OrderByDescending(es => es.EmissionSourceIsActive)
                .ThenBy(es => es.EmissionSourceName)
                .ToListAsync();

            return View("~/Views/Scopes/Index.cshtml", emissionSources);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Store(CreateEmissionSourceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                var emissionSource = new EmissionSource
                {
                    OrganizationId = organizationId, // Organization-specific source
                    EmissionSourceName = model.EmissionSourceName,
                    EmissionSourceDescription = model.EmissionSourceDescription,
                    EmissionSourcePeriod = model.EmissionSourcePeriod,
                    EmissionSourceScope = model.EmissionSourceScope,
                    EmissionSourceUnit = model.EmissionSourceUnit,
                    EmissionSourceEmissionFactor = model.EmissionSourceEmissionFactor,
                    EmissionSourceFormula = model.EmissionSourceFormula,
                    EmissionSourceIsActive = true,
                    EmissionSourceIsRequested = false
                };

                _context.EmissionSources.Add(emissionSource);
                await _context.SaveChangesAsync();

                TempData["success"] = "Emission source added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding emission source");
                TempData["error"] = "Error adding emission source: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, UpdateEmissionSourceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                // Only allow updating organization-specific sources (not global ones)
                var emissionSource = await _context.EmissionSources
                    .Where(es => es.EmissionSourceId == id && es.OrganizationId == organizationId)
                    .FirstOrDefaultAsync();

                if (emissionSource == null)
                {
                    TempData["error"] = "Emission source not found or you don't have permission to edit it.";
                    return RedirectToAction(nameof(Index));
                }

                emissionSource.EmissionSourceName = model.EmissionSourceName;
                emissionSource.EmissionSourceDescription = model.EmissionSourceDescription;
                emissionSource.EmissionSourcePeriod = model.EmissionSourcePeriod;
                emissionSource.EmissionSourceScope = model.EmissionSourceScope;
                emissionSource.EmissionSourceUnit = model.EmissionSourceUnit;
                emissionSource.EmissionSourceEmissionFactor = model.EmissionSourceEmissionFactor;
                emissionSource.EmissionSourceFormula = model.EmissionSourceFormula;
                emissionSource.EmissionSourceIsActive = model.EmissionSourceIsActive;

                await _context.SaveChangesAsync();

                TempData["success"] = "Emission source updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating emission source");
                TempData["error"] = "Error updating emission source: " + ex.Message;
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

                // Only allow deleting organization-specific sources (not global ones)
                var emissionSource = await _context.EmissionSources
                    .Include(es => es.EmissionRecords)
                    .Where(es => es.EmissionSourceId == id && es.OrganizationId == organizationId)
                    .FirstOrDefaultAsync();

                if (emissionSource == null)
                {
                    TempData["error"] = "Emission source not found or you don't have permission to delete it.";
                    return RedirectToAction(nameof(Index));
                }

                if (emissionSource.EmissionRecords.Any())
                {
                    TempData["error"] = "Cannot delete emission source with existing emission records.";
                    return RedirectToAction(nameof(Index));
                }

                _context.EmissionSources.Remove(emissionSource);
                await _context.SaveChangesAsync();

                TempData["success"] = "Emission source deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting emission source");
                TempData["error"] = "Error deleting emission source: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCustomSource(RequestCustomSourceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                var emissionSource = new EmissionSource
                {
                    OrganizationId = organizationId, // Organization-specific request
                    EmissionSourceName = model.EmissionSourceName,
                    EmissionSourceDescription = model.EmissionSourceDescription,
                    EmissionSourcePeriod = model.EmissionSourcePeriod,
                    EmissionSourceScope = model.EmissionSourceScope,
                    EmissionSourceUnit = model.EmissionSourceUnit,
                    EmissionSourceEmissionFactor = 0, // To be set by admin
                    EmissionSourceFormula = null, // To be set by admin
                    EmissionSourceIsActive = false,
                    EmissionSourceIsRequested = true,
                    EmissionSourceRequestedAt = DateTime.Now,
                    EmissionSourceRequestStatus = "Pending"
                };

                _context.EmissionSources.Add(emissionSource);
                await _context.SaveChangesAsync();

                TempData["success"] = "Custom emission source requested successfully! It will be reviewed by administrators.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting custom emission source");
                TempData["error"] = "Error requesting custom emission source: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}