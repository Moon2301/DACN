namespace DACN.Dtos.Ollama
{

    public class OllamaRequest
    {
        public string Model { get; set; } = "llama3.1:8b";
        public string Prompt { get; set; }
        public bool Stream { get; set; } = false;
        public float Temperature { get; set; } = 0.0f;

        public string Format { get; set; } = "json";
    }

    public class OllamaResponse
    {
        public string Model { get; set; }
        public string CreatedAt { get; set; }
        public string Response { get; set; } // Phản hồi văn bản từ mô hình (ví dụ: "safe", "unsafe")
        public bool Done { get; set; }

    }
}
