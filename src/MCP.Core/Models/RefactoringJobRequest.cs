using System.Text.Json;

namespace MCP.Core.Models;

/// <summary>
/// Represents a refactoring job submission request.
/// This matches the API payload structure defined in Section 6.3 of the architectural blueprint.
/// </summary>
public class RefactoringJobRequest
{
    /// <summary>
    /// Path to the VB.NET solution file (.sln) to be refactored.
    /// Example: "vcs/my-legacy-app/MyLegacyApp.sln"
    /// </summary>
    public required string SolutionPath { get; init; }

    /// <summary>
    /// The name of the refactoring tool/plugin to execute.
    /// Must match the Name property of an IRefactoringProvider implementation.
    /// Examples: "RenameSymbol", "ExtractMethod", "ConvertForToForEach"
    /// </summary>
    public required string RefactoringToolName { get; init; }

    /// <summary>
    /// Tool-specific parameters as a JSON object.
    /// The structure depends on the specific refactoring tool being invoked.
    /// </summary>
    public required JsonElement Parameters { get; init; }

    /// <summary>
    /// Defines the validation and post-processing policy for this job.
    /// </summary>
    public ValidationPolicy ValidationPolicy { get; init; } = new();

    /// <summary>
    /// Optional: Configuration for triggering CI/CD pipeline validation.
    /// </summary>
    public CiPipelineTrigger? CiPipelineTrigger { get; init; }
}

/// <summary>
/// Defines the automated validation workflow and actions to take based on results.
/// Implements the "three-legged stool" safety guarantee from Section 3.4.
/// </summary>
public class ValidationPolicy
{
    /// <summary>
    /// Action to take when all validation steps succeed.
    /// Options: "CreatePullRequest", "MergeToBranch", "NotifyOnly"
    /// </summary>
    public string OnSuccess { get; init; } = "CreatePullRequest";

    /// <summary>
    /// Action to take when any validation step fails.
    /// Options: "DeleteBranch", "KeepBranch", "NotifyOnly"
    /// </summary>
    public string OnFailure { get; init; } = "DeleteBranch";

    /// <summary>
    /// Validation steps to execute in order.
    /// Default: ["Compile", "Test"]
    /// </summary>
    public List<string> Steps { get; init; } = new() { "Compile", "Test" };
}

/// <summary>
/// Configuration for triggering a CI/CD pipeline for test execution.
/// Supports Azure DevOps and Jenkins as specified in Section 8.3.
/// </summary>
public class CiPipelineTrigger
{
    /// <summary>
    /// Type of CI/CD system.
    /// Supported: "AzureDevOps", "Jenkins"
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Pipeline/Job identifier.
    /// For Azure DevOps: Pipeline ID (integer)
    /// For Jenkins: Job name (string)
    /// </summary>
    public required string PipelineId { get; init; }

    /// <summary>
    /// API endpoint URL for the CI/CD system.
    /// </summary>
    public string? ApiEndpoint { get; init; }

    /// <summary>
    /// Authentication token or API key.
    /// Should be stored securely and not logged.
    /// </summary>
    public string? AuthToken { get; init; }
}
