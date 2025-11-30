using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sustain.Data;
using Sustain.Models;
using Sustain.Utilities.Helpers;
using Sustain.ViewModels.FactoryVMs;
using System.Security.Claims;

namespace Sustain.Controllers
{
    [Authorize]
    public class FactoryController : Controller
    {
        private readonly SustainDbContext _context;
        private readonly ILogger<FactoryController> _logger;

        public FactoryController(SustainDbContext context, ILogger<FactoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Factory/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.GetUserId();
                var organizationId = User.GetOrganizationId();

                if (organizationId == null)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }

                var factories = await _context.Factories
                    .Where(f => f.OrganizationId == organizationId.Value)
                    .OrderBy(f => f.FactoryName)
                    .ToListAsync();

                return View(factories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading factories");
                TempData["error"] = "Error loading factories. Please try again.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // POST: Factory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateFactoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();
                var userId = User.GetUserId();

                if (organizationId == null || userId == null)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }

                // Check subscription limits
                var canCreate = await SubscriptionHelper.CanCreateFactoryAsync(_context, organizationId, userId);
                if (!canCreate)
                {
                    TempData["error"] = "You have reached your subscription limit for factories. Please upgrade your plan.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if factory code already exists
                var existingFactory = await _context.Factories
                    .FirstOrDefaultAsync(f => f.FactoryCode == model.FactoryCode);

                if (existingFactory != null)
                {
                    TempData["error"] = "A factory with this code already exists.";
                    return RedirectToAction(nameof(Index));
                }

                var factory = new Factory
                {
                    OrganizationId = organizationId.Value,
                    FactoryName = model.FactoryName,
                    FactoryCode = model.FactoryCode,
                    FactoryLocation = model.FactoryLocation
                };

                _context.Factories.Add(factory);
                await _context.SaveChangesAsync();

                TempData["success"] = "Factory added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating factory");
                TempData["error"] = "Error adding factory. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Factory/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditFactoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fix the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var organizationId = User.GetOrganizationId();

                if (organizationId == null)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }

                var factory = await _context.Factories
                    .FirstOrDefaultAsync(f => f.FactoryId == id && f.OrganizationId == organizationId.Value);

                if (factory == null)
                {
                    TempData["error"] = "Factory not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if factory code already exists (excluding current factory)
                var existingFactory = await _context.Factories
                    .FirstOrDefaultAsync(f => f.FactoryCode == model.FactoryCode && f.FactoryId != id);

                if (existingFactory != null)
                {
                    TempData["error"] = "A factory with this code already exists.";
                    return RedirectToAction(nameof(Index));
                }

                factory.FactoryName = model.FactoryName;
                factory.FactoryCode = model.FactoryCode;
                factory.FactoryLocation = model.FactoryLocation;

                await _context.SaveChangesAsync();

                TempData["success"] = "Factory updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating factory with ID {FactoryId}", id);
                TempData["error"] = "Error updating factory. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Factory/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var organizationId = User.GetOrganizationId();

                if (organizationId == null)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }

                var factory = await _context.Factories
                    .Include(f => f.EmissionRecords)
                    .FirstOrDefaultAsync(f => f.FactoryId == id && f.OrganizationId == organizationId.Value);

                if (factory == null)
                {
                    TempData["error"] = "Factory not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if factory has emission records
                if (factory.EmissionRecords.Any())
                {
                    TempData["error"] = "Cannot delete factory with existing emission records.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Factories.Remove(factory);
                await _context.SaveChangesAsync();

                TempData["success"] = "Factory deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting factory with ID {FactoryId}", id);
                TempData["error"] = "Error deleting factory. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}