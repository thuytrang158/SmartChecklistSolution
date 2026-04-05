using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;
using System.Security.Claims;
using SmartChecklist.Web.ViewModels;

namespace SmartChecklist.Web.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projects = await _context.Projects
                .Where(p => p.ManagerId == userId && !p.IsDeleted)
                .ToListAsync();

            return View(projects);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            project.ManagerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            project.CreatedAt = DateTime.Now;
            project.Status = "Mới tạo";
            project.IsDeleted = false;

            ModelState.Remove("ManagerId");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("Status");
            ModelState.Remove("IsDeleted");

            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.ManagerId == userId && !p.IsDeleted);

            if (project == null) return NotFound();

            var checklists = await _context.Checklists
                .Where(c => c.ProjectId == project.ProjectId && !c.IsDeleted)
                .ToListAsync();

            var checklistIds = checklists.Select(c => c.ChecklistId).ToList();

            var tasks = await _context.TaskItems
                .Where(t => checklistIds.Contains(t.ChecklistId) && !t.IsDeleted)
                .ToListAsync();

            var activeMembers = await _context.ProjectMembers
                .CountAsync(pm => pm.ProjectId == project.ProjectId && pm.IsActive);

            var completedTasks = tasks.Count(t => t.Status == "Hoàn thành");
            var overdueTasks = tasks.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value < DateTime.Now &&
                t.Status != "Hoàn thành");

            var model = new ProjectDetailsViewModel
            {
                Project = project,
                TotalChecklists = checklists.Count,
                TotalTasks = tasks.Count,
                CompletedTasks = completedTasks,
                OverdueTasks = overdueTasks,
                ActiveMembers = activeMembers,
                ProgressPercent = tasks.Count == 0 ? 0 : (int)Math.Round((double)completedTasks * 100 / tasks.Count)
            };

            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.ManagerId == userId && !p.IsDeleted);

            if (project == null) return NotFound();

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.ProjectId) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingProject = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.ManagerId == userId && !p.IsDeleted);

            if (existingProject == null) return NotFound();

            ModelState.Remove("ManagerId");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("Manager");
            ModelState.Remove("ProjectMembers");
            ModelState.Remove("Checklists");

            if (ModelState.IsValid)
            {
                existingProject.Name = project.Name;
                existingProject.Description = project.Description;
                existingProject.StartDate = project.StartDate;
                existingProject.EndDate = project.EndDate;
                existingProject.Status = project.Status;
                existingProject.PriorityScore = project.PriorityScore;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.ManagerId == userId && !p.IsDeleted);

            if (project == null) return NotFound();

            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.ManagerId == userId && !p.IsDeleted);

            if (project == null) return NotFound();

            project.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}