using System.Text.Json;
using MCP.Contracts;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for the refactoring contracts and models.
/// Tests the IRefactoringProvider interface contract implementations.
/// </summary>
public class RefactoringContractsTests
{
    [Fact]
    public void RefactoringResult_Success_ShouldCreateSuccessResult()
    {
        // Arrange
        var solution = CreateTestSolution();

        // Act
        var result = RefactoringResult.Success(solution);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.TransformedSolution);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void RefactoringResult_Failure_ShouldCreateFailureResult()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = RefactoringResult.Failure(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.TransformedSolution);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public void RefactoringResult_Success_WithNullSolution_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RefactoringResult.Success(null!));
    }

    [Fact]
    public void RefactoringResult_Failure_WithNullMessage_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            RefactoringResult.Failure(null!));
    }

    [Fact]
    public void ValidationResult_Success_ShouldCreateSuccessResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidationResult_Failure_ShouldCreateFailureResult()
    {
        // Arrange
        var errorMessage = "Validation failed";

        // Act
        var result = ValidationResult.Failure(errorMessage);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public void RefactoringContext_Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var solution = CreateTestSolution();
        var parameters = JsonDocument.Parse("{}").RootElement;
        var progress = new Progress<string>();
        var cancellationToken = CancellationToken.None;

        // Act
        var context = new RefactoringContext(
            solution,
            parameters,
            progress,
            cancellationToken);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(solution, context.OriginalSolution);
        Assert.Equal(parameters, context.Parameters);
        Assert.Equal(progress, context.Progress);
        Assert.Equal(cancellationToken, context.CancellationToken);
    }

    [Fact]
    public void RefactoringContext_Constructor_WithNullSolution_ShouldThrow()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{}").RootElement;
        var progress = new Progress<string>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RefactoringContext(null!, parameters, progress, CancellationToken.None));
    }

    [Fact]
    public void RefactoringContext_Constructor_WithNullProgress_ShouldThrow()
    {
        // Arrange
        var solution = CreateTestSolution();
        var parameters = JsonDocument.Parse("{}").RootElement;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RefactoringContext(solution, parameters, null!, CancellationToken.None));
    }

    private static Solution CreateTestSolution()
    {
        var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "TestProject",
            "TestProject",
            LanguageNames.VisualBasic);

        return workspace.AddProject(projectInfo).Solution;
    }
}
