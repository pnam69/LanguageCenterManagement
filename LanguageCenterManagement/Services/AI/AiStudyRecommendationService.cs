using System.Net.Http.Json;
using System.Text.Json;
using LanguageCenterManagement.ViewModels;

namespace LanguageCenterManagement.Services.Ai
{
    public class AiStudyRecommendationService
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public AiStudyRecommendationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<AiStudyRecommendationViewModel> GenerateAsync(
            StudentAssessmentViewModel assessment,
            CancellationToken cancellationToken = default)
        {
            if (assessment == null)
                throw new ArgumentNullException(nameof(assessment));

            if (assessment.Skills == null)
                throw new InvalidOperationException(
                    "Student assessment does not contain skill statistics.");

            var skills = assessment.Skills
                .Select(s => new
                {
                    s.Skill,
                    s.GradedQuestions,
                    s.EarnedScore,
                    s.PossibleScore,
                    Percentage = s.PossibleScore > 0
                        ? s.Percentage
                        : (decimal?)null
                })
                .ToList();

            var statistics = new
            {
                assessment.CompletedExams,
                assessment.GradedAnswers,
                assessment.OverallPercentage,
                Skills = skills
            };

            string prompt = $$"""
        You are an English study coach.

        Use only the student statistics below to give practical,
        encouraging, specific study recommendations.

        Rules:
        - Do not invent results or claim the student has a weakness
          in a skill with no graded data.
        - A skill with zero graded questions is "not enough data",
          not a low score.
        - If there are few graded questions, mention that the
          assessment is preliminary.
        - Focus on at most two skills with the lowest measured
          percentages when enough data exists.
        - Give 3 concise, actionable recommendations.
        - Make the study plan realistic and specific.
        - Do not grade the student or invent additional statistics.
        - Return valid JSON only, using exactly these keys:
          "mainFocus", "recommendations", "studyPlan".
        - "recommendations" must be an array of strings.

        Student statistics:
        {{JsonSerializer.Serialize(statistics)}}
        """;

            var request = new
            {
                model = "qwen2.5:3b",
                prompt,
                stream = false,
                format = "json",
                options = new
                {
                    temperature = 0.3
                }
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    "api/generate",
                    request,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var ollamaResult =
                    await response.Content.ReadFromJsonAsync<OllamaResponse>(
                        JsonOptions,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(ollamaResult?.Response))
                {
                    throw new InvalidOperationException(
                        "Ollama returned an empty recommendation.");
                }

                using var document = JsonDocument.Parse(ollamaResult.Response);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        "Ollama returned an invalid recommendation JSON object.");
                }

                string ReadText(JsonElement element)
                {
                    return element.ValueKind switch
                    {
                        JsonValueKind.String => element.GetString() ?? string.Empty,

                        JsonValueKind.Array => string.Join(
                            Environment.NewLine,
                            element.EnumerateArray()
                                .Select(item => item.ValueKind == JsonValueKind.String
                                    ? item.GetString()
                                    : item.ToString())
                                .Where(item => !string.IsNullOrWhiteSpace(item))),

                        JsonValueKind.Null => string.Empty,

                        _ => element.ToString()
                    };
                }

                bool TryGetPropertyIgnoreCase(
                    JsonElement element,
                    string propertyName,
                    out JsonElement value)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Name.Equals(
                            propertyName,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            value = property.Value;
                            return true;
                        }
                    }

                    value = default;
                    return false;
                }

                var result = new AiStudyRecommendationViewModel
                {
                    MainFocus = TryGetPropertyIgnoreCase(
                        root, "mainFocus", out var focus)
                            ? ReadText(focus)
                            : string.Empty,

                    StudyPlan = TryGetPropertyIgnoreCase(
                        root, "studyPlan", out var plan)
                            ? ReadText(plan)
                            : string.Empty,

                    Recommendations = TryGetPropertyIgnoreCase(
                        root, "recommendations", out var recommendations)
                        && recommendations.ValueKind == JsonValueKind.Array
                            ? recommendations.EnumerateArray()
                                .Select(ReadText)
                                .Where(item => !string.IsNullOrWhiteSpace(item))
                                .Take(3)
                                .Select(item => Limit(item, 400))
                                .ToList()
                            : new List<string>()
                };

                result.MainFocus = Limit(result.MainFocus, 300);
                result.StudyPlan = Limit(result.StudyPlan, 1000);

                if (string.IsNullOrWhiteSpace(result.MainFocus) ||
                    result.Recommendations.Count == 0 ||
                    string.IsNullOrWhiteSpace(result.StudyPlan))
                {
                    throw new InvalidOperationException(
                        "The AI recommendation was incomplete.");
                }

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException(
                    "The AI study recommendation request timed out.", ex);
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
                    "Ollama returned invalid JSON for the study recommendation.", ex);
            }
        }


        private static string Limit(string? value, int maxLength)
        {
            value = value?.Trim() ?? string.Empty;

            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }

        private sealed class OllamaResponse
        {
            public string? Response { get; set; }
        }
    }
}
