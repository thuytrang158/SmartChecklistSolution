using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.ViewModels;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class AiReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AiReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MemberPerformance()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projectIds = await _context.Projects
                .Where(p => p.ManagerId == currentUserId && !p.IsDeleted)
                .Select(p => p.ProjectId)
                .ToListAsync();

            var memberIds = await _context.ProjectMembers
                .Where(pm => projectIds.Contains(pm.ProjectId) && pm.IsActive)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();

            var users = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            var tasks = await _context.TaskItems
                .Include(t => t.Checklist)
                .Where(t => !t.IsDeleted
                            && t.AssignedToUserId != null
                            && projectIds.Contains(t.Checklist!.ProjectId))
                .ToListAsync();

            var result = users.Select(u =>
            {
                var userTasks = tasks.Where(t => t.AssignedToUserId == u.Id).ToList();
                var completed = userTasks.Count(t => t.Status == "Hoàn thành");
                var overdue = userTasks.Count(t => t.Deadline != null && t.Deadline < DateTime.Now && t.Status != "Hoàn thành");

                return new MemberPerformanceViewModel
                {
                    UserId = u.Id,
                    Email = u.Email ?? "",
                    FullName = u.FullName,
                    TotalTasks = userTasks.Count,
                    CompletedTasks = completed,
                    OverdueTasks = overdue,
                    CompletionRate = userTasks.Count == 0 ? 0 : (double)completed * 100 / userTasks.Count
                };
            }).ToList();

            return View(result);
        }
    }
}