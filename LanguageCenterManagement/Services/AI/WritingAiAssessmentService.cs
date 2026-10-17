
using System.Net.Http.Json;
using System.Text.Json;
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Services.Ai
{
    public class WritingAiAssessmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public WritingAiAssessmentService(
            ApplicationDbContext context,
            HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        public async Task CheckAnswerAsync(int examAnswerId)
        {
            var answer = await _context.ExamAnswers
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a =>
                    a.ExamAnswerId == examAnswerId);

            if (answer == null ||
                answer.Question?.Skill != "Writing" ||
                string.IsNullOrWhiteSpace(answer.TextAnswer))
            {
                return;
            }

            var writingContent = await _context.WritingContents
                .FirstOrDefaultAsync(w =>
                    w.QuestionId == answer.QuestionId);

            string prompt = writingContent?.Prompt
                ?? answer.Question.QuestionText;

            var request = new
            {
                model = "qwen2.5:3b",
                stream = false,
                format = "json",
                options = new
                {
                    temperature = 0
                },
                prompt = $$$"""
                You are assisting a teacher reviewing an English essay.

                This is NOT a reliable AI-authorship detection test.
                Do not claim you can determine authorship with certainty.
                Assess only whether the text contains features that warrant
                a teacher's closer review.

                Consider:
                - Generic or formulaic wording
                - Repetitive structure or ideas
                - Unusually uniform sentence patterns
                - Specific inconsistencies between the prompt and response

                Do not treat good grammar, formal vocabulary, or correct
                spelling alone as evidence of AI use.
                Do not assume a student used AI merely because the essay
                is well written.

                Return valid JSON with exactly these properties:
                {{
                    "status": "Review" or "NoConcern",
                    "indicators": "Brief list of observable features",
                    "explanation": "Brief explanation and limitations"
                }}

                If there is insufficient evidence, choose NoConcern.
                A Review result means only that a teacher should examine it.
                Never describe the result as proof or a probability of AI use.

                Writing prompt:
                {prompt}

                Student essay:
                {answer.TextAnswer}
                """
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/generate", request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<OllamaResponse>();

            if (string.IsNullOrWhiteSpace(result?.Response))
                throw new InvalidOperationException(
                    "Ollama returned an empty assessment.");

            var assessment =
                JsonSerializer.Deserialize<AssessmentResult>(
                    result.Response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (assessment == null)
                throw new InvalidOperationException(
                    "Could not read Ollama's assessment.");

            var review = await _context.WritingAiReviews
                .FirstOrDefaultAsync(r =>
                    r.ExamAnswerId == examAnswerId);

            if (review == null)
            {
                review = new WritingAiReview
                {
                    ExamAnswerId = examAnswerId
                };

                _context.WritingAiReviews.Add(review);
            }

            review.Status =
                assessment.Status == "Review"
                    ? "Review"
                    : "NoConcern";

            review.Indicators = Limit(
                assessment.Indicators, 2000);

            review.Explanation = Limit(
                assessment.Explanation, 2000);

            review.CheckedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        private static string? Limit(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();

            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }

        private sealed class OllamaResponse
        {
            public string? Response { get; set; }
        }

        private sealed class AssessmentResult
        {
            public string Status { get; set; } = "NoConcern";
            public string? Indicators { get; set; }
            public string? Explanation { get; set; }
        }
    }
}
