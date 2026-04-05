namespace SmartChecklist.Web.ViewModels
{
    public class DashboardReportViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalChecklists { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }

        public double CompletionRate { get; set; }

        public string AiSummary { get; set; } = string.Empty;

        public List<AiInsightViewModel> AiInsights { get; set; } = new();
        public List<MemberPerformanceViewModel> MemberPerformances { get; set; } = new();
    }
}