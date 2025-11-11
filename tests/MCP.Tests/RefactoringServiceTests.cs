using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using MCP.Contracts;
using MCP.Core.Models;
using MCP.RefactoringWorker;
using MCP.RefactoringWorker.Services;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for RefactoringService - Core refactoring engine.
/// Tests solution loading, plugin execution, and error handling.
/// </summary>
public class RefactoringServiceTests
{
    private readonly Mock<ILogger<RefactoringService>> _loggerMock;
    private readonly Mock<PluginLoader> _pluginLoaderMock;
    private readonly RefactoringService _service;

    public RefactoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<RefactoringService>>();
        var pluginLoggerMock = new Mock<ILogger<PluginLoader>>();
        _pluginLoaderMock = new Mock<PluginLoader>(pluginLoggerMock.Object);
        _service = new RefactoringService(_loggerMock.Object, _pluginLoaderMock.Object);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            new RefactoringService(_loggerMock.Object, _pluginLoaderMock.Object));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ExecuteJobAsync_WithNonExistentTool_ShouldReturnFailedStatus()
    {
        // Arrange
        var request = CreateTestJobRequest("NonExistentTool");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        _pluginLoaderMock
            .Setup(x => x.GetProvider("NonExistentTool"))
            .Returns((IRefactoringProvider?)null);

        _pluginLoaderMock
            .Setup(x => x.GetProviderNames())
            .Returns(new[] { "RenameSymbol", "ExtractMethod" });

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.Equal(JobState.Failed, result.Status);
        Assert.Contains("not found", result.Message);
        Assert.Contains("NonExistentTool", result.Message);
    }

    [Fact]
    public async Task ExecuteJobAsync_WithInvalidParameters_ShouldReturnFailedStatus()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        var mockProvider = new Mock<IRefactoringProvider>();
        mockProvider.Setup(x => x.Name).Returns("RenameSymbol");
        mockProvider
            .Setup(x => x.ValidateParameters(It.IsAny<JsonElement>()))
            .Returns(ValidationResult.Failure("Missing required parameter"));

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns(mockProvider.Object);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.Equal(JobState.Failed, result.Status);
        Assert.Contains("Parameter validation failed", result.Message);
    }

    [Fact]
    public async Task ExecuteJobAsync_WithValidProvider_ShouldLoadPlugin()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        var mockProvider = new Mock<IRefactoringProvider>();
        mockProvider.Setup(x => x.Name).Returns("RenameSymbol");
        mockProvider
            .Setup(x => x.ValidateParameters(It.IsAny<JsonElement>()))
            .Returns(ValidationResult.Success());

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns(mockProvider.Object);

        // Act
        // Will fail at solution loading, but proves plugin was loaded
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        _pluginLoaderMock.Verify(x => x.GetProvider("RenameSymbol"), Times.Once);
        mockProvider.Verify(x => x.ValidateParameters(It.IsAny<JsonElement>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_ShouldLogJobStart()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns((IRefactoringProvider?)null);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.Contains("Job started", result.ExecutionLog);
        Assert.Contains(jobId, result.ExecutionLog[0]);
    }

    [Fact]
    public async Task ExecuteJobAsync_ShouldLogToolName()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns((IRefactoringProvider?)null);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.Contains(result.ExecutionLog, log => log.Contains("Tool: RenameSymbol"));
    }

    [Fact]
    public async Task ExecuteJobAsync_ShouldLogSolutionPath()
    {
        // Arrange
        var solutionPath = "/test/solution.sln";
        var request = CreateTestJobRequest("RenameSymbol", solutionPath);
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns((IRefactoringProvider?)null);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.Contains(result.ExecutionLog, log => log.Contains(solutionPath));
    }

    [Fact]
    public async Task ExecuteJobAsync_WhenProviderThrows_ShouldReturnFailedStatus()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        var mockProvider = new Mock<IRefactoringProvider>();
        mockProvider.Setup(x => x.Name).Returns("RenameSymbol");
        mockProvider
            .Setup(x => x.ValidateParameters(It.IsAny<JsonElement>()))
            .Throws(new InvalidOperationException("Provider error"));

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns(mockProvider.Object);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.Equal(JobState.Failed, result.Status);
        Assert.Contains("exception", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provider error", result.ExecutionLog.Last());
    }

    [Fact]
    public async Task ExecuteJobAsync_ShouldIncludeTimestamps()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns((IRefactoringProvider?)null);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.All(result.ExecutionLog, log =>
        {
            // Each log entry should have a timestamp in format [HH:mm:ss]
            Assert.Matches(@"\[\d{2}:\d{2}:\d{2}\]", log);
        });
    }

    [Fact]
    public async Task ExecuteJobAsync_ShouldSetCreatedAndUpdatedTimestamps()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();
        var beforeExecution = DateTime.UtcNow;

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns((IRefactoringProvider?)null);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);
        var afterExecution = DateTime.UtcNow;

        // Assert
        Assert.InRange(result.CreatedAt, beforeExecution, afterExecution);
        Assert.NotNull(result.UpdatedAt);
        Assert.InRange(result.UpdatedAt.Value, beforeExecution, afterExecution);
    }

    [Theory]
    [InlineData("RenameSymbol")]
    [InlineData("ExtractMethod")]
    [InlineData("ConvertForToForEach")]
    public async Task ExecuteJobAsync_WithDifferentToolNames_ShouldAttemptToLoad(string toolName)
    {
        // Arrange
        var request = CreateTestJobRequest(toolName);
        var jobId = $"job-{toolName}";
        var progress = new Progress<string>();

        _pluginLoaderMock
            .Setup(x => x.GetProvider(toolName))
            .Returns((IRefactoringProvider?)null);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        _pluginLoaderMock.Verify(x => x.GetProvider(toolName), Times.Once);
        Assert.Contains(toolName, result.ExecutionLog[1]); // Tool name in second log entry
    }

    [Fact]
    public async Task ExecuteJobAsync_ShouldHaveExecutionLogInResult()
    {
        // Arrange
        var request = CreateTestJobRequest("RenameSymbol");
        var jobId = "test-job-123";
        var progress = new Progress<string>();

        _pluginLoaderMock
            .Setup(x => x.GetProvider("RenameSymbol"))
            .Returns((IRefactoringProvider?)null);

        // Act
        var result = await _service.ExecuteJobAsync(request, jobId, progress);

        // Assert
        Assert.NotNull(result.ExecutionLog);
        Assert.NotEmpty(result.ExecutionLog);
        Assert.True(result.ExecutionLog.Count >= 3); // At least: start, tool, solution path
    }

    private RefactoringJobRequest CreateTestJobRequest(
        string toolName = "RenameSymbol",
        string solutionPath = "/test/solution.sln")
    {
        return new RefactoringJobRequest
        {
            SolutionPath = solutionPath,
            RefactoringToolName = toolName,
            Parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""MyClass.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 10,
                ""newName"": ""NewName""
            }").RootElement
        };
    }
}
