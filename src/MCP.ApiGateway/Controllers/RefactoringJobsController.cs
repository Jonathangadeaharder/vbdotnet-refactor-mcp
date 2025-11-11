using Hangfire;
using Microsoft.AspNetCore.Mvc;
using MCP.Core.Models;

namespace MCP.ApiGateway.Controllers;

/// <summary>
/// RESTful API controller for managing refactoring jobs.
///
/// Implements the asynchronous job pattern from Section 6.2 of the blueprint:
/// - POST /api/v1/jobs - Submit a new refactoring job (returns 202 Accepted)
/// - GET /api/v1/jobs/{jobId} - Poll for job status
/// - DELETE /api/v1/jobs/{jobId} - Cancel a pending job
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class RefactoringJobsController : ControllerBase
{
    private readonly ILogger<RefactoringJobsController> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public RefactoringJobsController(
        ILogger<RefactoringJobsController> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Submits a new refactoring job for asynchronous processing.
    /// </summary>
    /// <param name="request">The refactoring job request payload</param>
    /// <returns>202 Accepted with Location header pointing to status endpoint</returns>
    [HttpPost]
    [ProducesResponseType(typeof(JobSubmissionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult SubmitJob([FromBody] RefactoringJobRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Received job submission: Tool={Tool}, Solution={Solution}",
                request.RefactoringToolName,
                request.SolutionPath);

            // Basic validation
            if (string.IsNullOrWhiteSpace(request.SolutionPath))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "SolutionPath is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.RefactoringToolName))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "RefactoringToolName is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Enqueue the job with Hangfire
            // The RefactoringWorker service will pick this up
            var jobId = _backgroundJobClient.Enqueue<IRefactoringJobExecutor>(
                executor => executor.ExecuteAsync(request, JobCancellationToken.Null));

            _logger.LogInformation("Job enqueued with ID: {JobId}", jobId);

            var response = new JobSubmissionResponse
            {
                JobId = jobId,
                Status = "Accepted",
                Message = "Job has been accepted and queued for processing",
                StatusUrl = Url.Action(nameof(GetJobStatus), new { jobId }, Request.Scheme)!
            };

            // Return 202 Accepted with Location header
            return AcceptedAtAction(
                nameof(GetJobStatus),
                new { jobId },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to submit job. Please try again later.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets the current status of a refactoring job.
    /// </summary>
    /// <param name="jobId">The Hangfire job ID</param>
    /// <returns>Current job status</returns>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(RefactoringJobStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetJobStatus(string jobId)
    {
        try
        {
            // Query Hangfire for the job status
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var jobDetails = monitoringApi.JobDetails(jobId);

            if (jobDetails == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Job Not Found",
                    Detail = $"No job found with ID: {jobId}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Map Hangfire state to our JobState enum
            var status = MapHangfireState(jobDetails.History[0].StateName);

            var response = new RefactoringJobStatus
            {
                JobId = jobId,
                Status = status,
                Message = GetStatusMessage(status, jobDetails),
                CreatedAt = jobDetails.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = jobDetails.History[0].CreatedAt,
                ExecutionLog = new List<string>() // TODO: Retrieve from storage
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for {JobId}", jobId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to retrieve job status",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Attempts to cancel a pending or running job.
    /// </summary>
    /// <param name="jobId">The Hangfire job ID</param>
    /// <returns>200 OK if cancellation was successful</returns>
    [HttpDelete("{jobId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public IActionResult CancelJob(string jobId)
    {
        try
        {
            var deleted = _backgroundJobClient.Delete(jobId);

            if (!deleted)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Cannot Cancel Job",
                    Detail = "Job may have already completed or is in a state that cannot be cancelled",
                    Status = StatusCodes.Status409Conflict
                });
            }

            _logger.LogInformation("Job {JobId} cancelled", jobId);

            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "Failed to cancel job",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    private JobState MapHangfireState(string hangfireState)
    {
        return hangfireState.ToLowerInvariant() switch
        {
            "enqueued" => JobState.Pending,
            "processing" => JobState.Running,
            "succeeded" => JobState.Succeeded,
            "failed" => JobState.Failed,
            "deleted" => JobState.Cancelled,
            _ => JobState.Pending
        };
    }

    private string GetStatusMessage(JobState status, Hangfire.Common.JobDetailsDto jobDetails)
    {
        return status switch
        {
            JobState.Pending => "Job is queued and waiting for a worker",
            JobState.Running => "Job is currently being processed",
            JobState.Succeeded => "Job completed successfully",
            JobState.Failed => $"Job failed: {jobDetails.History[0].Reason}",
            JobState.Cancelled => "Job was cancelled",
            _ => "Unknown status"
        };
    }
}

/// <summary>
/// Response model for job submission.
/// </summary>
public class JobSubmissionResponse
{
    public required string JobId { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
    public required string StatusUrl { get; init; }
}

/// <summary>
/// Interface for the job executor.
/// This is what Hangfire will call on the RefactoringWorker.
/// </summary>
public interface IRefactoringJobExecutor
{
    Task ExecuteAsync(RefactoringJobRequest request, CancellationToken cancellationToken);
}
