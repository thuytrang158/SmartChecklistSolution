using SmartChecklist.Web.Models;

namespace SmartChecklist.Web.ViewModels
{
    public class PersonalProgressViewModel
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int NotStartedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRate { get; set; }

        public List<TaskItem> RecentTasks { get; set; } = new();
        public string AiComment { get; set; } = string.Empty;
        public List<AiInsightViewModel> AiInsights { get; set; } = new();
    }
}