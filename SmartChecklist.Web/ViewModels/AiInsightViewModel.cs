namespace SmartChecklist.Web.ViewModels
{
    public class AiInsightViewModel
    {
        public string Type { get; set; } = string.Empty;      // Risk, Strength, Focus, Reminder
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info";        // Info, Warning, Danger, Success
        public double Score { get; set; }
    }
}