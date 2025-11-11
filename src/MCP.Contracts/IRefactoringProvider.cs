using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace MCP.Contracts;

/// <summary>
/// Defines the core contract for an MCP Refactoring "Tool" plugin.
/// Each "tool" implements this interface to be discovered and executed
/// by the MCP RefactoringWorker host.
///
/// As specified in the architectural blueprint Section 7, this interface
/// forms the foundation of the extensible plugin system.
/// </summary>
public interface IRefactoringProvider
{
    /// <summary>
    /// The unique, machine-readable name of the tool.
    /// This MUST match the 'refactoringToolName' in the API payload.
    /// Examples: "RenameSymbol", "ExtractMethod", "ConvertForToForEach"
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A human-readable description of what this refactoring tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Validates the tool-specific 'parameters' block from the API request.
    /// This is called by the host before ExecuteAsync.
    /// </summary>
    /// <param name="parameters">The JsonElement from the API payload.</param>
    /// <returns>A result object indicating success or failure with errors.</returns>
    ValidationResult ValidateParameters(JsonElement parameters);

    /// <summary>
    /// Executes the refactoring transformation using Roslyn.
    /// This is the core logic of the tool.
    ///
    /// Implementation Notes:
    /// - Use Roslyn's semantic model for safe transformations
    /// - Leverage SpeculativeSemanticModel for pre-flight validation
    /// - For renames, use the built-in Renamer API with conflict detection
    /// </summary>
    /// <param name="context">The execution context containing the
    /// loaded Solution and tool-specific parameters.</param>
    /// <returns>A result object containing the new, transformed
    /// Solution or a detailed error message if transformation failed.</returns>
    Task<RefactoringResult> ExecuteAsync(RefactoringContext context);
}

/// <summary>
/// Context object passed to the plugin by the host worker.
/// Contains all necessary information for executing a refactoring.
/// </summary>
public class RefactoringContext
{
    public RefactoringContext(
        Solution originalSolution,
        JsonElement parameters,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        OriginalSolution = originalSolution ?? throw new ArgumentNullException(nameof(originalSolution));
        Parameters = parameters;
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The original Solution loaded from the VB.NET .sln file via MSBuildWorkspace.
    /// </summary>
    public Solution OriginalSolution { get; }

    /// <summary>
    /// The tool-specific parameters from the API request payload.
    /// Each plugin interprets this JSON according to its own schema.
    /// </summary>
    public JsonElement Parameters { get; }

    /// <summary>
    /// Progress reporter for sending status updates back to the job queue.
    /// </summary>
    public IProgress<string> Progress { get; }

    /// <summary>
    /// Cancellation token for cooperative cancellation.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}

/// <summary>
/// The object returned by the plugin to the host worker after ExecuteAsync.
/// </summary>
public class RefactoringResult
{
    private RefactoringResult(bool isSuccess, Solution? transformedSolution, string? errorMessage)
    {
        IsSuccess = isSuccess;
        TransformedSolution = transformedSolution;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Indicates whether the refactoring transformation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The transformed Solution with all changes applied.
    /// Null if IsSuccess is false.
    /// </summary>
    public Solution? TransformedSolution { get; }

    /// <summary>
    /// Detailed error message if the transformation failed.
    /// Null if IsSuccess is true.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful refactoring result.
    /// </summary>
    public static RefactoringResult Success(Solution transformedSolution)
    {
        if (transformedSolution == null)
            throw new ArgumentNullException(nameof(transformedSolution));

        return new RefactoringResult(true, transformedSolution, null);
    }

    /// <summary>
    /// Creates a failed refactoring result.
    /// </summary>
    public static RefactoringResult Failure(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

        return new RefactoringResult(false, null, errorMessage);
    }
}

/// <summary>
/// Result of parameter validation.
/// </summary>
public class ValidationResult
{
    private ValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Indicates whether the parameters are valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Error message explaining why validation failed.
    /// Null if IsValid is true.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new ValidationResult(true, null);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ValidationResult Failure(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

        return new ValidationResult(false, errorMessage);
    }
}
