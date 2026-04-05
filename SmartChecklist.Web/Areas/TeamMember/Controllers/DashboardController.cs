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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AiSuggestionService _aiSuggestionService;
        private readonly AiAnalysisService _aiAnalysisService;
        private readonly GeminiTextGenerationService _geminiTextGenerationService;
        private readonly AiPromptService _aiPromptService;

        public DashboardController(
            ApplicationDbContext context,
            AiSuggestionService aiSuggestionService,
            AiAnalysisService aiAnalysisService,
            GeminiTextGenerationService geminiTextGenerationService,
            AiPromptService aiPromptService)
        {
            _context = context;
            _aiSuggestionService = aiSuggestionService;
            _aiAnalysisService = aiAnalysisService;
            _geminiTextGenerationService = geminiTextGenerationService;
            _aiPromptService = aiPromptService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var now = DateTime.Now;
            var dueSoonDate = now.AddDays(3);

            var myProjectIds = await _context.ProjectMembers
                .Where(pm => pm.UserId == currentUserId
                             && pm.IsActive
                             && pm.Project != null
                             && !pm.Project.IsDeleted)
                .Select(pm => pm.ProjectId)
                .Distinct()
                .ToListAsync();

            var myTasksQuery = _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c!.Project)
                .Where(t => t.AssignedToUserId == currentUserId
                            && !t.IsDeleted
                            && t.Checklist != null
                            && !t.Checklist.IsDeleted
                            && t.Checklist.Project != null
                            && !t.Checklist.Project.IsDeleted);

            var totalProjects = myProjectIds.Count;
            var totalAssignedTasks = await myTasksQuery.CountAsync();

            var inProgressTasks = await myTasksQuery.CountAsync(t =>
                t.Status == "Đang thực hiện");

            var completedTasks = await myTasksQuery.CountAsync(t =>
                t.Status == "Hoàn thành");

            var overdueTasks = await myTasksQuery.CountAsync(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value < now &&
                t.Status != "Hoàn thành");

            var dueSoonTasks = await myTasksQuery.CountAsync(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value >= now &&
                t.Deadline.Value <= dueSoonDate &&
                t.Status != "Hoàn thành");

            var priorityTasks = await myTasksQuery
                .Where(t => t.Status != "Hoàn thành")
                .OrderByDescending(t => t.Priority == "Khẩn cấp")
                .ThenByDescending(t => t.Priority == "Cao")
                .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
                .Take(6)
                .Select(t => new TeamMemberTaskAlertViewModel
                {
                    TaskItemId = t.TaskItemId,
                    TaskTitle = t.Title,
                    ProjectName = t.Checklist!.Project!.Name,
                    ChecklistName = t.Checklist.Title,
                    Deadline = t.Deadline,
                    Priority = t.Priority,
                    Status = t.Status,
                    IsOverdue = t.Deadline.HasValue && t.Deadline.Value < now
                })
                .ToListAsync();

            var myProjects = await _context.Projects
                .Where(p => myProjectIds.Contains(p.ProjectId) && !p.IsDeleted)
                .Select(p => new TeamMemberProjectProgressViewModel
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.Name,
                    TotalTasks = p.Checklists!
                        .Where(c => !c.IsDeleted)
                        .SelectMany(c => c.TaskItems!)
                        .Count(t => !t.IsDeleted && t.AssignedToUserId == currentUserId),

                    CompletedTasks = p.Checklists!
                        .Where(c => !c.IsDeleted)
                        .SelectMany(c => c.TaskItems!)
                        .Count(t => !t.IsDeleted
                                    && t.AssignedToUserId == currentUserId
                                    && t.Status == "Hoàn thành")
                })
                .ToListAsync();

            foreach (var item in myProjects)
            {
                item.ProgressPercent = item.TotalTasks == 0
                    ? 0
                    : (int)Math.Round((double)item.CompletedTasks * 100 / item.TotalTasks);
            }

            var completionRate = totalAssignedTasks == 0
                ? 0
                : Math.Round((double)completedTasks * 100 / totalAssignedTasks, 1);

            var userName = User.Identity?.Name ?? "bạn";
            var topTaskTitle = priorityTasks.FirstOrDefault()?.TaskTitle;

            var reminderPrompt = _aiPromptService.BuildTeamMemberReminderPrompt(
                userName,
                overdueTasks,
                dueSoonTasks,
                topTaskTitle,
                completionRate
            );

            string aiReminder;

            try
            {
                aiReminder = await _geminiTextGenerationService.GenerateTextAsync(reminderPrompt);
            }
            catch
            {
                aiReminder = "Hôm nay bạn hãy ưu tiên các công việc quan trọng trước nhé.";
            }

            var model = new TeamMemberDashboardViewModel
            {
                TotalProjects = totalProjects,
                TotalAssignedTasks = totalAssignedTasks,
                InProgressTasks = inProgressTasks,
                CompletedTasks = completedTasks,
                OverdueTasks = overdueTasks,
                DueSoonTasks = dueSoonTasks,
                CompletionRate = completionRate,
                PriorityTasks = priorityTasks,
                MyProjects = myProjects,

                FriendlyReminder = aiReminder,
                DailyFocusSuggestion = await _aiSuggestionService.GenerateDailyFocusSuggestionAsync(currentUserId),
                AiInsights = await _aiAnalysisService.GenerateTeamMemberInsightsAsync(currentUserId),

                AssistantTitle = "Trợ lý công việc thông minh",
                AssistantMood = overdueTasks > 0
                    ? "Cần ưu tiên"
                    : dueSoonTasks > 0
                        ? "Nhắc nhẹ"
                        : "Ổn định"
            };

            return View(model);
        }
    }
}