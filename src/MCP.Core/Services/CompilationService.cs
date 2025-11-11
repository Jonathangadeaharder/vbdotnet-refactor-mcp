using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Extensions.Logging;

namespace MCP.Core.Services;

/// <summary>
/// Provides programmatic compilation using MSBuild.
///
/// Implements Stage 2 of the validation pipeline (Section 8.2):
/// - Compiles the refactored solution
/// - Captures build errors
/// - Returns success/failure status
///
/// This is the second leg of the "three-legged stool" safety guarantee.
/// </summary>
public class CompilationService
{
    private readonly Microsoft.Extensions.Logging.ILogger<CompilationService> _logger;

    public CompilationService(Microsoft.Extensions.Logging.ILogger<CompilationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compiles a solution file and returns the result.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file</param>
    /// <param name="configuration">Build configuration (Debug/Release)</param>
    /// <returns>Compilation result with success status and error log</returns>
    public async Task<CompilationResult> CompileSolutionAsync(
        string solutionPath,
        string configuration = "Release")
    {
        _logger.LogInformation(
            "Starting programmatic compilation of {SolutionPath} ({Configuration})",
            solutionPath,
            configuration);

        var errorLog = new List<string>();
        var warningLog = new List<string>();

        try
        {
            // Create build parameters
            var globalProperties = new Dictionary<string, string>
            {
                ["Configuration"] = configuration,
                ["Platform"] = "Any CPU"
            };

            // Create a custom logger to capture errors
            var buildLogger = new CustomBuildLogger(_logger, errorLog, warningLog);

            var buildParameters = new BuildParameters
            {
                Loggers = new Microsoft.Build.Framework.ILogger[] { buildLogger },
                MaxNodeCount = Environment.ProcessorCount,
                DetailedSummary = true
            };

            // Create the build request
            var buildRequest = new BuildRequestData(
                solutionPath,
                globalProperties,
                null,
                new[] { "Build" },
                null);

            // Execute the build
            _logger.LogInformation("Executing MSBuild...");

            var buildResult = await Task.Run(() =>
                BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest));

            var isSuccess = buildResult.OverallResult == BuildResultCode.Success;

            _logger.LogInformation(
                "Build completed with result: {Result}",
                buildResult.OverallResult);

            if (!isSuccess)
            {
                _logger.LogWarning(
                    "Build failed with {ErrorCount} error(s) and {WarningCount} warning(s)",
                    errorLog.Count,
                    warningLog.Count);
            }

            return new CompilationResult
            {
                IsSuccess = isSuccess,
                Errors = errorLog,
                Warnings = warningLog,
                BuildResultCode = buildResult.OverallResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compilation failed with exception");

            return new CompilationResult
            {
                IsSuccess = false,
                Errors = new List<string>
                {
                    $"Compilation exception: {ex.Message}",
                    ex.StackTrace ?? string.Empty
                },
                Warnings = new List<string>(),
                BuildResultCode = BuildResultCode.Failure
            };
        }
    }

    /// <summary>
    /// Custom MSBuild logger that captures errors and warnings.
    /// </summary>
    private class CustomBuildLogger : Microsoft.Build.Framework.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly List<string> _errors;
        private readonly List<string> _warnings;

        public CustomBuildLogger(
            Microsoft.Extensions.Logging.ILogger logger,
            List<string> errors,
            List<string> warnings)
        {
            _logger = logger;
            _errors = errors;
            _warnings = warnings;
        }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ErrorRaised += (sender, e) =>
            {
                var errorMsg = $"{e.File}({e.LineNumber},{e.ColumnNumber}): error {e.Code}: {e.Message}";
                _errors.Add(errorMsg);
                _logger.LogError("Build error: {Error}", errorMsg);
            };

            eventSource.WarningRaised += (sender, e) =>
            {
                var warningMsg = $"{e.File}({e.LineNumber},{e.ColumnNumber}): warning {e.Code}: {e.Message}";
                _warnings.Add(warningMsg);
                _logger.LogWarning("Build warning: {Warning}", warningMsg);
            };
        }

        public void Shutdown()
        {
        }

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Normal;
        public string? Parameters { get; set; }
    }
}

/// <summary>
/// Result of a compilation operation.
/// </summary>
public class CompilationResult
{
    public required bool IsSuccess { get; init; }
    public required List<string> Errors { get; init; }
    public required List<string> Warnings { get; init; }
    public required BuildResultCode BuildResultCode { get; init; }

    public string GetSummary()
    {
        if (IsSuccess)
        {
            return $"Build succeeded with {Warnings.Count} warning(s)";
        }

        return $"Build failed with {Errors.Count} error(s) and {Warnings.Count} warning(s)";
    }
}
