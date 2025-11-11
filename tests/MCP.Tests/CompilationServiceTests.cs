using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using MCP.Core.Services;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for CompilationService - Stage 2 of the safety guarantee.
/// Tests programmatic MSBuild compilation and error capture.
/// </summary>
public class CompilationServiceTests
{
    private readonly Mock<ILogger<CompilationService>> _loggerMock;
    private readonly CompilationService _service;

    public CompilationServiceTests()
    {
        _loggerMock = new Mock<ILogger<CompilationService>>();
        _service = new CompilationService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            new CompilationService(_loggerMock.Object));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task CompileSolutionAsync_WithNullPath_ShouldHandleGracefully()
    {
        // Act
        var result = await _service.CompileSolutionAsync(null!);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task CompileSolutionAsync_WithEmptyPath_ShouldHandleGracefully()
    {
        // Act
        var result = await _service.CompileSolutionAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task CompileSolutionAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".sln");

        // Act
        var result = await _service.CompileSolutionAsync(nonExistentPath);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal(BuildResultCode.Failure, result.BuildResultCode);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task CompileSolutionAsync_WithDifferentConfigurations_ShouldAcceptConfiguration(
        string configuration)
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test.sln");

        // Act
        var result = await _service.CompileSolutionAsync(testPath, configuration);

        // Assert
        Assert.NotNull(result);
        // Will fail because file doesn't exist, but proves configuration is accepted
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CompilationResult_GetSummary_WithSuccess_ShouldReturnSuccessMessage()
    {
        // Arrange
        var result = new CompilationResult
        {
            IsSuccess = true,
            Errors = new List<string>(),
            Warnings = new List<string> { "Warning 1", "Warning 2" },
            BuildResultCode = BuildResultCode.Success
        };

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("succeeded", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2", summary); // warning count
    }

    [Fact]
    public void CompilationResult_GetSummary_WithFailure_ShouldReturnFailureMessage()
    {
        // Arrange
        var result = new CompilationResult
        {
            IsSuccess = false,
            Errors = new List<string> { "Error 1", "Error 2", "Error 3" },
            Warnings = new List<string> { "Warning 1" },
            BuildResultCode = BuildResultCode.Failure
        };

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("failed", summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("3", summary); // error count
        Assert.Contains("1", summary); // warning count
    }

    [Fact]
    public void CompilationResult_WithNoErrors_ShouldIndicateSuccess()
    {
        // Arrange
        var result = new CompilationResult
        {
            IsSuccess = true,
            Errors = new List<string>(),
            Warnings = new List<string>(),
            BuildResultCode = BuildResultCode.Success
        };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(BuildResultCode.Success, result.BuildResultCode);
    }

    [Fact]
    public void CompilationResult_WithErrors_ShouldContainErrorDetails()
    {
        // Arrange
        var errorMessage = "CS0001: Syntax error";
        var result = new CompilationResult
        {
            IsSuccess = false,
            Errors = new List<string> { errorMessage },
            Warnings = new List<string>(),
            BuildResultCode = BuildResultCode.Failure
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Contains(errorMessage, result.Errors);
    }

    [Fact]
    public void CompilationResult_WithWarnings_ShouldContainWarningDetails()
    {
        // Arrange
        var warningMessage = "CS0168: Variable declared but never used";
        var result = new CompilationResult
        {
            IsSuccess = true,
            Errors = new List<string>(),
            Warnings = new List<string> { warningMessage },
            BuildResultCode = BuildResultCode.Success
        };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains(warningMessage, result.Warnings);
    }
}
