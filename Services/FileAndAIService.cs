using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TravelBlog.Services
{
    // ──────────────────────────────────────────────────────────
    // File Upload
    // ──────────────────────────────────────────────────────────
    public interface IFileUploadService
    {
        Task<string> UploadAsync(IFormFile file, string folder);
        void DeleteFile(string relativePath);
        bool IsValidImage(IFormFile file);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly string[] _allowedExt;
        private readonly long _maxBytes;

        public FileUploadService(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
            _allowedExt = config.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? [".jpg", ".jpeg", ".png", ".gif", ".webp"];
            _maxBytes = (config.GetValue<int>("FileUpload:MaxFileSizeMB", 5)) * 1024L * 1024L;
        }

        public async Task<string> UploadAsync(IFormFile file, string folder)
        {
            if (!IsValidImage(file)) throw new InvalidOperationException("Invalid file type or size.");

            var uploadsPath = Path.Combine(_env.WebRootPath, "images", "uploads", folder);
            Directory.CreateDirectory(uploadsPath);

            var ext = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/uploads/{folder}/{fileName}";
        }

        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        public bool IsValidImage(IFormFile file)
            => file.Length > 0 && file.Length <= _maxBytes
               && _allowedExt.Contains(Path.GetExtension(file.FileName).ToLower());
    }

    // ──────────────────────────────────────────────────────────
    // AI Chatbot (OpenAI)
    // ──────────────────────────────────────────────────────────
    public interface IAIService
    {
        Task<string> GetBlogIdeasAsync(string destination, string? topic = null);
        Task<string> GetContentOutlineAsync(string title, string destination);
        Task<string> GetWritingAssistanceAsync(string prompt);
    }

    public class AIService : IAIService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;

        public AIService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["AISettings:OpenAIApiKey"] ?? "";
            _model = config["AISettings:Model"] ?? "gpt-4o-mini";
        }

        public async Task<string> GetBlogIdeasAsync(string destination, string? topic = null)
        {
            var prompt = string.IsNullOrEmpty(topic)
                ? $"Generate 5 creative travel blog post ideas for {destination}. Return as a numbered list with title and one-sentence description."
                : $"Generate 5 creative travel blog post ideas about '{topic}' in {destination}. Return as a numbered list.";
            return await CallOpenAIAsync(prompt, "You are a creative travel blog writing assistant.");
        }

        public async Task<string> GetContentOutlineAsync(string title, string destination)
        {
            var prompt = $"Create a detailed blog post outline for: '{title}' (travel destination: {destination}). Include intro, 4-5 main sections with subpoints, and conclusion.";
            return await CallOpenAIAsync(prompt, "You are an expert travel writer helping structure blog posts.");
        }

        public async Task<string> GetWritingAssistanceAsync(string prompt)
            => await CallOpenAIAsync(prompt, "You are a helpful travel blog writing assistant. Be creative, descriptive, and engaging.");

        private async Task<string> CallOpenAIAsync(string userMessage, string systemMessage)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY_HERE")
                return "🤖 AI features require an OpenAI API key. Please configure AISettings:OpenAIApiKey in appsettings.json.";

            var payload = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userMessage }
                },
                max_tokens = 800,
                temperature = 0.7
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No response from AI.";
        }
    }
}
