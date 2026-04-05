namespace SmartChecklist.Web.ViewModels
{
    public class TeamMemberDashboardViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalAssignedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int DueSoonTasks { get; set; }

        public double CompletionRate { get; set; }

        public string FriendlyReminder { get; set; } = string.Empty;
        public string DailyFocusSuggestion { get; set; } = string.Empty;

        public string AssistantTitle { get; set; } = "Trợ lý công việc";
        public string AssistantMood { get; set; } = "Tập trung";

        public List<AiInsightViewModel> AiInsights { get; set; } = new();
        public List<TeamMemberTaskAlertViewModel> PriorityTasks { get; set; } = new();
        public List<TeamMemberProjectProgressViewModel> MyProjects { get; set; } = new();
    }

    public class TeamMemberTaskAlertViewModel
    {
        public int TaskItemId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ChecklistName { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
    }

    public class TeamMemberProjectProgressViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercent { get; set; }
    }
}