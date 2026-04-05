namespace SmartChecklist.Web.ViewModels
{
    public class ChecklistProgressViewModel
    {
        public int ChecklistId { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? WorkType { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double ProgressPercent { get; set; }
    }
}