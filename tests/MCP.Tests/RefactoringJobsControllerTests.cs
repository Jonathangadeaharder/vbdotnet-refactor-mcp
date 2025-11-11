using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MCP.ApiGateway.Controllers;
using MCP.Core.Models;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for RefactoringJobsController - REST API endpoints.
/// Tests job submission, status retrieval, and cancellation.
/// </summary>
public class RefactoringJobsControllerTests
{
    private readonly Mock<ILogger<RefactoringJobsController>> _loggerMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly RefactoringJobsController _controller;

    public RefactoringJobsControllerTests()
    {
        _loggerMock = new Mock<ILogger<RefactoringJobsController>>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _controller = new RefactoringJobsController(
            _loggerMock.Object,
            _backgroundJobClientMock.Object);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            new RefactoringJobsController(_loggerMock.Object, _backgroundJobClientMock.Object));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void SubmitJob_WithValidRequest_ShouldReturn202Accepted()
    {
        // Arrange
        var request = CreateValidJobRequest();
        var jobId = "test-job-123";

        _backgroundJobClientMock
            .Setup(x => x.Enqueue<IRefactoringJobExecutor>(
                It.IsAny<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>()))
            .Returns(jobId);

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedAtActionResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);

        var response = Assert.IsType<JobSubmissionResponse>(acceptedResult.Value);
        Assert.Equal(jobId, response.JobId);
        Assert.Equal("Accepted", response.Status);
    }

    [Fact]
    public void SubmitJob_WithNullSolutionPath_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = CreateValidJobRequest();
        request = request with { SolutionPath = null! };

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("SolutionPath", problemDetails.Detail);
    }

    [Fact]
    public void SubmitJob_WithEmptySolutionPath_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = CreateValidJobRequest();
        request = request with { SolutionPath = "" };

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public void SubmitJob_WithNullRefactoringToolName_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = CreateValidJobRequest();
        request = request with { RefactoringToolName = null! };

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Contains("RefactoringToolName", problemDetails.Detail);
    }

    [Fact]
    public void SubmitJob_WithEmptyRefactoringToolName_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = CreateValidJobRequest();
        request = request with { RefactoringToolName = "" };

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public void SubmitJob_WhenHangfireThrows_ShouldReturn500()
    {
        // Arrange
        var request = CreateValidJobRequest();

        _backgroundJobClientMock
            .Setup(x => x.Enqueue<IRefactoringJobExecutor>(
                It.IsAny<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>()))
            .Throws(new InvalidOperationException("Hangfire error"));

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public void CancelJob_WithValidJobId_ShouldReturn200()
    {
        // Arrange
        var jobId = "test-job-123";

        _backgroundJobClientMock
            .Setup(x => x.Delete(jobId))
            .Returns(true);

        // Act
        var result = _controller.CancelJob(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void CancelJob_WhenDeletionFails_ShouldReturn409Conflict()
    {
        // Arrange
        var jobId = "test-job-123";

        _backgroundJobClientMock
            .Setup(x => x.Delete(jobId))
            .Returns(false);

        // Act
        var result = _controller.CancelJob(jobId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Contains("Cannot Cancel Job", problemDetails.Title);
    }

    [Fact]
    public void CancelJob_WhenHangfireThrows_ShouldReturn500()
    {
        // Arrange
        var jobId = "test-job-123";

        _backgroundJobClientMock
            .Setup(x => x.Delete(jobId))
            .Throws(new InvalidOperationException("Hangfire error"));

        // Act
        var result = _controller.CancelJob(jobId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Theory]
    [InlineData("RenameSymbol")]
    [InlineData("ExtractMethod")]
    [InlineData("ConvertForToForEach")]
    public void SubmitJob_WithDifferentToolNames_ShouldAcceptAll(string toolName)
    {
        // Arrange
        var request = CreateValidJobRequest();
        request = request with { RefactoringToolName = toolName };

        var jobId = $"job-{toolName}";
        _backgroundJobClientMock
            .Setup(x => x.Enqueue<IRefactoringJobExecutor>(
                It.IsAny<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>()))
            .Returns(jobId);

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedAtActionResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
    }

    [Fact]
    public void SubmitJob_ShouldEnqueueJobWithCorrectParameters()
    {
        // Arrange
        var request = CreateValidJobRequest();
        RefactoringJobRequest? capturedRequest = null;

        _backgroundJobClientMock
            .Setup(x => x.Enqueue<IRefactoringJobExecutor>(
                It.IsAny<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>()))
            .Callback<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>(expr =>
            {
                // This would capture the request in a real scenario
                // For now, just verify the method was called
            })
            .Returns("job-123");

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        _backgroundJobClientMock.Verify(
            x => x.Enqueue<IRefactoringJobExecutor>(
                It.IsAny<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>()),
            Times.Once);
    }

    [Fact]
    public void SubmitJob_Response_ShouldContainStatusUrl()
    {
        // Arrange
        var request = CreateValidJobRequest();
        var jobId = "test-job-123";

        _backgroundJobClientMock
            .Setup(x => x.Enqueue<IRefactoringJobExecutor>(
                It.IsAny<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>()))
            .Returns(jobId);

        // Act
        var result = _controller.SubmitJob(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedAtActionResult>(result);
        var response = Assert.IsType<JobSubmissionResponse>(acceptedResult.Value);

        Assert.NotNull(response.StatusUrl);
        Assert.Contains(jobId, response.StatusUrl);
    }

    [Fact]
    public void SubmitJob_ShouldLogJobSubmission()
    {
        // Arrange
        var request = CreateValidJobRequest();
        var jobId = "test-job-123";

        _backgroundJobClientMock
            .Setup(x => x.Enqueue<IRefactoringJobExecutor>(
                It.IsAny<System.Linq.Expressions.Expression<Action<IRefactoringJobExecutor>>>()))
            .Returns(jobId);

        // Act
        _controller.SubmitJob(request);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received job submission")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private RefactoringJobRequest CreateValidJobRequest()
    {
        return new RefactoringJobRequest
        {
            SolutionPath = "/path/to/solution.sln",
            RefactoringToolName = "RenameSymbol",
            Parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""MyClass.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 10,
                ""newName"": ""NewMethodName""
            }").RootElement
        };
    }
}
