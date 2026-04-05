using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;
using SmartChecklist.Web.ViewModels;

namespace SmartChecklist.Web.Services
{
    public class AiAnalysisService
    {
        private readonly ApplicationDbContext _context;

        public AiAnalysisService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskItem>> GetPriorityTasksForUserAsync(string userId, int take = 5)
        {
            return await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c!.Project)
                .Where(t => t.AssignedToUserId == userId
                            && !t.IsDeleted
                            && t.Status != "Hoàn thành"
                            && t.Checklist != null
                            && !t.Checklist.IsDeleted
                            && t.Checklist.Project != null
                            && !t.Checklist.Project.IsDeleted)
                .OrderByDescending(t => t.Priority == "Khẩn cấp")
                .ThenByDescending(t => t.Priority == "Cao")
                .ThenBy(t => t.Deadline ?? DateTime.MaxValue)
                .Take(take)
                .ToListAsync();
        }

        public async Task<double> GetOnTimeCompletionRateAsync(string userId)
        {
            var completedTasks = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId
                            && !t.IsDeleted
                            && t.CompletedAt.HasValue
                            && t.Deadline.HasValue)
                .ToListAsync();

            if (!completedTasks.Any())
                return 0;

            var onTimeCount = completedTasks.Count(t => t.CompletedAt!.Value <= t.Deadline!.Value);
            return Math.Round((double)onTimeCount * 100 / completedTasks.Count, 1);
        }

        public async Task<double> GetAverageCompletionDaysAsync(string userId)
        {
            var completedTasks = await _context.TaskItems
                .Where(t => t.AssignedToUserId == userId
                            && !t.IsDeleted
                            && t.CompletedAt.HasValue)
                .ToListAsync();

            var valid = completedTasks
                .Where(t => t.CompletedAt!.Value > t.CreatedAt)
                .ToList();

            if (!valid.Any())
                return 0;

            return Math.Round(valid.Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalDays), 1);
        }

        public async Task<List<AiInsightViewModel>> GenerateTeamMemberInsightsAsync(string userId)
        {
            var now = DateTime.Now;
            var threeDaysLater = now.AddDays(3);

            var tasks = await _context.TaskItems
                .Include(t => t.Checklist)
                    .ThenInclude(c => c!.Project)
                .Where(t => t.AssignedToUserId == userId
                            && !t.IsDeleted
                            && t.Checklist != null
                            && !t.Checklist.IsDeleted
                            && t.Checklist.Project != null
                            && !t.Checklist.Project.IsDeleted)
                .ToListAsync();

            var insights = new List<AiInsightViewModel>();

            var overdueTasks = tasks.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value < now &&
                t.Status != "Hoàn thành");

            var dueSoonTasks = tasks.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value >= now &&
                t.Deadline.Value <= threeDaysLater &&
                t.Status != "Hoàn thành");

            var inProgressTasks = tasks.Count(t => t.Status == "Đang thực hiện");
            var completedTasks = tasks.Count(t => t.Status == "Hoàn thành");
            var totalTasks = tasks.Count;

            var completionRate = totalTasks == 0 ? 0 : Math.Round((double)completedTasks * 100 / totalTasks, 1);
            var onTimeRate = await GetOnTimeCompletionRateAsync(userId);
            var avgCompletionDays = await GetAverageCompletionDaysAsync(userId);

            if (overdueTasks >= 3)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Risk",
                    Title = "Nguy cơ trễ tiến độ",
                    Message = $"Bạn đang có {overdueTasks} công việc quá hạn. Nên xử lý các việc gần deadline trước để tránh bị dồn.",
                    Severity = "Danger",
                    Score = 0.92
                });
            }

            if (dueSoonTasks > 0)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Reminder",
                    Title = "Có việc cần chú ý sớm",
                    Message = $"Hiện có {dueSoonTasks} công việc sẽ đến hạn trong 3 ngày tới. Làm trước từng việc một sẽ nhẹ hơn nhiều.",
                    Severity = "Warning",
                    Score = 0.78
                });
            }

            if (inProgressTasks >= 4)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Focus",
                    Title = "Đang ôm khá nhiều việc cùng lúc",
                    Message = $"Bạn có {inProgressTasks} công việc đang thực hiện. Nên chốt xong từng task trước khi mở thêm việc mới.",
                    Severity = "Warning",
                    Score = 0.80
                });
            }

            if (completionRate >= 70)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Strength",
                    Title = "Tiến độ cá nhân khá ổn",
                    Message = $"Tỷ lệ hoàn thành hiện tại là {completionRate:0.#}%. Bạn đang giữ nhịp làm việc tương đối tốt.",
                    Severity = "Success",
                    Score = 0.86
                });
            }

            if (onTimeRate >= 75)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Strength",
                    Title = "Bám deadline tốt",
                    Message = $"Tỷ lệ hoàn thành đúng hạn của bạn là {onTimeRate:0.#}%, đây là một điểm rất tích cực.",
                    Severity = "Success",
                    Score = 0.89
                });
            }

            if (avgCompletionDays > 0)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Info",
                    Title = "Thời gian hoàn thành trung bình",
                    Message = $"Trung bình bạn hoàn thành một công việc trong khoảng {avgCompletionDays:0.#} ngày.",
                    Severity = "Info",
                    Score = 0.65
                });
            }

            return insights
                .OrderByDescending(i => i.Score)
                .Take(4)
                .ToList();
        }

        public async Task<List<MemberPerformanceViewModel>> GenerateMemberPerformanceAsync(string managerId)
        {
            var projects = await _context.Projects
                .Where(p => p.ManagerId == managerId && !p.IsDeleted)
                .Select(p => p.ProjectId)
                .ToListAsync();

            if (!projects.Any())
                return new List<MemberPerformanceViewModel>();

            var checklistIds = await _context.Checklists
                .Where(c => projects.Contains(c.ProjectId) && !c.IsDeleted)
                .Select(c => c.ChecklistId)
                .ToListAsync();

            var tasks = await _context.TaskItems
                .Include(t => t.AssignedToUser)
                .Where(t => checklistIds.Contains(t.ChecklistId)
                            && !t.IsDeleted
                            && !string.IsNullOrEmpty(t.AssignedToUserId))
                .ToListAsync();

            var result = tasks
                .GroupBy(t => new
                {
                    t.AssignedToUserId,
                    Email = t.AssignedToUser != null ? t.AssignedToUser.Email : "",
                    FullName = t.AssignedToUser != null ? t.AssignedToUser.FullName : ""
                })
                .Select(g =>
                {
                    var total = g.Count();
                    var completed = g.Count(x => x.Status == "Hoàn thành");
                    var overdue = g.Count(x =>
                        x.Deadline.HasValue &&
                        x.Deadline.Value < DateTime.Now &&
                        x.Status != "Hoàn thành");

                    return new MemberPerformanceViewModel
                    {
                        UserId = g.Key.AssignedToUserId ?? "",
                        Email = g.Key.Email ?? "",
                        FullName = g.Key.FullName,
                        TotalTasks = total,
                        CompletedTasks = completed,
                        OverdueTasks = overdue,
                        CompletionRate = total == 0 ? 0 : Math.Round((double)completed * 100 / total, 1)
                    };
                })
                .OrderByDescending(x => x.OverdueTasks)
                .ThenByDescending(x => x.TotalTasks)
                .ToList();

            return result;
        }

        public async Task<List<AiInsightViewModel>> GenerateProjectManagerInsightsAsync(string managerId)
        {
            var projects = await _context.Projects
                .Where(p => p.ManagerId == managerId && !p.IsDeleted)
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

            var insights = new List<AiInsightViewModel>();
            var now = DateTime.Now;
            var nextThreeDays = now.AddDays(3);

            var overdueTasks = tasks.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value < now &&
                t.Status != "Hoàn thành");

            var dueSoonTasks = tasks.Count(t =>
                t.Deadline.HasValue &&
                t.Deadline.Value >= now &&
                t.Deadline.Value <= nextThreeDays &&
                t.Status != "Hoàn thành");

            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == "Hoàn thành");
            var completionRate = totalTasks == 0 ? 0 : Math.Round((double)completedTasks * 100 / totalTasks, 1);

            if (overdueTasks >= 3)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Risk",
                    Title = "Dự án đang có điểm nghẽn deadline",
                    Message = $"Hiện có {overdueTasks} công việc quá hạn. Manager nên ưu tiên xử lý các task đã quá deadline trước.",
                    Severity = "Danger",
                    Score = 0.95
                });
            }

            if (dueSoonTasks >= 3)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Reminder",
                    Title = "Khối lượng việc sắp đến hạn khá nhiều",
                    Message = $"Có {dueSoonTasks} công việc sẽ đến hạn trong 3 ngày tới. Nên rà lại phân công để tránh bị dồn vào cuối kỳ.",
                    Severity = "Warning",
                    Score = 0.84
                });
            }

            if (completionRate >= 70)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Strength",
                    Title = "Tiến độ chung của nhóm khá ổn",
                    Message = $"Tỷ lệ hoàn thành hiện đang ở mức {completionRate:0.#}%. Nhóm đang bám tiến độ tương đối tốt.",
                    Severity = "Success",
                    Score = 0.82
                });
            }

            var riskChecklist = checklists
                .Select(c =>
                {
                    var checklistTasks = tasks.Where(t => t.ChecklistId == c.ChecklistId).ToList();
                    var checklistOverdue = checklistTasks.Count(t =>
                        t.Deadline.HasValue &&
                        t.Deadline.Value < now &&
                        t.Status != "Hoàn thành");

                    return new
                    {
                        c.Title,
                        Overdue = checklistOverdue,
                        Total = checklistTasks.Count
                    };
                })
                .Where(x => x.Overdue > 0)
                .OrderByDescending(x => x.Overdue)
                .FirstOrDefault();

            if (riskChecklist != null)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Focus",
                    Title = "Checklist cần theo dõi sát",
                    Message = $"Checklist \"{riskChecklist.Title}\" đang có {riskChecklist.Overdue} công việc quá hạn, đây có thể là khu vực chậm nhất hiện tại.",
                    Severity = "Warning",
                    Score = 0.80
                });
            }

            var memberPerformances = await GenerateMemberPerformanceAsync(managerId);
            var overloadedMember = memberPerformances
                .OrderByDescending(x => x.OverdueTasks)
                .ThenByDescending(x => x.TotalTasks)
                .FirstOrDefault(x => x.TotalTasks > 0);

            if (overloadedMember != null && overloadedMember.OverdueTasks > 0)
            {
                insights.Add(new AiInsightViewModel
                {
                    Type = "Risk",
                    Title = "Có thành viên đang chịu áp lực tiến độ",
                    Message = $"{overloadedMember.FullName ?? overloadedMember.Email} hiện có {overloadedMember.OverdueTasks} công việc quá hạn trên tổng {overloadedMember.TotalTasks} việc được giao.",
                    Severity = "Warning",
                    Score = 0.79
                });
            }

            return insights
                .OrderByDescending(x => x.Score)
                .Take(5)
                .ToList();
        }

        public async Task<string> GenerateProjectManagerSummaryAsync(string managerId)
        {
            var insights = await GenerateProjectManagerInsightsAsync(managerId);
            var memberPerformances = await GenerateMemberPerformanceAsync(managerId);

            if (!insights.Any())
            {
                return "Hiện tại chưa có tín hiệu rủi ro nổi bật. Bạn có thể tiếp tục theo dõi tiến độ và giữ nhịp phân công như hiện tại.";
            }

            var topRisk = insights.FirstOrDefault(x => x.Severity == "Danger" || x.Severity == "Warning");
            var bestMember = memberPerformances
                .Where(x => x.TotalTasks > 0)
                .OrderByDescending(x => x.CompletionRate)
                .ThenBy(x => x.OverdueTasks)
                .FirstOrDefault();

            if (topRisk != null && bestMember != null)
            {
                return $"Tổng quan hiện tại cho thấy nhóm vẫn đang tiến triển, nhưng cần chú ý: {topRisk.Message} Đồng thời, {bestMember.FullName ?? bestMember.Email} đang là thành viên có tỷ lệ hoàn thành tốt nhất ở thời điểm này.";
            }

            return insights[0].Message;
        }
        public async Task<string> GeneratePersonalProgressCommentAsync(string userId)
        {
            var insights = await GenerateTeamMemberInsightsAsync(userId);

            if (!insights.Any())
            {
                return "Tiến độ của bạn hiện khá ổn định. Chỉ cần tiếp tục giữ nhịp làm việc và bám deadline là rất tốt.";
            }

            var topInsight = insights
                .OrderByDescending(x => x.Score)
                .First();

            return topInsight.Message;
        }
        public async Task<List<string>> GenerateManagerSuggestedActionsAsync(string managerId)
        {
            var actions = new List<string>();
            var insights = await GenerateProjectManagerInsightsAsync(managerId);

            foreach (var insight in insights)
            {
                if (insight.Type == "Risk")
                {
                    actions.Add("Ưu tiên rà soát các task quá hạn và xác nhận nguyên nhân chậm tiến độ.");
                }
                else if (insight.Type == "Reminder")
                {
                    actions.Add("Kiểm tra lại phân công các việc sắp đến hạn trong 3 ngày tới.");
                }
                else if (insight.Type == "Focus")
                {
                    actions.Add("Theo dõi checklist đang có dấu hiệu nghẽn để xử lý sớm.");
                }
            }

            if (!actions.Any())
            {
                actions.Add("Tiếp tục duy trì nhịp theo dõi hiện tại, chưa có rủi ro nổi bật.");
            }

            return actions.Distinct().Take(3).ToList();
        }
    }
}