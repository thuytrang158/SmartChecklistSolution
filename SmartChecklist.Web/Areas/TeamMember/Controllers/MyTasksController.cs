using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.TeamMember.Controllers
{
    [Area("TeamMember")]
    [Authorize(Roles = "TeamMember")]
    public class MyTasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MyTasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.DebugUserId = currentUserId;

            var tasks = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .Where(t => t.AssignedToUserId == currentUserId && !t.IsDeleted)
                .OrderBy(t => t.Deadline)
                .ToListAsync();

            return View(tasks);
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .Include(t => t.TaskProgressLogs!)
                    .ThenInclude(l => l.ChangedByUser)
                .FirstOrDefaultAsync(t => t.TaskItemId == id && t.AssignedToUserId == currentUserId && !t.IsDeleted);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.TaskItemId == id && t.AssignedToUserId == currentUserId && !t.IsDeleted);

            if (task == null)
                return NotFound();

            var oldStatus = task.Status;

            if (oldStatus != newStatus)
            {
                task.Status = newStatus;
                task.UpdatedAt = DateTime.Now;

                if (newStatus == "Hoàn thành")
                    task.CompletedAt = DateTime.Now;
                else
                    task.CompletedAt = null;

                _context.TaskProgressLogs.Add(new TaskProgressLog
                {
                    TaskItemId = task.TaskItemId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedByUserId = currentUserId!,
                    ChangedAt = DateTime.Now,
                    Note = $"Team Member cập nhật trạng thái từ '{oldStatus}' sang '{newStatus}'"
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}