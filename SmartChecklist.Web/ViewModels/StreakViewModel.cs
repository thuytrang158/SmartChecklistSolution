namespace SmartChecklist.Web.ViewModels
{
    public class StreakViewModel
    {
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public List<DateTime> CompletionDays { get; set; } = new();
    }
}