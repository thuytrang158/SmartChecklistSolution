namespace SmartChecklist.Web.ViewModels
{
    public class ProjectManagerDashboardViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalChecklists { get; set; }
        public int TotalTasks { get; set; }
        public int OverdueTasks { get; set; }

        public string AiSummary { get; set; } = string.Empty;

        public string AssistantTitle { get; set; } = "Trợ lý quản lý thông minh";
        public List<string> SuggestedActions { get; set; } = new();

        public List<AiInsightViewModel> AiInsights { get; set; } = new();
        public List<ProjectProgressItemViewModel> ProjectProgresses { get; set; } = new();
        public List<DashboardTaskAlertViewModel> UpcomingTasks { get; set; } = new();
        public List<MemberPerformanceViewModel> MemberPerformances { get; set; } = new();
    }

    public class ProjectProgressItemViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercent { get; set; }
    }

    public class DashboardTaskAlertViewModel
    {
        public int TaskItemId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string? AssignedToName { get; set; }
        public DateTime? Deadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
    }
}