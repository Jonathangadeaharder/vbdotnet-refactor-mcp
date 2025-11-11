namespace MCP.Core.Models;

/// <summary>
/// Represents the current status of a refactoring job.
/// Used in the asynchronous job pattern (Section 6.2) for GET /api/v1/jobs/{jobId}.
/// </summary>
public class RefactoringJobStatus
{
    /// <summary>
    /// Unique identifier for the job (Hangfire job ID).
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Current state of the job.
    /// </summary>
    public required JobState Status { get; init; }

    /// <summary>
    /// Human-readable message describing the current state or any errors.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// URL to access the result (e.g., Pull Request URL, Git branch URL).
    /// Populated when Status is Succeeded.
    /// </summary>
    public string? ResultUrl { get; init; }

    /// <summary>
    /// Timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the job last changed state.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Detailed log of the job's execution.
    /// </summary>
    public List<string> ExecutionLog { get; init; } = new();
}

/// <summary>
/// Enumeration of possible job states.
/// Follows the lifecycle from submission through validation.
/// </summary>
public enum JobState
{
    /// <summary>
    /// Job has been accepted but not yet picked up by a worker.
    /// </summary>
    Pending,

    /// <summary>
    /// Worker is executing the refactoring transformation (Roslyn phase).
    /// </summary>
    Running,

    /// <summary>
    /// Transformation complete, worker is compiling the modified code.
    /// Stage 2 of the validation workflow (Section 8.2).
    /// </summary>
    Compiling,

    /// <summary>
    /// Compilation succeeded, worker is running CI/CD tests.
    /// Stage 3 of the validation workflow (Section 8.3).
    /// </summary>
    Testing,

    /// <summary>
    /// All validation stages passed. Refactoring is certified as safe.
    /// </summary>
    Succeeded,

    /// <summary>
    /// One or more validation stages failed. Refactoring was unsafe.
    /// Details available in Message and ExecutionLog.
    /// </summary>
    Failed,

    /// <summary>
    /// Job was cancelled by the user or system.
    /// </summary>
    Cancelled
}
