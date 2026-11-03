using System.Threading.Channels;
using LanguageCenterManagement.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LanguageCenterManagement.Services.Ai
{
    public sealed class WritingAssessmentBackgroundService : BackgroundService
    {
        private readonly Channel<int> _queue =
            Channel.CreateUnbounded<int>();

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WritingAssessmentBackgroundService> _logger;

        public WritingAssessmentBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<WritingAssessmentBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public bool Enqueue(int examAnswerId)
        {
            return _queue.Writer.TryWrite(examAnswerId);
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await foreach (var answerId in
                _queue.Reader.ReadAllAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();

                var integrityService = scope.ServiceProvider
                    .GetRequiredService<WritingIntegrityService>();

                var aiAssessmentService = scope.ServiceProvider
                    .GetRequiredService<WritingAiAssessmentService>();

                try
                {
                    await integrityService.CheckAnswerAsync(
                        answerId,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Writing integrity check failed for answer {AnswerId}",
                        answerId);
                }

                try
                {
                    await aiAssessmentService.CheckAnswerAsync(
                        answerId,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Ollama writing assessment failed for answer {AnswerId}",
                        answerId);
                }
            }
        }
    }
}