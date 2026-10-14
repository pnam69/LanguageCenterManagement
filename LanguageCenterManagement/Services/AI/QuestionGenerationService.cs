using System.Net.Http.Json;
using System.Text.Json;

namespace LanguageCenterManagement.Services.Ai
{
    public class QuestionGenerationService
    {
        private readonly HttpClient _httpClient;

        public QuestionGenerationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri("http://localhost:11434/");
        }

        public async Task<List<GeneratedQuestion>> GenerateAsync(
    QuestionGenerationRequest request,
    CancellationToken cancellationToken = default)
        {
            int expectedAnswers = request.QuestionType switch
            {
                "MultipleChoice" => 4,
                "TrueFalse" => 2,
                "FillInTheBlank" => 1,
                _ => throw new InvalidOperationException("Unsupported question type.")
            };

            var typeInstructions = request.QuestionType switch
            {
                "MultipleChoice" =>
                    "Each question must have exactly four distinct answer options and exactly one correct answer.",
                "TrueFalse" =>
                    "Each question must have exactly two options: \"True\" and \"False\". Exactly one must be correct.",
                "FillInTheBlank" =>
                    "Each question must contain a blank shown as ____. Return exactly one answer object with the expected word or phrase and isCorrect set to true.",
                _ => throw new InvalidOperationException("Unsupported question type.")
            };

            var prompt = string.Format("""
        You are an English teacher creating quiz questions.

        Generate exactly {0} questions.
        Topic: {1}
        Skill: {2}
        Difficulty: {3}
        Question type: {4}

        RULES:
        - Every question must have exactly {6} answer object(s).
        - Exactly one answer must have "isCorrect": true. All others must be false.
        - Never omit an answer.
        - Answer texts must be distinct and non-empty.
        - Return JSON only. Do not include explanations or markdown.
        - For each question, decide the correct answer first, then set isCorrect accordingly.
        - Do not set every isCorrect value to false.
        {5}

        Required JSON structure:
        {{
          "questions": [
            {{
              "questionText": "Example question",
              "answers": [
                {{ "answerText": "Correct option", "isCorrect": true }},
                {{ "answerText": "Wrong option 1", "isCorrect": false }},
                {{ "answerText": "Wrong option 2", "isCorrect": false }},
                {{ "answerText": "Wrong option 3", "isCorrect": false }}
              ]
            }}
          ]
        }}

        For MultipleChoice → exactly four options.
        For TrueFalse → exactly two options named "True" and "False".
        For FillInTheBlank → put ____ in the question text and return one correct answer.
        """,
                request.Count,
                request.Topic,
                request.Skill,
                request.Difficulty,
                request.QuestionType,
                typeInstructions,
                expectedAnswers);

            using var response = await _httpClient.PostAsJsonAsync(
                "api/generate",
                new
                {
                    model = "qwen2.5:3b",
                    prompt,
                    stream = false,
                    format = "json"
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            using var responseJson = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            var generatedText = responseJson.RootElement
                .GetProperty("response")
                .GetString();

            if (string.IsNullOrWhiteSpace(generatedText))
                throw new InvalidOperationException("Ollama returned an empty response.");

            using var resultJson = JsonDocument.Parse(generatedText);

            var questions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(
                resultJson.RootElement.GetProperty("questions").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (questions is null || questions.Count != request.Count)
                throw new InvalidOperationException(
                    "Ollama returned an unexpected number of questions.");

            foreach (var question in questions)
            {
                NormalizeQuestion(question, request.QuestionType);
            }

            return questions;
        }

        // Keep the NormalizeQuestion / Ensure* helper methods exactly as in the previous reply

        private static void NormalizeQuestion(GeneratedQuestion question, string questionType)
        {
            question.QuestionText ??= string.Empty;
            question.Answers ??= new List<GeneratedAnswer>();

            // Clean up answers while PRESERVING the isCorrect flag
            question.Answers = question.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                .Select(a =>
                {
                    a.AnswerText = a.AnswerText.Trim();
                    return a;
                })
                .GroupBy(a => a.AnswerText, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())   // keep the first occurrence (and its isCorrect)
                .ToList();

            if (string.IsNullOrWhiteSpace(question.QuestionText))
                throw new InvalidOperationException(
                    "Ollama returned a question with empty text.");

            switch (questionType)
            {
                case "TrueFalse":
                    EnsureTrueFalse(question);
                    break;

                case "FillInTheBlank":
                    EnsureFillInTheBlank(question);
                    break;

                case "MultipleChoice":
                    EnsureMultipleChoice(question);
                    break;
            }

            // Final safety: guarantee exactly one correct answer
            EnsureExactlyOneCorrect(question);
        }

        private static void EnsureTrueFalse(GeneratedQuestion question)
        {
            // Prefer the answer the model marked as correct
            var correctFromModel = question.Answers
                .FirstOrDefault(a => a.IsCorrect)?.AnswerText;

            bool isTrueCorrect = correctFromModel?.Equals("True", StringComparison.OrdinalIgnoreCase) == true
                                 || (correctFromModel is null &&
                                     question.Answers.Any(a => a.AnswerText.Equals("True", StringComparison.OrdinalIgnoreCase) && a.IsCorrect));

            // If the model didn't mark anything, try to guess from content (rare)
            if (correctFromModel is null)
            {
                // Fallback: look for any answer that looks like True
                isTrueCorrect = question.Answers
                    .Any(a => a.AnswerText.Equals("True", StringComparison.OrdinalIgnoreCase));
            }

            question.Answers = new List<GeneratedAnswer>
            {
                new() { AnswerText = "True",  IsCorrect = isTrueCorrect },
                new() { AnswerText = "False", IsCorrect = !isTrueCorrect }
            };
        }

        private static void EnsureFillInTheBlank(GeneratedQuestion question)
        {
            if (!question.QuestionText.Contains("____"))
            {
                question.QuestionText = question.QuestionText
                    .TrimEnd('.', '?', '!', ' ') + " ____";
            }

            // Keep only the first non-empty answer and mark it correct
            var answerText = question.Answers
                .Select(a => a.AnswerText)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? string.Empty;

            question.Answers = new List<GeneratedAnswer>
            {
                new() { AnswerText = answerText, IsCorrect = true }
            };
        }

        private static void EnsureMultipleChoice(GeneratedQuestion question)
        {
            // Pad or trim to exactly 4 options
            while (question.Answers.Count < 4)
            {
                question.Answers.Add(new GeneratedAnswer
                {
                    AnswerText = $"Option {question.Answers.Count + 1}",
                    IsCorrect = false
                });
            }

            if (question.Answers.Count > 4)
                question.Answers = question.Answers.Take(4).ToList();
        }

        private static void EnsureExactlyOneCorrect(GeneratedQuestion question)
        {
            var correctCount = question.Answers.Count(a => a.IsCorrect);

            if (correctCount == 1)
                return;

            // If the model marked zero or more than one, force the first answer to be correct
            foreach (var a in question.Answers)
                a.IsCorrect = false;

            if (question.Answers.Count > 0)
                question.Answers[0].IsCorrect = true;
        }
    }
}