using DACN.Dtos; 
using DACN.Dtos.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DACN.Services
{
    // Triển khai Service
    public class OllamaModerationService : IContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaModerationService> _logger;
        private readonly string _ollamaApiUrl;

        public OllamaModerationService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaModerationService> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
            // Lấy URL 
            _ollamaApiUrl = configuration["Ollama:ApiUrl"] ?? "http://localhost:11434/api/generate";
        }

        public async Task<bool> IsContentSafe(string content)
        {
            var moderationPrompt = $"User: {content}";

            var requestDto = new OllamaRequest
            {
                Model = "llama-guard3:8b",
                Prompt = moderationPrompt,
                Stream = false,
                Temperature = 0.0f
            };

            var requestJson = new StringContent(
                JsonSerializer.Serialize(requestDto),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsync(_ollamaApiUrl, requestJson);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var classification = ollamaResponse?.Response?.Trim().ToLower();

                if (classification == null || classification.Contains("unsafe"))
                {
                    _logger.LogWarning($"Content blocked by Llama-Guard3: {content.Substring(0, Math.Min(content.Length, 100))}...");
                    // Nếu là "unsafe" hoặc không có phản hồi, chặn
                    return false;
                }

                // Nếu là "safe" (như kết quả test của bạn)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Ollama/Llama-Guard API.");
                // Chặn nội dung nếu API gặp lỗi
                return false;
            }
        }
    }
}