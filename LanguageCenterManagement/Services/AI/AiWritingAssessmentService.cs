using System.Net.Http.Json;
using System.Text.Json;
using LanguageCenterManagement.ViewModels;

namespace LanguageCenterManagement.Services.Ai
{
    public class AiWritingAssessmentService
    {
        private readonly HttpClient _httpClient;

        public AiWritingAssessmentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<WritingAssessmentResultViewModel> AssessAsync(
            string taskPrompt,
            string studentWriting,
            CancellationToken cancellationToken = default)
        {
            var prompt = $$"""
                You are an English writing tutor and assessment assistant.

                Assess the student's writing against this task:
                {{taskPrompt}}

                Student's writing:
                {{studentWriting}}

                Score exactly four criteria, each from 0 to 5:
                1. Task Achievement and Relevance
                2. Organization and Coherence
                3. Vocabulary
                4. Grammar and Sentence Structure

                TotalScore must equal the sum of the four criterion scores.
                MaxScore must be 20.
                Do not invent mistakes. Quote actual phrases when correcting.
                Suggestions must be actionable.
                ImprovedVersion must preserve the student's meaning.
                If there are no errors, do not invent any.

                Return only valid JSON with this structure:
                {
                  "totalScore": 0,
                  "maxScore": 20,
                  "overallFeedback": "Overall feedback",
                  "criteria": [
                    {
                      "name": "Task Achievement and Relevance",
                      "score": 0,
                      "maxScore": 5,
                      "feedback": "Feedback"
                    },
                    {
                      "name": "Organization and Coherence",
                      "score": 0,
                      "maxScore": 5,
                      "feedback": "Feedback"
                    },
                    {
                      "name": "Vocabulary",
                      "score": 0,
                      "maxScore": 5,
                      "feedback": "Feedback"
                    },
                    {
                      "name": "Grammar and Sentence Structure",
                      "score": 0,
                      "maxScore": 5,
                      "feedback": "Feedback"
                    }
                  ],
                  "corrections": [
                    {
                      "original": "Exact original phrase",
                      "corrected": "Corrected phrase",
                      "explanation": "Reason"
                    }
                  ],
                  "suggestions": ["Actionable suggestion"],
                  "improvedVersion": "Revised writing"
                }
                """;

            var request = new
            {
                model = "qwen2.5:3b",
                prompt,
                stream = false,
                format = "json"
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/generate", request, cancellationToken);

            response.EnsureSuccessStatusCode();

            var ollamaResponse =
                await response.Content.ReadFromJsonAsync<OllamaResponse>(
                    cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(ollamaResponse?.Response))
            {
                throw new InvalidOperationException(
                    "Ollama returned an empty writing assessment.");
            }

            using var document = JsonDocument.Parse(ollamaResponse.Response);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    "Ollama returned an invalid JSON object.");
            }

            var normalizedCorrections = new List<string>();

            if (root.TryGetProperty("corrections", out var corrections) &&
                corrections.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in corrections.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var text = item.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                            normalizedCorrections.Add(text);
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        string GetText(string name) =>
                            item.TryGetProperty(name, out var value) &&
                            value.ValueKind == JsonValueKind.String
                                ? value.GetString() ?? string.Empty
                                : string.Empty;

                        var original = GetText("original");
                        var corrected = GetText("corrected");
                        var explanation = GetText("explanation");

                        var correction = string.IsNullOrWhiteSpace(original)
                            ? corrected
                            : $"{original} -> {corrected}";

                        if (!string.IsNullOrWhiteSpace(explanation))
                            correction += $" ({explanation})";

                        if (!string.IsNullOrWhiteSpace(correction))
                            normalizedCorrections.Add(correction);
                    }
                }
            }

            using var normalizedStream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(normalizedStream))
            {
                writer.WriteStartObject();

                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name.Equals(
                        "corrections", StringComparison.OrdinalIgnoreCase))
                    {
                        writer.WritePropertyName("corrections");
                        JsonSerializer.Serialize(writer, normalizedCorrections);
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            var normalizedJson =
                System.Text.Encoding.UTF8.GetString(normalizedStream.ToArray());

            var result = JsonSerializer.Deserialize<WritingAssessmentResultViewModel>(
                normalizedJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result?.Criteria == null || result.Criteria.Count != 4)
            {
                throw new InvalidOperationException(
                    "Ollama returned an invalid writing assessment.");
            }

            foreach (var criterion in result.Criteria)
            {
                criterion.Score = Math.Clamp(criterion.Score, 0, 5);
                criterion.MaxScore = 5;
            }

            result.TotalScore = result.Criteria.Sum(c => c.Score);
            result.MaxScore = 20;
            result.OverallFeedback ??= string.Empty;
            result.Corrections ??= new List<string>();
            result.Suggestions ??= new List<string>();
            result.ImprovedVersion ??= string.Empty;

            return result;
        }


        private class OllamaResponse
        {
            public string? Response { get; set; }
        }
    }
}