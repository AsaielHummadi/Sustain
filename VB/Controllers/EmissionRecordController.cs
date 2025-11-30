using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using Sustain.Utilities.Constants;
using Sustain.Utilities.Helpers;
using Sustain.ViewModels.EmissionRecordVMs;

namespace Sustain.Controllers
{
    [Authorize]
    public class EmissionRecordController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<EmissionRecordController> _logger;

        public EmissionRecordController(SustainDbContext context, ILogger<EmissionRecordController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? factory_filter, int? source_filter, int? year_filter, int? month_filter, string? scope_filter)
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            var query = _context.EmissionRecords
                .Include(er => er.EmissionSource)
                .Include(er => er.Factory)
                .Include(er => er.User)
                .AsQueryable();

            if (User.HasRole(Roles.GetName(Roles.ADMINISTRATOR)) || User.HasRole(Roles.GetName(Roles.SUSTAINABILITY_OFFICER)))
            {
                query = query.Where(er => er.OrganizationId == organizationId);
            }
            else
            {
                var invitation = await _context.Invitations
                    .Where(i => i.UserId == userId && i.FactoryId != null)
                    .FirstOrDefaultAsync();
                var facId = invitation?.FactoryId;
                query = query.Where(er => er.OrganizationId == organizationId && er.FactoryId == facId);
            }

            if (factory_filter.HasValue)
            {
                query = query.Where(er => er.FactoryId == factory_filter.Value);
            }
            if (source_filter.HasValue)
            {
                query = query.Where(er => er.EmissionSourceId == source_filter.Value);
            }
            if (year_filter.HasValue)
            {
                query = query.Where(er => er.EmissionYear == year_filter.Value);
            }
            if (month_filter.HasValue)
            {
                query = query.Where(er => er.EmissionMonth == month_filter.Value);
            }
            if (!string.IsNullOrEmpty(scope_filter))
            {
                query = query.Where(er => er.EmissionSource.EmissionSourceScope == scope_filter);
            }

            var emissionRecords = await query
                .OrderByDescending(er => er.EmissionYear)
                .ThenByDescending(er => er.EmissionMonth)
                .ToListAsync();

            var emissionSources = await _context.EmissionSources
                .Where(es => es.EmissionSourceIsActive == true)
                .ToListAsync();

            List<Factory> factories;
            if (User.HasRole(Roles.GetName(Roles.FACTORY_OPERATOR)))
            {
                var invitation = await _context.Invitations
                    .Where(i => i.UserId == userId && i.FactoryId != null)
                    .FirstOrDefaultAsync();
                var facId = invitation?.FactoryId;
                factories = await _context.Factories.Where(f => f.FactoryId == facId).ToListAsync();
            }
            else
            {
                factories = await _context.Factories.Where(f => f.OrganizationId == organizationId).ToListAsync();
            }

            var totalEmissions = emissionRecords.Sum(er =>
                (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));
            var scope1Emissions = emissionRecords
                .Where(er => er.EmissionSource.EmissionSourceScope == "Scope 1")
                .Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));
            var scope2Emissions = emissionRecords
                .Where(er => er.EmissionSource.EmissionSourceScope == "Scope 2")
                .Sum(er => (double)(er.EmissionSource.EmissionSourceEmissionFactor * er.EmissionQuantity));

            var viewModel = new EmissionRecordIndexViewModel
            {
                EmissionRecords = emissionRecords,
                EmissionSources = emissionSources,
                Factories = factories,
                TotalEmissions = totalEmissions,
                Scope1Emissions = scope1Emissions,
                Scope2Emissions = scope2Emissions
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Store(CreateEmissionRecordViewModel model)
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the errors below.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var existingRecord = await _context.EmissionRecords
                    .Where(er => er.OrganizationId == organizationId &&
                                 er.FactoryId == model.FactoryId &&
                                 er.EmissionSourceId == model.EmissionSourceId &&
                                 er.EmissionYear == model.EmissionYear &&
                                 er.EmissionMonth == model.EmissionMonth)
                    .FirstOrDefaultAsync();

                if (existingRecord != null)
                {
                    TempData["error"] = "An emission record already exists for this source, factory, and period.";
                    return RedirectToAction(nameof(Index));
                }

                var emissionRecord = new EmissionRecord
                {
                    EmissionSourceId = model.EmissionSourceId,
                    FactoryId = model.FactoryId,
                    UserId = userId.Value,
                    OrganizationId = organizationId.Value,
                    EmissionYear = model.EmissionYear,
                    EmissionMonth = model.EmissionMonth,
                    EmissionQuantity = model.EmissionQuantity,
                    EmissionCreatedAt = DateTime.Now,
                    EmissionUpdatedAt = DateTime.Now
                };

                _context.EmissionRecords.Add(emissionRecord);
                await _context.SaveChangesAsync();

                var emissionSource = await _context.EmissionSources.FindAsync(model.EmissionSourceId);
                var emissions = (double)(emissionSource.EmissionSourceEmissionFactor * model.EmissionQuantity);

                TempData["success"] = $"Emission record created successfully! Calculated emissions: {emissions:N3} tCO₂e";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating emission record");
                TempData["error"] = "Failed to create emission record. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, UpdateEmissionRecordViewModel model)
        {
            var userId = User.GetUserId();
            var organizationId = User.GetOrganizationId();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the errors below.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var emissionRecord = await _context.EmissionRecords
                    .Where(er => er.OrganizationId == organizationId)
                    .FirstOrDefaultAsync(er => er.EmissionRecordId == id);

                if (emissionRecord == null)
                {
                    TempData["error"] = "Emission record not found.";
                    return RedirectToAction(nameof(Index));
                }

                var existingRecord = await _context.EmissionRecords
                    .Where(er => er.OrganizationId == organizationId &&
                                 er.FactoryId == model.FactoryId &&
                                 er.EmissionSourceId == model.EmissionSourceId &&
                                 er.EmissionYear == model.EmissionYear &&
                                 er.EmissionMonth == model.EmissionMonth &&
                                 er.EmissionRecordId != id)
                    .FirstOrDefaultAsync();

                if (existingRecord != null)
                {
                    TempData["error"] = "Another emission record already exists for this source, factory, and period.";
                    return RedirectToAction(nameof(Index));
                }

                emissionRecord.EmissionSourceId = model.EmissionSourceId;
                emissionRecord.FactoryId = model.FactoryId;
                emissionRecord.EmissionYear = model.EmissionYear;
                emissionRecord.EmissionMonth = model.EmissionMonth;
                emissionRecord.EmissionQuantity = model.EmissionQuantity;
                emissionRecord.EmissionUpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var emissionSource = await _context.EmissionSources.FindAsync(model.EmissionSourceId);
                var emissions = (double)(emissionSource.EmissionSourceEmissionFactor * model.EmissionQuantity);

                TempData["success"] = $"Emission record updated successfully! Calculated emissions: {emissions:N3} tCO₂e";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating emission record");
                TempData["error"] = "Failed to update emission record. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Destroy(int id)
        {
            var organizationId = User.GetOrganizationId();

            try
            {
                var emissionRecord = await _context.EmissionRecords
                    .Where(er => er.OrganizationId == organizationId)
                    .FirstOrDefaultAsync(er => er.EmissionRecordId == id);

                if (emissionRecord == null)
                {
                    TempData["error"] = "Emission record not found.";
                    return RedirectToAction(nameof(Index));
                }

                _context.EmissionRecords.Remove(emissionRecord);
                await _context.SaveChangesAsync();

                TempData["success"] = "Emission record deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting emission record");
                TempData["error"] = "Failed to delete emission record. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Show(int id)
        {
            var organizationId = User.GetOrganizationId();

            var emissionRecord = await _context.EmissionRecords
                .Include(er => er.EmissionSource)
                .Include(er => er.Factory)
                .Include(er => er.User)
                .Include(er => er.Organization)
                .Where(er => er.OrganizationId == organizationId)
                .FirstOrDefaultAsync(er => er.EmissionRecordId == id);

            if (emissionRecord == null)
            {
                TempData["error"] = "Emission record not found.";
                return RedirectToAction(nameof(Index));
            }

            var emissions = (double)(emissionRecord.EmissionSource.EmissionSourceEmissionFactor * emissionRecord.EmissionQuantity);

            var similarRecords = await _context.EmissionRecords
                .Include(er => er.Factory)
                .Where(er => er.OrganizationId == organizationId &&
                            er.EmissionSourceId == emissionRecord.EmissionSourceId &&
                            er.EmissionRecordId != id)
                .OrderByDescending(er => er.EmissionYear)
                .ThenByDescending(er => er.EmissionMonth)
                .Take(5)
                .ToListAsync();

            var viewModel = new EmissionRecordShowViewModel
            {
                EmissionRecord = emissionRecord,
                Emissions = emissions,
                SimilarRecords = similarRecords
            };

            return View(viewModel);
        }
    }
}