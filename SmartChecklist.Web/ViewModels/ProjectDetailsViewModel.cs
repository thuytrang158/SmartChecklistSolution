using SmartChecklist.Web.Models;

namespace SmartChecklist.Web.ViewModels
{
    public class ProjectDetailsViewModel
    {
        public Project Project { get; set; } = new Project();

        public int TotalChecklists { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int ActiveMembers { get; set; }
        public int ProgressPercent { get; set; }
    }
}