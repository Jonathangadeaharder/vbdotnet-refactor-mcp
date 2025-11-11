using MCP.RefactoringWorker.Services;

namespace MCP.RefactoringWorker;

/// <summary>
/// Background worker service that processes refactoring jobs from the Hangfire queue.
///
/// This implements the "RefactoringWorker" component of the SOA architecture
/// described in Section 2.3 of the blueprint. This service:
/// - Runs as a Hangfire server
/// - Picks up enqueued jobs from the persistent queue
/// - Executes CPU-intensive Roslyn transformations
/// - Can be scaled horizontally based on queue depth
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefactoringWorker service started at: {time}", DateTimeOffset.Now);

        try
        {
            // The Hangfire server runs in the background automatically
            // This worker just keeps the service alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RefactoringWorker service is stopping");
        }

        _logger.LogInformation("RefactoringWorker service stopped at: {time}", DateTimeOffset.Now);
    }
}
