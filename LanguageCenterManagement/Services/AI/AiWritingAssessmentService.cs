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
            if (string.IsNullOrWhiteSpace(taskPrompt))
                throw new ArgumentException("The writing task cannot be empty.", nameof(taskPrompt));

            if (string.IsNullOrWhiteSpace(studentWriting))
                throw new ArgumentException("The student's writing cannot be empty.", nameof(studentWriting));

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

            //var request = new
            //{
            //    model = "qwen2.5:3b",
            //    prompt,
            //    stream = false,
            //    format = "json"
            //};

            //try
            //{
            //    using var response = await _httpClient.PostAsJsonAsync(
            //        "api/generate",
            //        request,
            //        cancellationToken);

            //    response.EnsureSuccessStatusCode();

            //    var ollamaResponse =
            //        await response.Content.ReadFromJsonAsync<OllamaResponse>(
            //            cancellationToken: cancellationToken);

            //    if (string.IsNullOrWhiteSpace(ollamaResponse?.Response))
            //    {
            //        throw new InvalidOperationException(
            //            "Ollama returned an empty writing assessment.");
            //    }

            //    using var document = JsonDocument.Parse(ollamaResponse.Response);
            //    var root = document.RootElement;

            //    if (root.ValueKind != JsonValueKind.Object)
            //    {
            //        throw new InvalidOperationException(
            //            "Ollama returned an invalid JSON object.");
            //    }

            //    var normalizedCorrections = new List<string>();

            //    if (root.TryGetProperty("corrections", out var corrections) &&
            //        corrections.ValueKind == JsonValueKind.Array)
            //    {
            //        foreach (var item in corrections.EnumerateArray())
            //        {
            //            if (item.ValueKind == JsonValueKind.String)
            //            {
            //                var text = item.GetString();

            //                if (!string.IsNullOrWhiteSpace(text))
            //                    normalizedCorrections.Add(text);
            //            }
            //            else if (item.ValueKind == JsonValueKind.Object)
            //            {
            //                string GetText(string name) =>
            //                    item.TryGetProperty(name, out var value) &&
            //                    value.ValueKind == JsonValueKind.String
            //                        ? value.GetString() ?? string.Empty
            //                        : string.Empty;

            //                var original = GetText("original");
            //                var corrected = GetText("corrected");
            //                var explanation = GetText("explanation");

            //                var correction = string.IsNullOrWhiteSpace(original)
            //                    ? corrected
            //                    : $"{original} -> {corrected}";

            //                if (!string.IsNullOrWhiteSpace(explanation))
            //                    correction += $" ({explanation})";

            //                if (!string.IsNullOrWhiteSpace(correction))
            //                    normalizedCorrections.Add(correction);
            //            }
            //        }
            //    }

            //    using var normalizedStream = new MemoryStream();

            //    using (var writer = new Utf8JsonWriter(normalizedStream))
            //    {
            //        writer.WriteStartObject();

            //        foreach (var property in root.EnumerateObject())
            //        {
            //            if (property.Name.Equals(
            //                "corrections",
            //                StringComparison.OrdinalIgnoreCase))
            //            {
            //                writer.WritePropertyName("corrections");
            //                JsonSerializer.Serialize(writer, normalizedCorrections);
            //            }
            //            else
            //            {
            //                property.WriteTo(writer);
            //            }
            //        }

            //        if (!root.EnumerateObject().Any(p =>
            //            p.Name.Equals("corrections", StringComparison.OrdinalIgnoreCase)))
            //        {
            //            writer.WritePropertyName("corrections");
            //            JsonSerializer.Serialize(writer, normalizedCorrections);
            //        }

            //        writer.WriteEndObject();
            //    }

            //    var normalizedJson =
            //        System.Text.Encoding.UTF8.GetString(normalizedStream.ToArray());

            //    var result = JsonSerializer.Deserialize<WritingAssessmentResultViewModel>(
            //        normalizedJson,
            //        new JsonSerializerOptions
            //        {
            //            PropertyNameCaseInsensitive = true
            //        });

            //    if (result?.Criteria == null || result.Criteria.Count != 4)
            //    {
            //        throw new InvalidOperationException(
            //            "Ollama returned an invalid writing assessment: expected exactly four criteria.");
            //    }

            //    foreach (var criterion in result.Criteria)
            //    {
            //        if (criterion == null)
            //            throw new InvalidOperationException("Ollama returned a null assessment criterion.");

            //        criterion.Score = Math.Clamp(criterion.Score, 0, 5);
            //        criterion.MaxScore = 5;
            //        criterion.Name ??= string.Empty;
            //        criterion.Feedback ??= string.Empty;
            //    }

            //    result.TotalScore = result.Criteria.Sum(c => c.Score);
            //    result.MaxScore = 20;
            //    result.OverallFeedback ??= string.Empty;
            //    result.Corrections ??= new List<string>();
            //    result.Suggestions ??= new List<string>();
            //    result.ImprovedVersion ??= string.Empty;

            //    return result;
            //}
            //catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            //{
            //    throw;
            //}

            //catch (OperationCanceledException ex)
            //{
            //    throw new TimeoutException(
            //        "The Ollama writing assessment request timed out.", ex);
            //}
            //catch (HttpRequestException ex)
            //{
            //    throw new InvalidOperationException(
            //        "Could not communicate with Ollama. Check that Ollama is running and the qwen2.5:3b model is available.",
            //        ex);
            //}
            //catch (JsonException ex)
            //{
            //    throw new InvalidOperationException(
            //        "Ollama returned invalid JSON for the writing assessment.", ex);
            //}
            return await AssessPromptAsync(prompt, cancellationToken);
        }

        public async Task<WritingAssessmentResultViewModel> AssessExamAsync(
    string examName,
    IEnumerable<TeacherExamGradeQuestionViewModel> questions,
    CancellationToken cancellationToken = default)
        {
            var submittedAnswers = questions.Select(q => new
            {
                Question = q.QuestionOrder,
                q.QuestionText,
                q.QuestionType,
                q.Skill,
                StudentAnswer = q.StudentAnswer ?? "(No answer)",
                CorrectAnswer = q.CorrectAnswer ?? "(Not available)",
                EarnedScore = q.Score,
                MaximumScore = q.MaxScore,
                IsCorrect = q.IsCorrect
            }).ToList();

            if (!submittedAnswers.Any())
                throw new InvalidOperationException("No exam answers are available.");

            var earnedScore = submittedAnswers.Sum(q => q.EarnedScore);
            var maximumScore = submittedAnswers.Sum(q => q.MaximumScore);

            if (maximumScore <= 0)
                throw new InvalidOperationException("The exam has no available marks.");

            var percentage = earnedScore / maximumScore * 100;

            var examContent = JsonSerializer.Serialize(submittedAnswers);

            var prompt = $$"""
    You are an English language exam feedback assistant.

    Review the student's entire exam: {{examName}}

    Actual grading results:
    - Earned marks: {{earnedScore}}
    - Maximum marks: {{maximumScore}}
    - Performance: {{percentage:F2}}%

    Submitted questions and answers:
    {{examContent}}

    IMPORTANT:
    The supplied scores are authoritative.
    Do not claim that correct answers are incorrect.
    Do not claim that all answers are incorrect.
    Do not contradict the supplied IsCorrect values.
    Do not independently calculate or change the official exam score.

    Give useful feedback about:
    1. Knowledge and Accuracy
    2. Reading and Listening Comprehension
    3. Language Application
    4. Overall Performance and Consistency

    Explain the student's actual strengths and weaknesses.
    Do not invent weaknesses.
    Do not claim to assess speaking pronunciation from an audio URL.
    Do not assume an unanswered question was answered incorrectly for
    a reason that is not evident.

    Return only valid JSON with exactly four criteria,
    each having a score from 0 to 5.

    {
      "totalScore": 0,
      "maxScore": 20,
      "overallFeedback": "Overall feedback",
      "criteria": [
        {
          "name": "Knowledge and Accuracy",
          "score": 0,
          "maxScore": 5,
          "feedback": "Feedback"
        },
        {
          "name": "Reading and Listening Comprehension",
          "score": 0,
          "maxScore": 5,
          "feedback": "Feedback"
        },
        {
          "name": "Language Application",
          "score": 0,
          "maxScore": 5,
          "feedback": "Feedback"
        },
        {
          "name": "Overall Performance and Consistency",
          "score": 0,
          "maxScore": 5,
          "feedback": "Feedback"
        }
      ],
      "corrections": [],
      "suggestions": ["Recommendation"],
      "improvedVersion": ""
    }
    """;

            var result = await AssessPromptAsync(prompt, cancellationToken);

            // Numeric assessment is derived from actual grading data,
            // not from the language model's potentially incorrect score.
            var criterionScore = (int)Math.Round(
                earnedScore / maximumScore * 5,
                MidpointRounding.AwayFromZero);

            criterionScore = Math.Clamp(criterionScore, 0, 5);

            foreach (var criterion in result.Criteria)
            {
                criterion.Score = criterionScore;
                criterion.MaxScore = 5;
            }

            result.TotalScore = criterionScore * 4;
            result.MaxScore = 20;

            return result;
        }

        private async Task<WritingAssessmentResultViewModel> AssessPromptAsync(
    string prompt,
    CancellationToken cancellationToken)
        {
            var request = new
            {
                model = "qwen2.5:3b",
                prompt,
                stream = false,
                format = "json"
            };

            try
            {
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
                            "corrections",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WritePropertyName("corrections");
                            JsonSerializer.Serialize(writer, normalizedCorrections);
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    if (!root.EnumerateObject().Any(p =>
                        p.Name.Equals("corrections", StringComparison.OrdinalIgnoreCase)))
                    {
                        writer.WritePropertyName("corrections");
                        JsonSerializer.Serialize(writer, normalizedCorrections);
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
                        "Ollama returned an invalid writing assessment: expected exactly four criteria.");
                }

                foreach (var criterion in result.Criteria)
                {
                    if (criterion == null)
                        throw new InvalidOperationException("Ollama returned a null assessment criterion.");

                    criterion.Score = Math.Clamp(criterion.Score, 0, 5);
                    criterion.MaxScore = 5;
                    criterion.Name ??= string.Empty;
                    criterion.Feedback ??= string.Empty;
                }

                result.TotalScore = result.Criteria.Sum(c => c.Score);
                result.MaxScore = 20;
                result.OverallFeedback ??= string.Empty;
                result.Corrections ??= new List<string>();
                result.Suggestions ??= new List<string>();
                result.ImprovedVersion ??= string.Empty;

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            catch (OperationCanceledException ex)
            {
                throw new TimeoutException(
                    "The Ollama writing assessment request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    "Could not communicate with Ollama. Check that Ollama is running and the qwen2.5:3b model is available.",
                    ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "Ollama returned invalid JSON for the writing assessment.", ex);
            }
        }
        private class OllamaResponse
        {
            public string? Response { get; set; }
        }
    }
}