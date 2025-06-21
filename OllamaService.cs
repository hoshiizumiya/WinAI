using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AIAppT
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _modelName;

        public OllamaService(string baseUrl = "http://localhost:11434", string modelName = "llama2")
        {
            _baseUrl = baseUrl;
            _modelName = modelName;
            _httpClient = new HttpClient();
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            var request = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                return result?.Response ?? "无响应";
            }
            catch (Exception ex)
            {
                return $"发生错误: {ex.Message}";
            }
        }

        public async Task<bool> IsServerAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/version");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<string[]> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<OllamaModelsResponse>();
                return result?.Models?.Select(m => m.Name)?.ToArray() ?? Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }

    public class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
        
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }
    
    public class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel>? Models { get; set; }
    }
    
    public class OllamaModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}