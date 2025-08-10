using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentWrangler.Services
{
    public class GroqApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;

        public GroqApiClient(string apiKey, string endpoint = "https://api.groq.com/openai/v1/chat/completions", string model = "moonshotai/kimi-k2-instruct")
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _endpoint = endpoint;
            _model = model;
        }

        public async Task<string?> SendOcrResultAsync(string ocrText, string language)
        {
            string promptJson = BuildGroqPromptJson(ocrText, language);
            var content = new StringContent(promptJson, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Content = content;
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            try
            {
                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var llmResponse = choices[0].GetProperty("message").GetProperty("content").GetString();
                    return llmResponse;
                }
                return null;
            }
            catch (Exception e)
            {
                // Log or handle error as needed
                return null;
            }
        }

        public string BuildGroqPromptJson(string userInput, string language)
        {
            var payload = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = $"You are a senior {language} developer. You are on an interview. Describe the given problem. Solve the given problem and explain the solution. Comment the code with the explanation. If you are given code, analyze it, run a practical code review and produce a clear, actionable report." },
                    new { role = "user", content = userInput }
                },
                temperature = 0.5
            };
            return JsonSerializer.Serialize(payload);
        }
    }
}
