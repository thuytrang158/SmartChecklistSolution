using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;
using System.Security.Claims;
using SmartChecklist.Web.Services;

namespace SmartChecklist.Web.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class TaskItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AiSuggestionService _aiSuggestionService;

        public TaskItemsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AiSuggestionService aiSuggestionService)
        {
            _context = context;
            _userManager = userManager;
            _aiSuggestionService = aiSuggestionService;
        }

        public async Task<IActionResult> Index(int checklistId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checklist = await _context.Checklists
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ChecklistId == checklistId && !c.IsDeleted);

            if (checklist == null)
                return NotFound();

            if (checklist.Project == null || checklist.Project.ManagerId != currentUserId)
                return Forbid();

            ViewBag.ChecklistId = checklistId;
            ViewBag.ChecklistTitle = checklist.Title;
            ViewBag.ProjectId = checklist.ProjectId;

            var tasks = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Where(t => t.ChecklistId == checklistId && !t.IsDeleted)
                .ToListAsync();

            return View(tasks);
        }

        public async Task<IActionResult> Overview()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tasks = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .Where(t => !t.IsDeleted
                            && t.Checklist != null
                            && !t.Checklist.IsDeleted
                            && t.Checklist.Project != null
                            && !t.Checklist.Project.IsDeleted
                            && t.Checklist.Project.ManagerId == currentUserId)
                .OrderBy(t => t.Status)
                .ThenBy(t => t.Deadline)
                .ToListAsync();

            ViewBag.IsOverview = true;
            return View("Index", tasks);
        }
        public async Task<IActionResult> Create(int checklistId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var checklist = await _context.Checklists
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ChecklistId == checklistId && !c.IsDeleted);

            if (checklist == null)
                return NotFound();

            if (checklist.Project == null || checklist.Project.ManagerId != currentUserId)
                return Forbid();

            var memberUserIds = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == checklist.ProjectId && pm.IsActive)
                .Select(pm => pm.UserId)
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    DisplayName = (u.FullName ?? u.Email) + " - " + u.Email
                })
                .ToListAsync();

            ViewBag.ChecklistId = checklistId;
            ViewBag.ChecklistTitle = checklist.Title;
            ViewBag.AssignedToUserId = new SelectList(users, "Id", "DisplayName");
            ViewBag.SuggestedTaskTitles = await _aiSuggestionService.SuggestTaskTitlesAsync(checklistId);

            // THÊM 2 DÒNG NÀY
            ViewBag.AiPriority = TempData["AiPriority"];
            ViewBag.AiDeadline = TempData["AiDeadline"];

            return View(new TaskItem
            {
                ChecklistId = checklistId,
                Status = "Chưa thực hiện",
                Priority = "Trung bình"
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem taskItem)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            taskItem.CreatedByUserId = currentUserId!;
            taskItem.CreatedAt = DateTime.Now;
            taskItem.IsDeleted = false;

            ModelState.Remove("CreatedByUserId");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("Checklist");
            ModelState.Remove("AssignedToUser");
            ModelState.Remove("CreatedByUser");

            var checklist = await _context.Checklists
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.ChecklistId == taskItem.ChecklistId && !c.IsDeleted);

            if (checklist == null)
                return NotFound();

            if (checklist.Project == null || checklist.Project.ManagerId != currentUserId)
                return Forbid();

            if (ModelState.IsValid)
            {
                _context.TaskItems.Add(taskItem);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { checklistId = taskItem.ChecklistId });
            }

            var memberUserIds = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == checklist.ProjectId && pm.IsActive)
                .Select(pm => pm.UserId)
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    DisplayName = (u.FullName ?? u.Email) + " - " + u.Email
                })
                .ToListAsync();

            ViewBag.ChecklistId = taskItem.ChecklistId;
            ViewBag.ChecklistTitle = checklist.Title;
            ViewBag.AssignedToUserId = new SelectList(users, "Id", "DisplayName", taskItem.AssignedToUserId);

            return View(taskItem);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.TaskProgressLogs!)
                    .ThenInclude(l => l.ChangedByUser)
                .FirstOrDefaultAsync(t => t.TaskItemId == id && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            return View(taskItem);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(t => t.TaskItemId == id && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            var memberUserIds = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == taskItem.Checklist.ProjectId && pm.IsActive)
                .Select(pm => pm.UserId)
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    DisplayName = (u.FullName ?? u.Email) + " - " + u.Email
                })
                .ToListAsync();

            ViewBag.AssignedToUserId = new SelectList(users, "Id", "DisplayName", taskItem.AssignedToUserId);
            ViewBag.ChecklistTitle = taskItem.Checklist.Title;

            return View(taskItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskItem taskItem)
        {
            if (id != taskItem.TaskItemId)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingTask = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(t => t.TaskItemId == id && !t.IsDeleted);

            if (existingTask == null)
                return NotFound();

            if (existingTask.Checklist?.Project == null || existingTask.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            ModelState.Remove("CreatedByUserId");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");
            ModelState.Remove("CompletedAt");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("Checklist");
            ModelState.Remove("AssignedToUser");
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("Reminders");
            ModelState.Remove("TaskProgressLogs");

            if (ModelState.IsValid)
            {
                existingTask.Title = taskItem.Title;
                existingTask.Description = taskItem.Description;
                existingTask.Deadline = taskItem.Deadline;
                existingTask.Priority = taskItem.Priority;
                existingTask.Status = taskItem.Status;
                existingTask.AssignedToUserId = taskItem.AssignedToUserId;
                existingTask.UpdatedAt = DateTime.Now;

                if (taskItem.Status == "Hoàn thành" && existingTask.CompletedAt == null)
                {
                    existingTask.CompletedAt = DateTime.Now;
                }
                else if (taskItem.Status != "Hoàn thành")
                {
                    existingTask.CompletedAt = null;
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { checklistId = existingTask.ChecklistId });
            }

            var memberUserIds = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == existingTask.Checklist.ProjectId && pm.IsActive)
                .Select(pm => pm.UserId)
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    DisplayName = (u.FullName ?? u.Email) + " - " + u.Email
                })
                .ToListAsync();

            ViewBag.AssignedToUserId = new SelectList(users, "Id", "DisplayName", taskItem.AssignedToUserId);
            ViewBag.ChecklistTitle = existingTask.Checklist.Title;

            return View(taskItem);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TaskItemId == id && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            return View(taskItem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(t => t.TaskItemId == id && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            taskItem.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { checklistId = taskItem.ChecklistId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(t => t.TaskItemId == id && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            var oldStatus = taskItem.Status;

            if (oldStatus != newStatus)
            {
                taskItem.Status = newStatus;
                taskItem.UpdatedAt = DateTime.Now;

                if (newStatus == "Hoàn thành")
                {
                    taskItem.CompletedAt = DateTime.Now;
                }
                else
                {
                    taskItem.CompletedAt = null;
                }

                var log = new TaskProgressLog
                {
                    TaskItemId = taskItem.TaskItemId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedByUserId = currentUserId!,
                    ChangedAt = DateTime.Now,
                    Note = $"Cập nhật trạng thái từ '{oldStatus}' sang '{newStatus}'"
                };

                _context.TaskProgressLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { checklistId = taskItem.ChecklistId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAiSuggestions(string? assignedToUserId, int checklistId)
        {
            var priority = await _aiSuggestionService.SuggestPriorityAsync(assignedToUserId);
            var deadline = await _aiSuggestionService.SuggestDeadlineAsync(assignedToUserId);

            TempData["AiPriority"] = priority;
            TempData["AiDeadline"] = deadline?.ToString("yyyy-MM-ddTHH:mm");

            return RedirectToAction(nameof(Create), new { checklistId });
        }

    }
}