namespace SmartChecklist.Web.Services
{
    public class AiTextStylingService
    {
        private static readonly List<string> WarmOpeners = new()
        {
            "Mình nhắc nhẹ bạn một chút nhé,",
            "Có một việc bạn nên ưu tiên nè,",
            "Hôm nay mình thấy bạn nên chú ý một chút đến việc này,",
            "Nhắc khẽ thôi nha,",
            "Mình gợi ý nhẹ cho bạn nè,"
        };

        private static readonly List<string> EncouragingClosers = new()
        {
            "Làm sớm một chút sẽ đỡ áp lực hơn nhiều đó.",
            "Xử lý trước phần này thì nhịp làm việc sẽ nhẹ hơn nha.",
            "Chốt xong việc này trước là bạn sẽ thoải mái hơn đó.",
            "Bạn làm từng bước thôi là ổn lắm.",
            "Giữ nhịp đều như vậy là rất tốt rồi."
        };

        private readonly Random _random = new();

        public string BuildFriendlyMessage(string coreMessage)
        {
            var opener = WarmOpeners[_random.Next(WarmOpeners.Count)];
            var closer = EncouragingClosers[_random.Next(EncouragingClosers.Count)];

            return $"{opener} {coreMessage} {closer}";
        }

        public string BuildManagerSummary(string coreMessage)
        {
            return $"Gợi ý từ trợ lý thông minh: {coreMessage}";
        }
    }
}