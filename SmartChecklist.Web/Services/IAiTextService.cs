namespace SmartChecklist.Web.Services
{
    public interface IAiTextService
    {
        Task<string> GenerateTextAsync(string prompt);
    }
}