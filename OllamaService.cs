using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
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
            //1.	使用下划线 (_) 作为私有字段（private fields）的前缀
            //2.能够清晰区分局部变量和类字段
            //3.遵循驼峰命名法（首字母小写，后续单词首字母大写）
            _baseUrl = baseUrl;
            _modelName = modelName;
            _httpClient = new HttpClient();
        }

        //异步方法，用于向 Ollama 服务发送提示(prompt)并获取生成的响应。它接收一个提示字符串作为输入，然后返回模型生成的文本响应
        //返回一个代表异步操作的 Task，该操作完成后将产生一个字符串结果
        public async Task<string> GenerateResponseAsync(string prompt)
        {
            //使用匿名类对象创建 API 请求体，无需预先定义。包含模型名称、提示文本和是否流式传输的标志
            var request = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false
            };

            //序列化请求体并创建 HTTP 内容，使用 JsonSerializer.Serialize 将请求对象转换为 JSON 字符串
            //创建 StringContent 对象，将 JSON 作为内容，指定 UTF-8 编码和 JSON MIME 类型

            var content = new StringContent(
                JsonSerializer.Serialize(request),//在这个方法中，options 参数是可选的（带有默认值 null），所以可以只传入第一个参数而忽略第二个参数。返回 WriteString<TValue>
                Encoding.UTF8,
                "application/json");

            Trace.WriteLine(content);

            //发送 HTTP 请求并处理响应。
            //使用 $ 前缀和 { }插入变量值
            //使用 await 等待异步 POST 请求完成，请求发送到 /api/generate 端点
            //EnsureSuccessStatusCode() 确保响应状态码表示成功，如果不是则抛出异常
            //使用 ReadFromJsonAsync<OllamaResponse>() 将响应内容反序列化为 OllamaResponse 对象
            //返回响应文本，如果为空则返回 "无响应"（使用空合并操作符 ??）
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                return result?.Response ?? "无响应";   //?. 是空条件运算符，如果 result 为 null，整个表达式返回 null
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