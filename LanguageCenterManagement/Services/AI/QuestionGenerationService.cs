
using System.Net.Http.Json;
using System.Text.Json;

namespace LanguageCenterManagement.Services.Ai
{
    public class QuestionGenerationService
    {
        private readonly HttpClient _httpClient;
        private const string Model = "qwen2.5:3b";
        private const int MaxAttempts = 3;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public QuestionGenerationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri("http://localhost:11434/");
        }


        public async Task<List<GeneratedQuestion>> GenerateAsync(
            QuestionGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var questions = new List<GeneratedQuestion>();

            for (int i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? lastError = null;
                bool generated = false;

                for (int attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        // Request exactly one question at a time.
                        var singleRequest = new QuestionGenerationRequest
                        {
                            Topic = request.Topic,
                            Skill = request.Skill,
                            Difficulty = request.Difficulty,
                            QuestionType = request.QuestionType,
                            Count = 1,
                            ReadingContentId = request.ReadingContentId,
                            PassageText = request.PassageText
                        };

                        string prompt = BuildPrompt(singleRequest, lastError);

                        using var response = await _httpClient.PostAsJsonAsync(
                            "api/generate",
                            new
                            {
                                model = Model,
                                prompt,
                                stream = false,
                                format = "json"
                            },
                            cancellationToken);

                        response.EnsureSuccessStatusCode();

                        using var responseJson = JsonDocument.Parse(
                            await response.Content.ReadAsStringAsync(
                                cancellationToken));

                        if (!responseJson.RootElement.TryGetProperty(
                            "response", out var responseElement))
                        {
                            throw new JsonException(
                                "Ollama response is missing the response field.");
                        }

                        string? generatedText = responseElement.GetString();

                        if (string.IsNullOrWhiteSpace(generatedText))
                            throw new JsonException("Ollama returned empty output.");

                        using var resultJson = JsonDocument.Parse(generatedText);

                        if (!resultJson.RootElement.TryGetProperty(
                            "questions", out var questionsElement))
                        {
                            throw new JsonException(
                                "The generated JSON has no questions array.");
                        }

                        var batch =
                            JsonSerializer.Deserialize<List<GeneratedQuestion>>(
                                questionsElement.GetRawText(),
                                JsonOptions);

                        if (batch == null || batch.Count != 1)
                        {
                            throw new InvalidOperationException(
                                "Expected exactly one question.");
                        }

                        ValidateQuestion(batch[0], request.QuestionType);

                        questions.Add(batch[0]);
                        generated = true;
                        break;
                    }
                    catch (JsonException ex)
                    {
                        lastError = ex.Message;
                    }
                    catch (InvalidOperationException ex)
                    {
                        lastError = ex.Message;
                    }
                }

                if (!generated)
                {
                    throw new InvalidOperationException(
                        $"Could not generate question {i + 1} of {request.Count} " +
                        $"after {MaxAttempts} attempts. Last error: {lastError}");
                }
            }

            return questions;
        }


        private static void ValidateRequest(
            QuestionGenerationRequest request)
        {
            if (request.Count < 1 || request.Count > 10)
                throw new InvalidOperationException(
                    "Generate between 1 and 10 questions.");

            if (!new[] { "MultipleChoice", "TrueFalse", "FillInTheBlank" }
                .Contains(request.QuestionType))
            {
                throw new InvalidOperationException(
                    "Unsupported question type.");
            }

            if (string.IsNullOrWhiteSpace(request.Topic))
                throw new InvalidOperationException(
                    "A topic is required.");

            if (request.Skill == "Reading" &&
                !string.IsNullOrWhiteSpace(request.PassageText) &&
                request.PassageText.Length > 15000)
            {
                throw new InvalidOperationException(
                    "The reading passage is too long.");
            }
        }

        private static string BuildPrompt(
            QuestionGenerationRequest request,
            string? previousError)
        {
            string typeRules = request.QuestionType switch
            {
                "MultipleChoice" => """
                    QUESTION TYPE: MultipleChoice
                    - Write a normal question that asks the student to choose an answer.
                    - Provide exactly four distinct, meaningful answer options.
                    - Exactly one option must be correct.
                    - Do not use True/False format.
                    - Do not ask the student to fill in a blank.
                    - Do not put ____ in the question text.
                    """,

                "TrueFalse" => """
                    QUESTION TYPE: TrueFalse
                    - Write a factual statement that can be judged True or False.
                    - Provide exactly two answers: True and False.
                    - Exactly one answer must be correct.
                    - Use the exact answer texts "True" and "False".
                    - Do not provide other answer options.
                    - Do not use fill-in-the-blank format.
                    """,

                "FillInTheBlank" => """
                    QUESTION TYPE: FillInTheBlank
                    - Write a sentence with exactly one missing word or phrase.
                    - Mark the missing part with ____ in the question text.
                    - Provide exactly one answer containing the correct word or phrase.
                    - Set that answer's isCorrect value to true.
                    - Do not provide multiple-choice distractors.
                    """,

                _ => throw new InvalidOperationException(
                    "Unsupported question type.")
            };

            string passageRules =
                request.Skill == "Reading" &&
                !string.IsNullOrWhiteSpace(request.PassageText)
                ? $"""
                   
                   READING PASSAGE:
                   {request.PassageText}

                   - Base questions and correct answers on this passage.
                   - Do not require outside knowledge.
                   - Ensure the correct answer is supported by the passage.
                   """
                : "";

            string retryRules = string.IsNullOrWhiteSpace(previousError)
                ? ""
                : $"""

                   YOUR PREVIOUS RESPONSE WAS INVALID:
                   {previousError}

                   Generate a completely new response.
                   Follow every rule. Do not repeat the previous mistake.
                   """;

            return $$"""
                You are an English teacher generating assessment questions.

                Generate exactly {{request.Count}} questions.
                Topic: {{request.Topic}}
                Skill: {{request.Skill}}
                Difficulty: {{request.Difficulty}}

                {{typeRules}}

                GENERAL RULES:
                - All questions must use the requested question type.
                - Never mix question types.
                - Every question must have non-empty question text.
                - Answer texts must be non-empty and distinct.
                - Mark exactly one answer as correct, except that the
                  single FillInTheBlank answer must be correct.
                - Decide the correct answer before writing the distractors.
                - Do not fabricate answers or use placeholders.
                - Return valid JSON only, without markdown or explanations.

                {{passageRules}}
                {{retryRules}}

                REQUIRED JSON:
                {
                  "questions": [
                    {
                      "questionText": "Question text",
                      "answers": [
                        {
                          "answerText": "Answer text",
                          "isCorrect": true
                        }
                      ]
                    }
                  ]
                }
                """;
        }

        private static void ValidateQuestion(
            GeneratedQuestion question,
            string questionType)
        {
            if (question == null ||
                string.IsNullOrWhiteSpace(question.QuestionText))
            {
                throw new InvalidOperationException(
                    "A question has empty text.");
            }

            question.QuestionText = question.QuestionText.Trim();
            question.Answers ??= new List<GeneratedAnswer>();

            if (question.QuestionText.Length > 2000)
                throw new InvalidOperationException(
                    "A question exceeds 2000 characters.");

            if (question.Answers.Any(a =>
                    a == null ||
                    string.IsNullOrWhiteSpace(a.AnswerText) ||
                    a.AnswerText.Length > 1000))
            {
                throw new InvalidOperationException(
                    "An answer is empty or too long.");
            }

            foreach (var answer in question.Answers)
                answer.AnswerText = answer.AnswerText.Trim();

            if (question.Answers
                .Select(a => a.AnswerText)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != question.Answers.Count)
            {
                throw new InvalidOperationException(
                    "A question contains duplicate answers.");
            }

            int expectedCount = questionType switch
            {
                "MultipleChoice" => 4,
                "TrueFalse" => 2,
                "FillInTheBlank" => 1,
                _ => throw new InvalidOperationException(
                    "Unsupported question type.")
            };

            if (question.Answers.Count != expectedCount)
            {
                throw new InvalidOperationException(
                    $"{questionType} requires exactly {expectedCount} answers.");
            }

            if (question.Answers.Count(a => a.IsCorrect) != 1)
            {
                throw new InvalidOperationException(
                    "A question must have exactly one correct answer.");
            }

            switch (questionType)
            {
                case "MultipleChoice":
                    if (question.QuestionText.Contains("____") ||
                        question.QuestionText.Contains(
                            "true or false",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "A MultipleChoice question uses another question format.");
                    }

                    if (question.Answers.Any(a =>
                        a.AnswerText.Equals(
                            "Option 1", StringComparison.OrdinalIgnoreCase) ||
                        a.AnswerText.Equals(
                            "Option 2", StringComparison.OrdinalIgnoreCase) ||
                        a.AnswerText.Equals(
                            "Option 3", StringComparison.OrdinalIgnoreCase) ||
                        a.AnswerText.Equals(
                            "Option 4", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException(
                            "MultipleChoice contains placeholder answers.");
                    }
                    break;

                case "TrueFalse":
                    var answerSet = question.Answers
                        .Select(a => a.AnswerText)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (!answerSet.SetEquals(new[] { "True", "False" }))
                    {
                        throw new InvalidOperationException(
                            "TrueFalse must contain exactly True and False.");
                    }

                    // Normalize spelling while preserving correctness.
                    foreach (var answer in question.Answers)
                    {
                        answer.AnswerText =
                            answer.AnswerText.Equals(
                                "True", StringComparison.OrdinalIgnoreCase)
                                ? "True"
                                : "False";
                    }
                    break;

                case "FillInTheBlank":
                    if (!question.QuestionText.Contains("____"))
                    {
                        throw new InvalidOperationException(
                            "FillInTheBlank must contain ____.");
                    }

                    if (!question.Answers[0].IsCorrect)
                    {
                        throw new InvalidOperationException(
                            "The fill-in-the-blank answer must be correct.");
                    }
                    break;
            }
        }
    }
}
