using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LanguageCenterManagement.Services.Ai
{
    public class WritingIntegrityService
    {
        private readonly ApplicationDbContext _context;

        public WritingIntegrityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CheckAnswerAsync(
            int examAnswerId,
            CancellationToken cancellationToken = default)
        {
            var answer = await _context.ExamAnswers
                .Include(a => a.Question)
                .Include(a => a.ExamResult)
                .FirstOrDefaultAsync(
                    a => a.ExamAnswerId == examAnswerId,
                    cancellationToken);

            if (answer?.Question == null ||
                answer.ExamResult == null ||
                string.IsNullOrWhiteSpace(answer.TextAnswer))
            {
                return;
            }

            // Only compare writing answers.
            if (!string.Equals(
                answer.Question.Skill,
                "Writing",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var targetWords = GetWords(answer.TextAnswer);

            // Very short answers are too unreliable to compare.
            if (targetWords.Count < 20)
                return;

            var targetShingles = GetShingles(targetWords);

            var candidates = await _context.ExamAnswers
                .AsNoTracking()
                .Include(a => a.ExamResult)
                .Where(a =>
                    a.QuestionId == answer.QuestionId &&
                    a.ExamAnswerId != answer.ExamAnswerId &&
                    a.ExamResult != null &&
                    a.ExamResult.StudentId != answer.ExamResult.StudentId &&
                    a.TextAnswer != null &&
                    a.TextAnswer != "")
                .ToListAsync(cancellationToken);

            decimal bestSimilarity = 0;
            int? matchedAnswerId = null;
            string? matchingText = null;

            foreach (var candidate in candidates)
            {
                var candidateWords = GetWords(candidate.TextAnswer!);

                if (candidateWords.Count < 20)
                    continue;

                var candidateShingles = GetShingles(candidateWords);

                if (candidateShingles.Count == 0 ||
                    targetShingles.Count == 0)
                {
                    continue;
                }

                var shared = targetShingles
                    .Intersect(candidateShingles)
                    .ToHashSet();

                // Require several matching phrases to reduce noise.
                if (shared.Count < 4)
                    continue;

                // Measures how much of the shorter answer's phrase set
                // is also found in the other answer.
                var denominator = Math.Min(
                    targetShingles.Count,
                    candidateShingles.Count);

                var similarity = Math.Round(
                    (decimal)shared.Count / denominator * 100,
                    2);

                if (similarity <= bestSimilarity)
                    continue;

                bestSimilarity = similarity;
                matchedAnswerId = candidate.ExamAnswerId;

                matchingText = string.Join(
                    " ... ",
                    shared.Take(5));
            }

            var review = await _context.WritingIntegrityReviews
                .FirstOrDefaultAsync(
                    r => r.ExamAnswerId == examAnswerId,
                    cancellationToken);

            if (review == null)
            {
                review = new WritingIntegrityReview
                {
                    ExamAnswerId = examAnswerId
                };

                _context.WritingIntegrityReviews.Add(review);
            }

            review.SimilarityPercent = bestSimilarity;
            review.MatchedExamAnswerId = matchedAnswerId;
            review.MatchingText = matchingText;
            review.CheckedAt = DateTime.Now;

            // Rechecking doesn't erase a teacher's completed decision.
            if (review.Status != "Reviewed" &&
                review.Status != "Cleared" &&
                review.Status != "Confirmed")
            {
                review.Status = bestSimilarity >= 35
                    ? "Pending"
                    : "NoMatch";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static List<string> GetWords(string text)
        {
            return Regex.Matches(
                    text.ToLowerInvariant(),
                    @"[\p{L}\p{N}]+")
                .Select(m => m.Value)
                .ToList();
        }

        private static HashSet<string> GetShingles(
            List<string> words)
        {
            var shingles = new HashSet<string>();

            for (int i = 0; i <= words.Count - 5; i++)
            {
                shingles.Add(
                    string.Join(" ", words.Skip(i).Take(5)));
            }

            return shingles;
        }
    }
}