using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.Net;

namespace LanguageCenterManagement.Services.Ai
{
    public class QuestionGenerationService
    {
        private readonly HttpClient _httpClient;
        private const string Model = "qwen2.5:3b";
        //private const int MaxAttempts = 3;
        private readonly ILogger<QuestionGenerationService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public QuestionGenerationService(
            HttpClient httpClient,
            ILogger<QuestionGenerationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress ??= new Uri("http://localhost:11434/");
            _httpClient.Timeout = TimeSpan.FromMinutes(3);
        }



        public async Task<List<GeneratedQuestion>> GenerateAsync(
            QuestionGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            string prompt = BuildPrompt(
                request,
                null,
                new List<GeneratedQuestion>());

            using var response = await _httpClient.PostAsJsonAsync(
                "api/generate",
                new
                {
                    model = Model,
                    prompt,
                    stream = false,
                    format = "json",
                    options = new
                    {
                        temperature = 0.5
                    }
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            using var responseJson = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            if (!responseJson.RootElement.TryGetProperty(
                "response", out var responseElement))
            {
                throw new JsonException("Ollama response is missing the response field.");
            }

            string? generatedText = responseElement.GetString();

            if (string.IsNullOrWhiteSpace(generatedText))
                throw new JsonException("Ollama returned empty output.");

            _logger.LogDebug(
                "Ollama raw question output for {QuestionType}: {GeneratedText}",
                request.QuestionType,
                generatedText);

            using var resultJson = JsonDocument.Parse(generatedText);

            if (!resultJson.RootElement.TryGetProperty(
                "questions", out var questionsElement) ||
                questionsElement.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("Generated JSON has no valid questions array.");
            }

            var questions = new List<GeneratedQuestion>();

            foreach (var element in questionsElement.EnumerateArray())
            {
                if (questions.Count >= request.Count)
                    break;

                try
                {
                    var candidate = JsonSerializer.Deserialize<GeneratedQuestion>(
                        element.GetRawText(),
                        JsonOptions);

                    if (candidate == null)
                        continue;

                    ValidateQuestion(candidate, request.QuestionType);

                    if (IsDuplicateQuestion(candidate, questions))
                        continue;

                    questions.Add(candidate);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Rejected malformed question JSON for type {QuestionType}.",
                        request.QuestionType);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Rejected generated question for type {QuestionType}: {Reason}",
                        request.QuestionType,
                        ex.Message);
                }
            }

            if (questions.Count == 0)
                throw new InvalidOperationException(
                    "Ollama did not generate any valid questions.");

            return questions;
        }



        private static void ValidateRequest(QuestionGenerationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Count < 1 || request.Count > 10)
                throw new InvalidOperationException(
                    "Generate between 1 and 10 questions.");

            if (!new[]
                {
            "MultipleChoice",
            "TrueFalse",
            "FillInTheBlank"
        }.Contains(request.QuestionType))
            {
                throw new InvalidOperationException(
                    "Unsupported question type.");
            }

            if (string.IsNullOrWhiteSpace(request.Topic))
                throw new InvalidOperationException(
                    "A topic is required.");

            if (!new[]
                {
            "Reading",
            "Listening",
            "Speaking",
            "Writing",
            "Grammar",
            "Vocabulary"
        }.Contains(request.Skill, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Unsupported skill.");
            }

            if (string.IsNullOrWhiteSpace(request.Difficulty))
                throw new InvalidOperationException(
                    "A difficulty level is required.");

            if (request.Skill.Equals(
                    "Reading",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(request.PassageText) &&
                request.PassageText.Length > 15000)
            {
                throw new InvalidOperationException(
                    "The reading passage is too long.");
            }
        }


        private static string BuildPrompt(
            QuestionGenerationRequest request,
            string? previousError,
            List<GeneratedQuestion> previousQuestions)
        {
            string typeRules = request.QuestionType switch
            {
                "MultipleChoice" => """
            QUESTION TYPE: MultipleChoice

            - questionText contains only the question.
            - Provide exactly 4 answer objects.
            - Each answerText contains one possible answer.
            - Exactly one answer has "isCorrect": true.
            - The other three answers have "isCorrect": false.
            - Do not put A), B), C), D) or answer options in questionText.
            - Use plausible but clearly incorrect distractors.
            - Do not use ____ in questionText.

            Example:
            {
              "questionText": "She _____ to school every day.",
              "answers": [
                { "answerText": "go", "isCorrect": false },
                { "answerText": "goes", "isCorrect": true },
                { "answerText": "going", "isCorrect": false },
                { "answerText": "gone", "isCorrect": false }
              ]
            }
            """,

                "TrueFalse" => """
            QUESTION TYPE: TrueFalse

            - questionText must be one clear factual statement.
            - Do not phrase it as a question.
            - Provide exactly TWO answer objects.
            - The first answerText must be exactly "True".
            - The second answerText must be exactly "False".
            - Exactly one answer must have "isCorrect": true.
            - The other answer must have "isCorrect": false.
            - Do not use Yes, No, T, F, or explanations as answerText.

            Example:
            {
              "questionText": "The past tense of 'go' is 'went'.",
              "answers": [
                { "answerText": "True", "isCorrect": true },
                { "answerText": "False", "isCorrect": false }
              ]
            }
            """,

                "FillInTheBlank" => """
            QUESTION TYPE: FillInTheBlank

            - questionText must contain exactly ONE blank.
            - Represent the blank using exactly four underscores: ____.
            - Provide exactly ONE answer object.
            - answerText must contain only the missing word or phrase.
            - The single answer must have "isCorrect": true.
            - Do not provide alternative answers.
            - Do not put the answer inside questionText.

            Example:
            {
              "questionText": "She _____ to school every day.",
              "answers": [
                { "answerText": "goes", "isCorrect": true }
              ]
            }
            """,

                _ => throw new InvalidOperationException(
                    "Unsupported question type.")
            };

            string passageRules =
                string.Equals(
                    request.Skill,
                    "Reading",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(request.PassageText)
                ? $$"""

        READING PASSAGE:
        {{request.PassageText}}

        Base every reading question on this passage.
        The correct answer must be supported by the passage.
        Do not introduce facts that are not in the passage.
        """
                : "";

            string previousQuestionRules = previousQuestions.Count == 0
                ? "Create distinct questions relevant to the requested topic."
                : $$"""

        PREVIOUSLY GENERATED QUESTIONS:
        {{JsonSerializer.Serialize(previousQuestions.Select(q => new
                {
                    Question = q.QuestionText,
                    Answers = q.Answers.Select(a => a.AnswerText).ToList()
                }))}}

        Do not repeat these questions or make minor wording changes.
        You may test the same concept using a different sentence.
        """
                ;

            string retryRules = string.IsNullOrWhiteSpace(previousError)
                ? ""
                : $$"""

        PREVIOUS VALIDATION ERROR:
        {{previousError}}

        Correct this specific problem. Follow the required structure exactly.
        """
                ;

            return $$"""
    You are an experienced English teacher creating an English practice quiz.

    Generate exactly {{request.Count}} questions.

    Topic: {{request.Topic}}
    Skill: {{request.Skill}}
    Difficulty: {{request.Difficulty}}
    Question type: {{request.QuestionType}}

    CONTENT REQUIREMENTS:
    - Use natural, grammatically correct English.
    - Match the requested difficulty.
    - Test actual English knowledge.
    - For grammar, test rules using new sentences.
    - For vocabulary, test word meaning and appropriate usage.
    - For reading, test comprehension and inference.
    - Ensure every question has one unambiguous correct answer.
    - Do not copy existing examination questions.
    - Do not repeat questions.

    {{typeRules}}

    {{previousQuestionRules}}

    {{passageRules}}

    {{retryRules}}

    OUTPUT REQUIREMENTS:
    - Return valid JSON only.
    - Do not use Markdown or code fences.
    - The root must contain exactly one property named "questions".
    - "questions" must be an array containing exactly {{request.Count}} objects.
    - Every question must contain "questionText" and "answers".
    - Every answer must contain "answerText" and "isCorrect".
    - "isCorrect" must be a JSON boolean, not a string.
    - Do not add explanations or other properties.
    - Follow the question-type example and rules exactly.

    Required root structure:
    {
      "questions": [
        {
          "questionText": "Example question",
          "answers": [
            {
              "answerText": "Example answer",
              "isCorrect": true
            }
          ]
        }
      ]
    }
    """;
        }

        private static bool IsDuplicateQuestion(
    GeneratedQuestion candidate,
    List<GeneratedQuestion> existingQuestions)
        {
            string candidateText = NormalizeQuestion(candidate.QuestionText);

            return existingQuestions.Any(existing =>
                NormalizeQuestion(existing.QuestionText) == candidateText);
        }

        private static string NormalizeQuestion(string question)
        {
            string normalized = question.Trim().ToLowerInvariant();

            normalized = Regex.Replace(normalized, @"[^\p{L}\p{N}\s]", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ");

            return normalized.Trim();
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
            {
                throw new InvalidOperationException(
                    "A question exceeds 2000 characters.");
            }

            if (question.Answers.Any(a =>
                a == null ||
                string.IsNullOrWhiteSpace(a.AnswerText) ||
                a.AnswerText.Length > 1000))
            {
                throw new InvalidOperationException(
                    "An answer is empty or too long.");
            }

            foreach (var answer in question.Answers)
            {
                answer.AnswerText = answer.AnswerText.Trim();
            }

            switch (questionType)
            {
                case "MultipleChoice":
                    {
                        if (question.Answers.Count < 2 ||
                            question.Answers.Count > 4)
                        {
                            throw new InvalidOperationException(
                                "MultipleChoice requires between 2 and 4 answers.");
                        }

                        if (question.QuestionText.Contains("____") ||
                            question.QuestionText.Contains(
                                "true or false",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                "MultipleChoice uses another question format.");
                        }

                        if (question.Answers.Any(a =>
                            Regex.IsMatch(
                                a.AnswerText,
                                @"^Option\s+[1-4]$",
                                RegexOptions.IgnoreCase)))
                        {
                            throw new InvalidOperationException(
                                "MultipleChoice contains placeholder answers.");
                        }

                        break;
                    }

                case "TrueFalse":
                    {
                        if (question.Answers.Count != 2)
                        {
                            throw new InvalidOperationException(
                                "TrueFalse requires exactly 2 answers.");
                        }

                        foreach (var answer in question.Answers)
                        {
                            string normalized = answer.AnswerText
                                .Trim()
                                .TrimEnd('.', '!', '?', ':', ';')
                                .Trim();

                            if (normalized.Equals(
                                "True",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                answer.AnswerText = "True";
                            }
                            else if (normalized.Equals(
                                "False",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                answer.AnswerText = "False";
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    "TrueFalse answers must be True and False.");
                            }
                        }

                        var answerSet = question.Answers
                            .Select(a => a.AnswerText)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        if (!answerSet.SetEquals(new[] { "True", "False" }))
                        {
                            throw new InvalidOperationException(
                                "TrueFalse must contain one True and one False answer.");
                        }

                        break;
                    }

                case "FillInTheBlank":
                    {
                        if (question.Answers.Count != 1)
                        {
                            throw new InvalidOperationException(
                                "FillInTheBlank requires exactly 1 answer.");
                        }

                        // Accept common model variations for a single blank.
                        question.QuestionText = Regex.Replace(
                            question.QuestionText,
                            @"_{3,}|\[blank\]|\[____\]",
                            "____",
                            RegexOptions.IgnoreCase);

                        int blankCount = Regex.Matches(
                            question.QuestionText,
                            @"____").Count;

                        if (blankCount != 1)
                        {
                            throw new InvalidOperationException(
                                "FillInTheBlank must contain exactly one blank.");
                        }

                        // There is exactly one permitted answer in this question type.
                        question.Answers[0].IsCorrect = true;

                        break;
                    }

                default:
                    throw new InvalidOperationException(
                        "Unsupported question type.");
            }

            // Reject duplicate answer labels/options rather than silently merging them.
            if (question.Answers
                .Select(a => a.AnswerText)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != question.Answers.Count)
            {
                throw new InvalidOperationException(
                    "The question contains duplicate answers.");
            }

            if (question.Answers.Count(a => a.IsCorrect) != 1)
            {
                throw new InvalidOperationException(
                    "A question must have exactly one correct answer.");
            }
        }
    }
}
