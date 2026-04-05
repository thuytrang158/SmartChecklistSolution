using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartChecklist.Web.Services
{
    public class OpenAiTextGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenAiTextGenerationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GenerateTextAsync(string prompt)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4.1-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "Chưa cấu hình OpenAI API key.";
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = model,
                input = prompt
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/responses", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Lỗi gọi OpenAI API: {response.StatusCode} - {responseText}";
            }

            using var doc = JsonDocument.Parse(responseText);

            if (doc.RootElement.TryGetProperty("output", out var outputElement))
            {
                foreach (var item in outputElement.EnumerateArray())
                {
                    if (item.TryGetProperty("content", out var contentArray))
                    {
                        foreach (var part in contentArray.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var textElement))
                            {
                                return textElement.GetString() ?? "AI không trả về nội dung.";
                            }
                        }
                    }
                }
            }

            return "AI không trả về nội dung hợp lệ.";
        }
    }
}