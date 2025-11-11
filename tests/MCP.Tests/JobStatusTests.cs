using MCP.Core.Models;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for job status models and state transitions.
/// </summary>
public class JobStatusTests
{
    [Fact]
    public void JobState_ShouldHaveAllExpectedStates()
    {
        // Assert - verify all states from the architectural blueprint exist
        Assert.True(Enum.IsDefined(typeof(JobState), JobState.Pending));
        Assert.True(Enum.IsDefined(typeof(JobState), JobState.Running));
        Assert.True(Enum.IsDefined(typeof(JobState), JobState.Compiling));
        Assert.True(Enum.IsDefined(typeof(JobState), JobState.Testing));
        Assert.True(Enum.IsDefined(typeof(JobState), JobState.Succeeded));
        Assert.True(Enum.IsDefined(typeof(JobState), JobState.Failed));
        Assert.True(Enum.IsDefined(typeof(JobState), JobState.Cancelled));
    }

    [Fact]
    public void RefactoringJobStatus_Initialization_ShouldSetProperties()
    {
        // Arrange
        var jobId = "test-job-123";
        var status = JobState.Running;
        var message = "Processing refactoring...";
        var createdAt = DateTime.UtcNow;

        // Act
        var jobStatus = new RefactoringJobStatus
        {
            JobId = jobId,
            Status = status,
            Message = message,
            CreatedAt = createdAt,
            ExecutionLog = new List<string> { "Log entry 1" }
        };

        // Assert
        Assert.Equal(jobId, jobStatus.JobId);
        Assert.Equal(status, jobStatus.Status);
        Assert.Equal(message, jobStatus.Message);
        Assert.Equal(createdAt, jobStatus.CreatedAt);
        Assert.Single(jobStatus.ExecutionLog);
    }

    [Theory]
    [InlineData(JobState.Pending, "Job is queued")]
    [InlineData(JobState.Running, "Executing refactoring")]
    [InlineData(JobState.Compiling, "Compiling solution")]
    [InlineData(JobState.Testing, "Running tests")]
    [InlineData(JobState.Succeeded, "Completed successfully")]
    [InlineData(JobState.Failed, "Failed")]
    [InlineData(JobState.Cancelled, "Cancelled by user")]
    public void RefactoringJobStatus_WithDifferentStates_ShouldStoreCorrectly(
        JobState state,
        string message)
    {
        // Act
        var jobStatus = new RefactoringJobStatus
        {
            JobId = "test",
            Status = state,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(state, jobStatus.Status);
        Assert.Equal(message, jobStatus.Message);
    }

    [Fact]
    public void ValidationPolicy_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var policy = new ValidationPolicy();

        // Assert
        Assert.Equal("CreatePullRequest", policy.OnSuccess);
        Assert.Equal("DeleteBranch", policy.OnFailure);
        Assert.Contains("Compile", policy.Steps);
        Assert.Contains("Test", policy.Steps);
    }

    [Fact]
    public void RefactoringJobRequest_ShouldAcceptAllRequiredFields()
    {
        // Arrange & Act
        var request = new RefactoringJobRequest
        {
            SolutionPath = "/path/to/solution.sln",
            RefactoringToolName = "RenameSymbol",
            Parameters = System.Text.Json.JsonDocument.Parse("{}").RootElement
        };

        // Assert
        Assert.Equal("/path/to/solution.sln", request.SolutionPath);
        Assert.Equal("RenameSymbol", request.RefactoringToolName);
        Assert.NotNull(request.ValidationPolicy);
    }

    [Fact]
    public void CiPipelineTrigger_ShouldSupportMultipleTypes()
    {
        // Arrange & Act
        var azureDevOps = new CiPipelineTrigger
        {
            Type = "AzureDevOps",
            PipelineId = "123",
            ApiEndpoint = "https://dev.azure.com/org/project",
            AuthToken = "token"
        };

        var jenkins = new CiPipelineTrigger
        {
            Type = "Jenkins",
            PipelineId = "MyJob",
            ApiEndpoint = "https://jenkins.example.com",
            AuthToken = "token"
        };

        // Assert
        Assert.Equal("AzureDevOps", azureDevOps.Type);
        Assert.Equal("Jenkins", jenkins.Type);
        Assert.NotEqual(azureDevOps.PipelineId, jenkins.PipelineId);
    }
}
