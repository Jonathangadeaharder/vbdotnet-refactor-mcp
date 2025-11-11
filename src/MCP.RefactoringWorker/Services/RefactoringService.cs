using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using MCP.Contracts;
using MCP.Core.Models;

namespace MCP.RefactoringWorker.Services;

/// <summary>
/// Core refactoring service that orchestrates the transformation workflow.
///
/// Implements the Phase 1 "Core Engine PoC" from the architectural blueprint:
/// 1. Uses MSBuild.Locator to register MSBuild instance
/// 2. Uses MSBuildWorkspace to load VB.NET solutions
/// 3. Delegates to the appropriate IRefactoringProvider plugin
/// 4. Returns the transformed solution
///
/// This is called by Hangfire workers as the main job executor.
/// </summary>
public class RefactoringService : IRefactoringService
{
    private readonly ILogger<RefactoringService> _logger;
    private readonly PluginLoader _pluginLoader;
    private static bool _msbuildRegistered = false;
    private static readonly object _msbuildLock = new object();

    public RefactoringService(
        ILogger<RefactoringService> logger,
        PluginLoader pluginLoader)
    {
        _logger = logger;
        _pluginLoader = pluginLoader;

        // Ensure MSBuild is registered - this MUST happen before any MSBuildWorkspace is created
        // As per Section 4.2 of the architectural blueprint
        EnsureMSBuildRegistered();
    }

    private void EnsureMSBuildRegistered()
    {
        lock (_msbuildLock)
        {
            if (!_msbuildRegistered && !MSBuildLocator.IsRegistered)
            {
                _logger.LogInformation("Registering MSBuild instance...");

                // RegisterDefaults() finds the latest Visual Studio or .NET SDK installation
                var instance = MSBuildLocator.RegisterDefaults();

                _logger.LogInformation(
                    "MSBuild registered: {Name} {Version} at {Path}",
                    instance.Name,
                    instance.Version,
                    instance.MSBuildPath);

                _msbuildRegistered = true;
            }
        }
    }

    /// <summary>
    /// Executes a refactoring job asynchronously.
    /// This is the main entry point called by Hangfire workers.
    /// </summary>
    public async Task<RefactoringJobStatus> ExecuteJobAsync(
        RefactoringJobRequest request,
        string jobId,
        IProgress<string> progress,
        CancellationToken cancellationToken = default)
    {
        var log = new List<string>();
        var startTime = DateTime.UtcNow;

        try
        {
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Job started: {jobId}");
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Tool: {request.RefactoringToolName}");
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Solution: {request.SolutionPath}");

            // Step 1: Get the refactoring provider plugin
            progress.Report("Loading refactoring tool plugin...");
            var provider = _pluginLoader.GetProvider(request.RefactoringToolName);
            if (provider == null)
            {
                var errorMsg = $"Refactoring tool '{request.RefactoringToolName}' not found. " +
                              $"Available tools: {string.Join(", ", _pluginLoader.GetProviderNames())}";
                log.Add($"[{DateTime.UtcNow:HH:mm:ss}] ERROR: {errorMsg}");

                return new RefactoringJobStatus
                {
                    JobId = jobId,
                    Status = JobState.Failed,
                    Message = errorMsg,
                    CreatedAt = startTime,
                    UpdatedAt = DateTime.UtcNow,
                    ExecutionLog = log
                };
            }

            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Loaded provider: {provider.Name}");

            // Step 2: Validate parameters
            progress.Report("Validating parameters...");
            var validation = provider.ValidateParameters(request.Parameters);
            if (!validation.IsValid)
            {
                log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Parameter validation failed: {validation.ErrorMessage}");

                return new RefactoringJobStatus
                {
                    JobId = jobId,
                    Status = JobState.Failed,
                    Message = $"Parameter validation failed: {validation.ErrorMessage}",
                    CreatedAt = startTime,
                    UpdatedAt = DateTime.UtcNow,
                    ExecutionLog = log
                };
            }

            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Parameters validated successfully");

            // Step 3: Load the VB.NET solution using MSBuildWorkspace
            progress.Report($"Loading solution: {request.SolutionPath}");
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Loading solution with MSBuildWorkspace...");

            var solution = await LoadSolutionAsync(request.SolutionPath, log, cancellationToken);

            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Solution loaded. Projects: {solution.Projects.Count()}");

            // Step 4: Execute the refactoring
            progress.Report($"Executing refactoring: {provider.Name}");
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Executing refactoring transformation...");

            var context = new RefactoringContext(
                solution,
                request.Parameters,
                new ProgressLogger(log, progress),
                cancellationToken);

            var result = await provider.ExecuteAsync(context);

            if (!result.IsSuccess)
            {
                log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Refactoring failed: {result.ErrorMessage}");

                return new RefactoringJobStatus
                {
                    JobId = jobId,
                    Status = JobState.Failed,
                    Message = $"Refactoring transformation failed: {result.ErrorMessage}",
                    CreatedAt = startTime,
                    UpdatedAt = DateTime.UtcNow,
                    ExecutionLog = log
                };
            }

            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Refactoring transformation completed successfully");

            // Step 5: Apply changes to disk
            progress.Report("Applying changes to disk...");
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Writing transformed files to disk...");

            var changedFiles = await ApplyChangesAsync(result.TransformedSolution!, log, cancellationToken);

            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Modified {changedFiles} file(s)");
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Job completed successfully");

            return new RefactoringJobStatus
            {
                JobId = jobId,
                Status = JobState.Succeeded,
                Message = $"Refactoring completed successfully. Modified {changedFiles} file(s).",
                CreatedAt = startTime,
                UpdatedAt = DateTime.UtcNow,
                ExecutionLog = log
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed with exception", jobId);
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] EXCEPTION: {ex.Message}");
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Stack trace: {ex.StackTrace}");

            return new RefactoringJobStatus
            {
                JobId = jobId,
                Status = JobState.Failed,
                Message = $"Job failed with exception: {ex.Message}",
                CreatedAt = startTime,
                UpdatedAt = DateTime.UtcNow,
                ExecutionLog = log
            };
        }
    }

    /// <summary>
    /// Loads a VB.NET solution using MSBuildWorkspace.
    /// Implements the loading strategy from Section 4.2 of the blueprint.
    /// </summary>
    private async Task<Solution> LoadSolutionAsync(
        string solutionPath,
        List<string> log,
        CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create();

        // Subscribe to workspace diagnostics for debugging
        workspace.WorkspaceFailed += (sender, e) =>
        {
            _logger.LogWarning(
                "Workspace diagnostic: {Kind} - {Message}",
                e.Diagnostic.Kind,
                e.Diagnostic.Message);
            log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Workspace warning: {e.Diagnostic.Message}");
        };

        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        // Verify the solution loaded correctly
        if (!solution.Projects.Any())
        {
            throw new InvalidOperationException(
                $"Solution '{solutionPath}' was loaded but contains no projects. " +
                "Ensure Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll is present and " +
                "the solution file is valid.");
        }

        return solution;
    }

    /// <summary>
    /// Applies the transformed solution's changes to disk.
    /// </summary>
    private async Task<int> ApplyChangesAsync(
        Solution transformedSolution,
        List<string> log,
        CancellationToken cancellationToken)
    {
        int changedCount = 0;

        foreach (var projectChange in transformedSolution.GetChanges(transformedSolution).GetProjectChanges())
        {
            foreach (var documentId in projectChange.GetChangedDocuments())
            {
                var document = transformedSolution.GetDocument(documentId);
                if (document?.FilePath == null) continue;

                var text = await document.GetTextAsync(cancellationToken);
                await File.WriteAllTextAsync(document.FilePath, text.ToString(), cancellationToken);

                log.Add($"[{DateTime.UtcNow:HH:mm:ss}] Modified: {document.FilePath}");
                changedCount++;
            }
        }

        return changedCount;
    }

    /// <summary>
    /// Helper class to report progress to both log and IProgress.
    /// </summary>
    private class ProgressLogger : IProgress<string>
    {
        private readonly List<string> _log;
        private readonly IProgress<string> _progress;

        public ProgressLogger(List<string> log, IProgress<string> progress)
        {
            _log = log;
            _progress = progress;
        }

        public void Report(string value)
        {
            _log.Add($"[{DateTime.UtcNow:HH:mm:ss}] {value}");
            _progress.Report(value);
        }
    }
}

/// <summary>
/// Interface for the refactoring service.
/// Allows for dependency injection and testing.
/// </summary>
public interface IRefactoringService
{
    Task<RefactoringJobStatus> ExecuteJobAsync(
        RefactoringJobRequest request,
        string jobId,
        IProgress<string> progress,
        CancellationToken cancellationToken = default);
}
