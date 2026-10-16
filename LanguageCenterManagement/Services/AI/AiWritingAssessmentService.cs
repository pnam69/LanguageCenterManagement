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

            Score the writing using exactly four criteria, each from 0 to 5:
            1. Task Achievement and Relevance
            2. Organization and Coherence
            3. Vocabulary
            4. Grammar and Sentence Structure

            TotalScore must equal the sum of the four criterion scores.
            MaxScore must be 20.

            Give specific, constructive feedback. Do not invent mistakes.
            Corrections should quote an actual phrase from the student's writing
            and explain a correction when possible.
            Suggestions should be actionable.
            ImprovedVersion should preserve the student's original meaning and
            use language appropriate to the apparent proficiency level.
            If the writing is already correct, say so instead of inventing errors.

            Return only valid JSON using this exact structure:
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
              "corrections": ["Original phrase -> corrected phrase and reason"],
              "suggestions": ["Actionable suggestion"],
              "improvedVersion": "Revised version of the student's writing"
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
                "api/generate",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var ollamaResponse =
                await response.Content.ReadFromJsonAsync<OllamaResponse>(
                    cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(ollamaResponse?.Response))
            {
                throw new InvalidOperationException(
                    "Ollama returned an empty writing assessment.");
            }

            var result =
                JsonSerializer.Deserialize<WritingAssessmentResultViewModel>(
                    ollamaResponse.Response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null || result.Criteria == null ||
                result.Criteria.Count != 4)
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