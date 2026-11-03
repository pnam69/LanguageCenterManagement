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
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentException("Topic cannot be empty.", nameof(topic));

            if (string.IsNullOrWhiteSpace(level))
                throw new ArgumentException("English level cannot be empty.", nameof(level));

            if (wordCount < 50 || wordCount > 2000)
                throw new ArgumentOutOfRangeException(
                    nameof(wordCount),
                    "Word count must be between 50 and 2000.");

            var prompt = $"""
        You are an experienced English language teacher.

        Write one English reading passage with these settings:
        Topic: {topic.Trim()}
        English proficiency level: {level.Trim()}
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

            try
            {
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException(
                    "The reading passage generation request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    "Could not connect to Ollama. Check that Ollama is running and qwen2.5:3b is available.",
                    ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "Ollama returned an invalid reading passage response.", ex);
            }
        }


        private sealed class OllamaResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }
    }
}
