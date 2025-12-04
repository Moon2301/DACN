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
            // Lấy URL từ cấu hình hoặc mặc định
            _ollamaApiUrl = configuration["Ollama:ApiUrl"] ?? "http://localhost:11434/api/generate";
        }

        public async Task<bool> IsContentSafe(string content)
        {
            // Llama-Guard 3 hoạt động tốt nhất với định dạng [INST] [/INST] hoặc đơn giản như sau.
            // QUAN TRỌNG: Bạn nên đưa nội dung cần kiểm duyệt vào một wrapper (ví dụ: User: Nội dung) 
            // để mô hình hiểu rằng đây là một đoạn chat/nội dung cần kiểm duyệt.
            var moderationPrompt = $"User: {content}";

            var requestDto = new OllamaRequest
            {
                // Thay bằng tên model đã chạy thành công
                Model = "llama-guard3:8b",
                Prompt = moderationPrompt,
                Stream = false,
                Temperature = 0.0f
                // KHÔNG cần format: "json" vì mô hình trả về chuỗi đơn giản
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

                // 1. Phân tích phản hồi Ollama tổng thể (Chứa trường 'response')
                // OllamaResponse phải chứa trường 'response' (string)
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // 2. Lấy kết quả phân loại và kiểm tra
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
                // CHỌN CƠ CHẾ AN TOÀN: Chặn nội dung nếu API gặp lỗi
                return false;
            }
        }
    }
}