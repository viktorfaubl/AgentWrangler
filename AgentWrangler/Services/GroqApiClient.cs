using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using AgentWrangler.Services;

namespace AgentWrangler.Services
{
    public class GroqApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;

        public GroqApiClient(string apiKey, string endpoint = "https://api.groq.com/openai/v1/chat/completions", string model = "compound-beta-mini")
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
                Logger.LogError(e, "GroqApiClient.SendOcrResultAsync");
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

        /// <summary>
        /// Performs OCR by sending an image to Groq's chat completion endpoint.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <param name="question">Question to ask about the image (e.g., "What's in this image?").</param>
        /// <param name="model">Model to use (default: meta-llama/llama-4-scout-17b-16e-instruct).</param>
        /// <returns>LLM response about the image.</returns>
        public async Task<string?> OcrImageAsync(string imagePath, string question = "OCR", string? model = null)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            question =
                "OCR this image, if the image contains multiple parts, OCR every part. Return only the OCR result, nothing else";
            string base64Image = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));
            string imageMimeType = "image/jpeg"; // You may want to detect MIME type from extension
            string imageDataUrl = $"data:{imageMimeType};base64,{base64Image}";

            var payload = new
            {
                model = model ?? "meta-llama/llama-4-maverick-17b-128e-instruct",
                messages = new[]
                {
                    new {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = question },
                            new {
                                type = "image_url",
                                image_url = new {
                                    url = imageDataUrl
                                }
                            }
                        }
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
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
                Logger.LogError(e, "GroqApiClient.OcrImageAsync");
                return null;
            }
        }

        /// <summary>
        /// Transcribes an audio file to text using Groq's Whisper endpoint.
        /// </summary>
        /// <param name="audioPath">Path to the audio file.</param>
        /// <param name="model">Model to use (default: whisper-large-v3-turbo).</param>
        /// <param name="language">Language code (default: en).</param>
        /// <param name="temperature">Temperature (default: 0).</param>
        /// <returns>Transcription result as JSON string, or null if failed.</returns>
        public async Task<string?> TranscribeAudioAsync(string audioPath, string? model = null, string language = "en", double temperature = 0)
        {
            if (!File.Exists(audioPath))
                throw new FileNotFoundException($"Audio file not found: {audioPath}");

            var endpoint = "https://api.groq.com/openai/v1/audio/transcriptions";
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("whisper-large-v3-turbo"), "model");
            form.Add(new StringContent(temperature.ToString()), "temperature");
            form.Add(new StringContent("verbose_json"), "response_format");
            //form.Add(new StringContent("[\"word\"]"), "timestamp_granularities");
            form.Add(new StringContent(language), "language");
            var audioBytes = await File.ReadAllBytesAsync(audioPath);
            var audioContent = new ByteArrayContent(audioBytes);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(audioContent, "file", Path.GetFileName(audioPath));

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = form;
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return ExtractTranscribedText(body);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "GroqApiClient.TranscribeAudioAsync");
                return null;
            }
        }

        /// <summary>
        /// Extracts only the transcribed text from the Groq transcription JSON response.
        /// </summary>
        /// <param name="jsonResponse">The JSON response string from Groq transcription.</param>
        /// <returns>The transcribed text, or null if not found.</returns>
        public static string? ExtractTranscribedText(string jsonResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "GroqApiClient.ExtractTranscribedText");
            }
            return null;
        }
    }
}
