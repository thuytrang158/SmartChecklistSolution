using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Services;
using SmartChecklist.Web.ViewModels;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.TeamMember.Controllers
{
    [Area("TeamMember")]
    [Authorize(Roles = "TeamMember")]
    public class ProgressController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AiAnalysisService _aiAnalysisService;

        public ProgressController(ApplicationDbContext context, AiAnalysisService aiAnalysisService)
        {
            _context = context;
            _aiAnalysisService = aiAnalysisService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tasks = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c.Project)
                .Where(t => t.AssignedToUserId == currentUserId && !t.IsDeleted)
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .ToListAsync();

            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == "Hoàn thành");
            var inProgressTasks = tasks.Count(t => t.Status == "Đang thực hiện");
            var notStartedTasks = tasks.Count(t => t.Status == "Chưa thực hiện");
            var overdueTasks = tasks.Count(t =>
                t.Deadline != null &&
                t.Deadline < DateTime.Now &&
                t.Status != "Hoàn thành");

            var completionRate = totalTasks == 0 ? 0 : (double)completedTasks * 100 / totalTasks;

            var model = new PersonalProgressViewModel
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                NotStartedTasks = notStartedTasks,
                OverdueTasks = overdueTasks,
                CompletionRate = completionRate,
                RecentTasks = tasks.Take(10).ToList(),
                AiComment = await _aiAnalysisService.GeneratePersonalProgressCommentAsync(currentUserId!),
                AiInsights = await _aiAnalysisService.GenerateTeamMemberInsightsAsync(currentUserId!)
            };

            return View(model);
        }
    }
}