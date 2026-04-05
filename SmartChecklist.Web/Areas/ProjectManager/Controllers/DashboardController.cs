using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Services;
using SmartChecklist.Web.ViewModels;
using System.Security.Claims;

namespace SmartChecklist.Web.Areas.ProjectManager.Controllers
{
    [Area("ProjectManager")]
    [Authorize(Roles = "ProjectManager")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AiAnalysisService _aiAnalysisService;
        private readonly GeminiTextGenerationService _geminiTextGenerationService;
        private readonly AiPromptService _aiPromptService;

        public DashboardController(
            ApplicationDbContext context,
            AiAnalysisService aiAnalysisService,
            GeminiTextGenerationService geminiTextGenerationService,
            AiPromptService aiPromptService)
        {
            _context = context;
            _aiAnalysisService = aiAnalysisService;
            _geminiTextGenerationService = geminiTextGenerationService;
            _aiPromptService = aiPromptService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projects = await _context.Projects
                .Where(p => p.ManagerId == userId && !p.IsDeleted)
                .ToListAsync();

            var projectIds = projects.Select(p => p.ProjectId).ToList();

            var checklists = await _context.Checklists
                .Where(c => projectIds.Contains(c.ProjectId) && !c.IsDeleted)
                .ToListAsync();

            var checklistIds = checklists.Select(c => c.ChecklistId).ToList();

            var tasks = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Include(t => t.Checklist)
                .Where(t => checklistIds.Contains(t.ChecklistId) && !t.IsDeleted)
                .ToListAsync();

            var now = DateTime.Now;
            var nextThreeDays = now.AddDays(3);

            var overdueTasksCount = tasks.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value < now &&
                t.Status != "Hoàn thành");

            var model = new ProjectManagerDashboardViewModel
            {
                TotalProjects = projects.Count,
                TotalChecklists = checklists.Count,
                TotalTasks = tasks.Count,
                OverdueTasks = overdueTasksCount,
                AiSummary = await _aiAnalysisService.GenerateProjectManagerSummaryAsync(userId!),
                AiInsights = await _aiAnalysisService.GenerateProjectManagerInsightsAsync(userId!),
                MemberPerformances = await _aiAnalysisService.GenerateMemberPerformanceAsync(userId!),
                AssistantTitle = "Trợ lý quản lý thông minh",
                SuggestedActions = await _aiAnalysisService.GenerateManagerSuggestedActionsAsync(userId!)
            };

            foreach (var project in projects)
            {
                var projectChecklistIds = checklists
                    .Where(c => c.ProjectId == project.ProjectId)
                    .Select(c => c.ChecklistId)
                    .ToList();

                var projectTasks = tasks
                    .Where(t => projectChecklistIds.Contains(t.ChecklistId))
                    .ToList();

                var completedTasks = projectTasks.Count(t => t.Status == "Hoàn thành");
                var totalTasks = projectTasks.Count;

                model.ProjectProgresses.Add(new ProjectProgressItemViewModel
                {
                    ProjectId = project.ProjectId,
                    ProjectName = project.Name,
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    ProgressPercent = totalTasks == 0 ? 0 : (int)Math.Round((double)completedTasks * 100 / totalTasks)
                });
            }

            model.ProjectProgresses = model.ProjectProgresses
                .OrderByDescending(x => x.ProgressPercent)
                .ToList();

            var alertTasks = tasks
                .Where(t =>
                    t.Status != "Hoàn thành" &&
                    t.Deadline.HasValue &&
                    t.Deadline.Value <= nextThreeDays)
                .OrderBy(t => t.Deadline)
                .Take(8)
                .ToList();

            foreach (var task in alertTasks)
            {
                var checklist = checklists.FirstOrDefault(c => c.ChecklistId == task.ChecklistId);
                var project = checklist == null
                    ? null
                    : projects.FirstOrDefault(p => p.ProjectId == checklist.ProjectId);

                model.UpcomingTasks.Add(new DashboardTaskAlertViewModel
                {
                    TaskItemId = task.TaskItemId,
                    TaskTitle = task.Title,
                    ProjectName = project?.Name ?? "Không xác định",
                    AssignedToName = task.AssignedToUser?.FullName ?? task.AssignedToUser?.Email ?? "Chưa phân công",
                    Deadline = task.Deadline,
                    Status = task.Status ?? "",
                    IsOverdue = task.Deadline.HasValue && task.Deadline.Value < now
                });
            }

            // ===== Gemini thật bắt đầu ở đây =====
            var completionRate = tasks.Count == 0
                ? 0
                : Math.Round((double)tasks.Count(t => t.Status == "Hoàn thành") * 100 / tasks.Count, 1);

            var mostRiskyChecklist = checklists.FirstOrDefault()?.Title;

            var overloadedMember = model.MemberPerformances
                .OrderByDescending(x => x.OverdueTasks)
                .ThenByDescending(x => x.TotalTasks)
                .FirstOrDefault()?.FullName;

            var managerPrompt = _aiPromptService.BuildManagerSummaryPrompt(
                projects.Count,
                tasks.Count,
                overdueTasksCount,
                completionRate,
                mostRiskyChecklist,
                overloadedMember
            );

            var aiSummary = await _geminiTextGenerationService.GenerateTextAsync(managerPrompt);

            if (!string.IsNullOrWhiteSpace(aiSummary) &&
                !aiSummary.StartsWith("Lỗi gọi Gemini API") &&
                !aiSummary.StartsWith("Gemini không"))
            {
                model.AiSummary = aiSummary;
            }

            return View(model);
        }

        public async Task<IActionResult> Report()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projects = await _context.Projects
                .Where(p => p.ManagerId == userId && !p.IsDeleted)
                .ToListAsync();

            var projectIds = projects.Select(p => p.ProjectId).ToList();

            var checklists = await _context.Checklists
                .Where(c => projectIds.Contains(c.ProjectId) && !c.IsDeleted)
                .ToListAsync();

            var checklistIds = checklists.Select(c => c.ChecklistId).ToList();

            var tasks = await _context.TaskItems
                .Where(t => checklistIds.Contains(t.ChecklistId) && !t.IsDeleted)
                .ToListAsync();

            var completedTasks = tasks.Count(t => t.Status == "Hoàn thành");

            var overdueTasks = tasks.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value < DateTime.Now &&
                t.Status != "Hoàn thành");

            var completionRate = tasks.Count == 0
                ? 0
                : (double)completedTasks * 100 / tasks.Count;

            var memberPerformances = await _aiAnalysisService.GenerateMemberPerformanceAsync(userId!);

            var model = new DashboardReportViewModel
            {
                TotalProjects = projects.Count,
                TotalChecklists = checklists.Count,
                TotalTasks = tasks.Count,
                CompletedTasks = completedTasks,
                OverdueTasks = overdueTasks,
                CompletionRate = completionRate,
                AiSummary = await _aiAnalysisService.GenerateProjectManagerSummaryAsync(userId!),
                AiInsights = await _aiAnalysisService.GenerateProjectManagerInsightsAsync(userId!),
                MemberPerformances = memberPerformances
            };

            var mostRiskyChecklist = checklists.FirstOrDefault()?.Title;

            var overloadedMember = memberPerformances
                .OrderByDescending(x => x.OverdueTasks)
                .ThenByDescending(x => x.TotalTasks)
                .FirstOrDefault()?.FullName;

            var managerPrompt = _aiPromptService.BuildManagerSummaryPrompt(
                projects.Count,
                tasks.Count,
                overdueTasks,
                completionRate,
                mostRiskyChecklist,
                overloadedMember
            );

            var aiSummary = await _geminiTextGenerationService.GenerateTextAsync(managerPrompt);

            if (!string.IsNullOrWhiteSpace(aiSummary) &&
                !aiSummary.StartsWith("Lỗi gọi Gemini API") &&
                !aiSummary.StartsWith("Gemini không"))
            {
                model.AiSummary = aiSummary;
            }

            return View(model);
        }
    }
}