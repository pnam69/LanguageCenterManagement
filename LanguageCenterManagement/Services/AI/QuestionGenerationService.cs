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
        private const int MaxAttempts = 3;
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
                    _logger.LogDebug(ex, "Skipping invalid generated question JSON.");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogDebug(ex, "Skipping invalid generated question.");
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
                    - Provide between 2 and 4 distinct answer options.
                    - Exactly one option must be correct.
                    - The question must have ONE unambiguously correct answer.
                    - Incorrect options must contain a clear grammatical or factual error.
                    - Never include multiple sentences that are all correct.
                    - For grammar questions, make distractors test common grammar mistakes.
                    - Check every option independently before returning the question.
                    """,

                "TrueFalse" => """
            QUESTION TYPE: TrueFalse
            - Write a factual statement.
            - Provide exactly two answers: "True" and "False".
            - Exactly one answer must be correct.
            """,

                "FillInTheBlank" => """
            QUESTION TYPE: FillInTheBlank
            - Include exactly one missing word or phrase marked ____.
            - Provide exactly one answer, with "isCorrect": true.
            """,

                _ => throw new InvalidOperationException(
                    "Unsupported question type.")
            };



            string passageRules =
                request.Skill == "Reading" &&
                !string.IsNullOrWhiteSpace(request.PassageText)
                ? $$"""

          READING PASSAGE:
          {{request.PassageText}}

          Base the question on this passage.
          Do not copy existing exam questions.
          Ensure the correct answer is supported by the passage.
          """
                : "";

            string previousQuestionRules = previousQuestions.Count == 0
                ? "Create useful, distinct questions for the requested topic."
                : $$"""

          PREVIOUSLY GENERATED QUESTIONS:
          {{JsonSerializer.Serialize(previousQuestions.Select(q => new
                {
                    Question = q.QuestionText,
                    Answers = q.Answers.Select(a => a.AnswerText).ToList()
                }))}}

          Do not repeat these questions or make minor wording changes.
          You may test the same grammar concept using a new sentence.
          Prioritize useful questions over forced variety.
          """;

            string retryRules = string.IsNullOrWhiteSpace(previousError)
                ? ""
                : $$"""

          YOUR PREVIOUS ATTEMPT WAS REJECTED:
          {{previousError}}

          Correct this specific problem. Generate a valid, new question.
          Check the answer count and correct-answer flags before responding.
          """;

            return $$"""
        You are an experienced English teacher creating a practice quiz.

        Generate exactly {{request.Count}} questions.
        Return all questions inside one JSON "questions" array.

        Topic: {{request.Topic}}
        Skill: {{request.Skill}}
        Difficulty: {{request.Difficulty}}

        PURPOSE:
        Test the student's underlying English knowledge, not reproduce
        an existing examination.

        - For grammar, test rules using new sentences and examples.
        - For vocabulary, test meaning and appropriate word usage.
        - For reading, test comprehension, inference, or vocabulary in context.
        - Do not copy existing exam questions or their answer options.
        - Use natural English appropriate to the requested difficulty.
        - Questions may test the same general topic, but must not repeat
          an earlier question with trivial changes.

        {{typeRules}}

        For MultipleChoice grammar questions:
        - "questionText" must contain ONLY the question or sentence with a blank.
        - Never include answer options inside "questionText".
        - Put every possible answer exclusively in the "answers" array.
        - Do not append options such as A), B), C), D) to the question text.
        - Do not include the correct answer or its explanation in "questionText".

        {{previousQuestionRules}}

        {{passageRules}}

        {{retryRules}}
        
        OUTPUT REQUIREMENTS:
        Return valid JSON only. No Markdown, code fences, or explanations.
        The root object MUST contain a property named "questions".
        "questions" MUST be an array containing exactly {{request.Count}} questions.
        Each question MUST contain "questionText" and "answers".
        Each answer MUST contain "answerText" and "isCorrect".
        Do not put answers into the question text. Do not include any other properties. In any forms.
        "isCorrect" MUST be a JSON boolean: true or false, not a string.

        Required structure:
        {
          "questions": [
            {
              "questionText": "A new question",
              "answers": [
                {
                  "answerText": "An answer",
                  "isCorrect": true
                },
                {
                  "answerText": "Another answer",
                  "isCorrect": false
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
            if (question.Answers.Any(a => a == null))
            {
                throw new InvalidOperationException(
                    "The generated question contains a null answer.");
            }

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

            // Merge duplicate answer options, preserving a correct answer if present.
            question.Answers = question.Answers
                .GroupBy(
                    a => a.AnswerText.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g.First();
                    first.AnswerText = first.AnswerText.Trim();
                    first.IsCorrect = g.Any(a => a.IsCorrect);
                    return first;
                })
                .ToList();

            switch (questionType)
            {
                case "MultipleChoice":
                    if (question.Answers.Count < 2 ||
                        question.Answers.Count > 4)
                    {
                        throw new InvalidOperationException(
                            "MultipleChoice requires between 2 and 4 answers.");
                    }
                    break;

                case "TrueFalse":
                    if (question.Answers.Count != 2)
                    {
                        throw new InvalidOperationException(
                            "TrueFalse requires exactly 2 answers.");
                    }
                    break;

                case "FillInTheBlank":
                    if (question.Answers.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "FillInTheBlank requires exactly 1 answer.");
                    }
                    break;

                default:
                    throw new InvalidOperationException(
                        "Unsupported question type.");
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
