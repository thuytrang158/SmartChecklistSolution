using Microsoft.EntityFrameworkCore;
using SmartChecklist.Web.Data;
using SmartChecklist.Web.Models;

namespace SmartChecklist.Web.Services
{
    public class AiSuggestionService
    {
        private readonly ApplicationDbContext _context;
        private readonly AiTextStylingService _aiTextStylingService;

        public AiSuggestionService(ApplicationDbContext context, AiTextStylingService aiTextStylingService)
        {
            _context = context;
            _aiTextStylingService = aiTextStylingService;
        }

        public async Task<List<string>> SuggestChecklistTitlesAsync(string userId, int projectId)
        {
            return await _context.Checklists
                .Where(c => c.ProjectId == projectId && !c.IsDeleted && c.CreatedBy == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.Title)
                .Distinct()
                .Take(5)
                .ToListAsync();
        }

        public async Task<List<string>> SuggestWorkTypesAsync(string userId, int projectId)
        {
            return await _context.Checklists
                .Where(c => c.ProjectId == projectId && !c.IsDeleted && c.CreatedBy == userId && c.WorkType != null)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.WorkType!)
                .Distinct()
                .Take(5)
                .ToListAsync();
        }

        public async Task<List<string>> SuggestTaskTitlesAsync(int checklistId)
        {
            var checklist = await _context.Checklists.FirstOrDefaultAsync(c => c.ChecklistId == checklistId);
            if (checklist == null) return new List<string>();

            return await _context.TaskItems
                .Where(t => t.ChecklistId == checklistId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => t.Title)
                .Distinct()
                .Take(5)
                .ToListAsync();
        }

        public async Task<string> SuggestPriorityAsync(string? assignedUserId)
        {
            if (string.IsNullOrEmpty(assignedUserId))
                return "Trung bình";

            var recentTasks = await _context.TaskItems
                .Where(t => t.AssignedToUserId == assignedUserId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();

            if (!recentTasks.Any()) return "Trung bình";

            var highCount = recentTasks.Count(t => t.Priority == "Cao" || t.Priority == "Khẩn cấp");

            return highCount >= recentTasks.Count / 2 ? "Cao" : "Trung bình";
        }

        public async Task<DateTime?> SuggestDeadlineAsync(string? assignedUserId)
        {
            if (string.IsNullOrEmpty(assignedUserId))
                return DateTime.Now.AddDays(3);

            var completedTasks = await _context.TaskItems
                .Where(t => t.AssignedToUserId == assignedUserId
                            && t.CompletedAt != null
                            && t.CreatedAt != default
                            && !t.IsDeleted)
                .OrderByDescending(t => t.CompletedAt)
                .Take(20)
                .ToListAsync();

            if (!completedTasks.Any()) return DateTime.Now.AddDays(3);

            var avgHours = completedTasks
                .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours)
                .Where(h => h > 0)
                .DefaultIfEmpty(72)
                .Average();

            return DateTime.Now.AddHours(Math.Max(24, avgHours));
        }

        public async Task<string> GenerateFriendlyReminderAsync(string userId)
        {
            var now = DateTime.Now;
            var nextThreeDays = now.AddDays(3);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            var tasks = await _context.TaskItems
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
                .ToListAsync();

            var name = user?.DisplayName ?? user?.FullName ?? "bạn";

            if (!tasks.Any())
            {
                var message = $"Hôm nay trông khá nhẹ nhàng đó {name} ơi, hiện chưa có công việc nào cần nhắc gấp. Giữ phong độ này nhé.";
                await SaveSuggestionHistoryAsync(userId, "FriendlyReminder", "NoTasks", message);
                return message;
            }

            var overdueTask = tasks.FirstOrDefault(t => t.Deadline.HasValue && t.Deadline.Value < now);
            if (overdueTask != null)
            {
                var message = $"{name} ơi, mình nhắc nhẹ là việc \"{overdueTask.Title}\" đang quá hạn rồi đó. Xử lý việc này trước sẽ giúp bạn đỡ bị dồn áp lực hơn nhiều.";
                await SaveSuggestionHistoryAsync(userId, "FriendlyReminder", overdueTask.Title, message);
                return message;
            }

            var dueSoonTask = tasks.FirstOrDefault(t => t.Deadline.HasValue && t.Deadline.Value <= nextThreeDays);
            if (dueSoonTask != null)
            {
                var daysLeft = (dueSoonTask.Deadline!.Value.Date - now.Date).Days;
                string message;

                if (daysLeft <= 0)
                {
                    message = $"{name} ơi, việc \"{dueSoonTask.Title}\" đến hạn trong hôm nay rồi nè. Chốt sớm một chút để phần còn lại thoải mái hơn nhé.";
                }
                else
                {
                    message = $"{name} ơi, việc \"{dueSoonTask.Title}\" còn khoảng {daysLeft} ngày nữa là đến hạn. Làm trước từ bây giờ sẽ đỡ bị dí vào phút cuối đó.";
                }

                await SaveSuggestionHistoryAsync(userId, "FriendlyReminder", dueSoonTask.Title, message);
                return message;
            }

            var highPriorityTask = tasks.FirstOrDefault(t => t.Priority == "Khẩn cấp" || t.Priority == "Cao");
            if (highPriorityTask != null)
            {
                var message = $"{name} nè, hôm nay bạn nên ưu tiên \"{highPriorityTask.Title}\" trước vì đây là công việc mức ưu tiên cao. Xử lý xong việc này là nhịp làm việc sẽ nhẹ hơn đó.";
                await SaveSuggestionHistoryAsync(userId, "FriendlyReminder", highPriorityTask.Title, message);
                return message;
            }

            var finalMessage = $"{name} ơi, tiến độ hiện tại khá ổn rồi. Bạn cứ xử lý lần lượt từng việc đang mở là đẹp lắm.";
            await SaveSuggestionHistoryAsync(userId, "FriendlyReminder", "GeneralProgress", finalMessage);
            return finalMessage;
        }

        public async Task<string> GenerateDailyFocusSuggestionAsync(string userId)
        {
            var now = DateTime.Now;

            var task = await _context.TaskItems
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
                .FirstOrDefaultAsync();

            if (task == null)
            {
                var message = "Hôm nay chưa có đầu việc nào nổi bật cần ưu tiên, bạn có thể tranh thủ rà soát lại các phần đã hoàn thành.";
                await SaveSuggestionHistoryAsync(userId, "DailyFocus", "NoTask", message);
                return message;
            }

            if (task.Deadline.HasValue && task.Deadline.Value < now)
            {
                var message = $"Ưu tiên số 1 hôm nay là \"{task.Title}\" vì công việc này đã quá hạn và cần được xử lý trước.";
                await SaveSuggestionHistoryAsync(userId, "DailyFocus", task.Title, message);
                return message;
            }

            if (task.Deadline.HasValue)
            {
                var message = $"Bạn nên bắt đầu với \"{task.Title}\" trước vì deadline đang gần hơn các việc còn lại.";
                await SaveSuggestionHistoryAsync(userId, "DailyFocus", task.Title, message);
                return message;
            }

            if (task.Priority == "Khẩn cấp" || task.Priority == "Cao")
            {
                var message = $"Bạn nên xử lý \"{task.Title}\" trước vì đây là công việc có mức ưu tiên cao.";
                await SaveSuggestionHistoryAsync(userId, "DailyFocus", task.Title, message);
                return message;
            }

            var finalMessage = $"Bạn có thể bắt đầu với \"{task.Title}\" để giữ nhịp công việc ổn định trong hôm nay.";
            await SaveSuggestionHistoryAsync(userId, "DailyFocus", task.Title, finalMessage);
            return finalMessage;
        }

        public async Task SaveSuggestionHistoryAsync(string userId, string suggestionType, string? inputContext, string outputSuggestion)
        {
            var history = new AiSuggestionHistory
            {
                UserId = userId,
                SuggestionType = suggestionType,
                InputContext = inputContext,
                OutputSuggestion = outputSuggestion,
                Accepted = false,
                CreatedAt = DateTime.Now
            };

            _context.AiSuggestionHistories.Add(history);
            await _context.SaveChangesAsync();
        }
    }
}