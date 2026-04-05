using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class RemindersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RemindersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int taskItemId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskItemId && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            ViewBag.TaskItemId = taskItemId;
            ViewBag.TaskTitle = taskItem.Title;
            ViewBag.ChecklistId = taskItem.ChecklistId;

            var reminders = await _context.Reminders
                .Where(r => r.TaskItemId == taskItemId)
                .OrderBy(r => r.ReminderTime)
                .ToListAsync();

            return View(reminders);
        }

        public async Task<IActionResult> Overview()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reminders = await _context.Reminders
                .Include(r => r.TaskItem)
                    .ThenInclude(t => t!.Checklist)
                        .ThenInclude(c => c.Project)
                .Where(r => r.TaskItem != null
                            && !r.TaskItem.IsDeleted
                            && r.TaskItem.Checklist != null
                            && !r.TaskItem.Checklist.IsDeleted
                            && r.TaskItem.Checklist.Project != null
                            && !r.TaskItem.Checklist.Project.IsDeleted
                            && r.TaskItem.Checklist.Project.ManagerId == currentUserId)
                .OrderBy(r => r.ReminderTime)
                .ToListAsync();

            ViewBag.IsOverview = true;
            return View("Index", reminders);
        }

        public async Task<IActionResult> Create(int taskItemId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(t => t.TaskItemId == taskItemId && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            ViewBag.TaskItemId = taskItemId;
            ViewBag.TaskTitle = taskItem.Title;

            return View(new Reminder
            {
                TaskItemId = taskItemId,
                ReminderTime = DateTime.Now.AddHours(1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reminder reminder)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskItem = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(t => t.TaskItemId == reminder.TaskItemId && !t.IsDeleted);

            if (taskItem == null)
                return NotFound();

            if (taskItem.Checklist?.Project == null || taskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            if (ModelState.IsValid)
            {
                reminder.CreatedAt = DateTime.Now;
                reminder.IsSent = false;

                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { taskItemId = reminder.TaskItemId });
            }

            ViewBag.TaskItemId = reminder.TaskItemId;
            ViewBag.TaskTitle = taskItem.Title;
            return View(reminder);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reminder = await _context.Reminders
                .Include(r => r.TaskItem)
                    .ThenInclude(t => t!.Checklist)
                        .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(r => r.ReminderId == id);

            if (reminder == null)
                return NotFound();

            if (reminder.TaskItem?.Checklist?.Project == null || reminder.TaskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            return View(reminder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reminder = await _context.Reminders
                .Include(r => r.TaskItem)
                    .ThenInclude(t => t!.Checklist)
                        .ThenInclude(c => c.Project)
                .FirstOrDefaultAsync(r => r.ReminderId == id);

            if (reminder == null)
                return NotFound();

            if (reminder.TaskItem?.Checklist?.Project == null || reminder.TaskItem.Checklist.Project.ManagerId != currentUserId)
                return Forbid();

            var taskItemId = reminder.TaskItemId;

            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { taskItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateNotifications()
        {
            var now = DateTime.Now;

            var dueReminders = await _context.Reminders
                .Include(r => r.TaskItem)
                .Where(r => !r.IsSent && r.ReminderTime <= now)
                .ToListAsync();

            foreach (var reminder in dueReminders)
            {
                if (reminder.TaskItem != null && !string.IsNullOrEmpty(reminder.TaskItem.AssignedToUserId))
                {
                    var notification = new Notification
                    {
                        UserId = reminder.TaskItem.AssignedToUserId,
                        Title = "Nhắc nhở công việc",
                        Message = $"Nhắc nhẹ bạn một chút nhé: công việc \"{reminder.TaskItem.Title}\" đã đến thời điểm cần xử lý rồi. Nếu bắt đầu ngay bây giờ thì bạn sẽ đỡ bị dồn việc hơn đó.",
                        Type = "Reminder",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        RelatedTaskId = reminder.TaskItem.TaskItemId
                    };

                    _context.Notifications.Add(notification);

                    reminder.IsSent = true;
                    reminder.SentAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã kiểm tra reminder và tạo notification.";
            return RedirectToAction("Index", "Dashboard", new { area = "ProjectManager" });
        }
    }
}