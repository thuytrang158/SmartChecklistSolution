using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;
using System.Security.Claims;
using SmartChecklist.Web.ViewModels;
using SmartChecklist.Web.Services;

namespace SmartChecklist.Web.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class ChecklistsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly AiSuggestionService _aiSuggestionService;

        public ChecklistsController(ApplicationDbContext context, AiSuggestionService aiSuggestionService)
        {
            _context = context;
            _aiSuggestionService = aiSuggestionService;
        }

        public async Task<IActionResult> Index(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ManagerId == userId && !p.IsDeleted);

            if (project == null)
                return NotFound();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;

            var checklists = await _context.Checklists
                .Where(c => c.ProjectId == projectId && !c.IsDeleted)
                .Select(c => new ChecklistProgressViewModel
                {
                    ChecklistId = c.ChecklistId,
                    ProjectId = c.ProjectId,
                    Title = c.Title,
                    Description = c.Description,
                    WorkType = c.WorkType,
                    CreatedAt = c.CreatedAt,
                    TotalTasks = c.TaskItems!.Count(t => !t.IsDeleted),
                    CompletedTasks = c.TaskItems!.Count(t => !t.IsDeleted && t.Status == "Hoàn thành"),
                    ProgressPercent = c.TaskItems!.Count(t => !t.IsDeleted) == 0
                        ? 0
                        : (double)c.TaskItems.Count(t => !t.IsDeleted && t.Status == "Hoàn thành") * 100
                            / c.TaskItems.Count(t => !t.IsDeleted)
                })
                .ToListAsync();

            return View(checklists);
        }

        public async Task<IActionResult> Overview()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checklists = await _context.Checklists
                .Include(c => c.Project)
                .Where(c => !c.IsDeleted
                            && c.Project != null
                            && !c.Project.IsDeleted
                            && c.Project.ManagerId == userId)
                .Select(c => new ChecklistProgressViewModel
                {
                    ChecklistId = c.ChecklistId,
                    ProjectId = c.ProjectId,
                    Title = c.Title,
                    Description = c.Description,
                    WorkType = c.WorkType,
                    CreatedAt = c.CreatedAt,
                    TotalTasks = c.TaskItems!.Count(t => !t.IsDeleted),
                    CompletedTasks = c.TaskItems!.Count(t => !t.IsDeleted && t.Status == "Hoàn thành"),
                    ProgressPercent = c.TaskItems!.Count(t => !t.IsDeleted) == 0
                        ? 0
                        : (double)c.TaskItems.Count(t => !t.IsDeleted && t.Status == "Hoàn thành") * 100
                            / c.TaskItems.Count(t => !t.IsDeleted)
                })
                .ToListAsync();

            ViewBag.IsOverview = true;
            return View("Index", checklists);
        }

        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ManagerId == userId && !p.IsDeleted);

            if (project == null)
                return NotFound();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;

            ViewBag.SuggestedTitles = await _aiSuggestionService.SuggestChecklistTitlesAsync(userId!, projectId);
            ViewBag.SuggestedWorkTypes = await _aiSuggestionService.SuggestWorkTypesAsync(userId!, projectId);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Checklist checklist)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            checklist.CreatedBy = userId!;
            checklist.CreatedAt = DateTime.Now;
            checklist.IsDeleted = false;

            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("Project");

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == checklist.ProjectId && p.ManagerId == userId && !p.IsDeleted);

            if (project == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Checklists.Add(checklist);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { projectId = checklist.ProjectId });
            }

            ViewBag.ProjectId = checklist.ProjectId;
            ViewBag.ProjectName = project.Name;
            return View(checklist);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checklist = await _context.Checklists
                .Include(c => c.Project)
                .Include(c => c.TaskItems)
                .FirstOrDefaultAsync(c => c.ChecklistId == id && !c.IsDeleted);

            if (checklist == null)
                return NotFound();

            if (checklist.Project == null || checklist.Project.ManagerId != userId)
                return Forbid();

            return View(checklist);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checklist = await _context.Checklists
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ChecklistId == id && !c.IsDeleted);

            if (checklist == null)
                return NotFound();

            if (checklist.Project == null || checklist.Project.ManagerId != userId)
                return Forbid();

            ViewBag.ProjectName = checklist.Project.Name;
            return View(checklist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Checklist checklist)
        {
            if (id != checklist.ChecklistId)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingChecklist = await _context.Checklists
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ChecklistId == id && !c.IsDeleted);

            if (existingChecklist == null)
                return NotFound();

            if (existingChecklist.Project == null || existingChecklist.Project.ManagerId != userId)
                return Forbid();

            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("Project");
            ModelState.Remove("TaskItems");

            if (ModelState.IsValid)
            {
                existingChecklist.Title = checklist.Title;
                existingChecklist.Description = checklist.Description;
                existingChecklist.WorkType = checklist.WorkType;
                existingChecklist.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { projectId = existingChecklist.ProjectId });
            }

            ViewBag.ProjectName = existingChecklist.Project?.Name;
            return View(checklist);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checklist = await _context.Checklists
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ChecklistId == id && !c.IsDeleted);

            if (checklist == null)
                return NotFound();

            if (checklist.Project == null || checklist.Project.ManagerId != userId)
                return Forbid();

            return View(checklist);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checklist = await _context.Checklists
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ChecklistId == id && !c.IsDeleted);

            if (checklist == null)
                return NotFound();

            if (checklist.Project == null || checklist.Project.ManagerId != userId)
                return Forbid();

            checklist.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { projectId = checklist.ProjectId });
        }
    }
}