
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanguageCenterManagement.Services.Ai
{
    public class ReadingGenerationService
    {
        private readonly HttpClient _httpClient;

        public ReadingGenerationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri("http://localhost:11434/");
        }

        public async Task<string> GeneratePassageAsync(
            string topic,
            string level,
            int wordCount,
            string? focus,
            CancellationToken cancellationToken = default)
        {
            var prompt = $"""
                You are an experienced English language teacher.

                Write one English reading passage with these settings:
                Topic: {topic}
                English proficiency level: {level}
                Target length: approximately {wordCount} words
                Vocabulary or grammar focus:
                {(string.IsNullOrWhiteSpace(focus) ? "None specified" : focus.Trim())}

                Requirements:
                - Use vocabulary and grammar appropriate for the stated level.
                - Write a coherent, natural passage focused on the topic.
                - Respect the requested length as closely as possible.
                - Do not include comprehension questions or answers.
                - Do not include a title, introduction, or teaching notes.
                - Return only the passage text.
                """;

            using var response = await _httpClient.PostAsJsonAsync(
                "api/generate",
                new
                {
                    model = "qwen2.5:3b",
                    prompt,
                    stream = false
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(result?.Response))
            {
                throw new InvalidOperationException(
                    "Ollama returned an empty passage.");
            }

            return result.Response.Trim();
        }

        private sealed class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }
    }
}
