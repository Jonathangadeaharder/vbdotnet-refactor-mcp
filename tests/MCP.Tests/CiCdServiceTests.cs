using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MCP.Core.Services;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for CiCdService - Stage 3 of the safety guarantee.
/// Tests CI/CD pipeline integration for Azure DevOps and Jenkins.
/// </summary>
public class CiCdServiceTests
{
    private readonly Mock<ILogger<CiCdService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly CiCdService _service;

    public CiCdServiceTests()
    {
        _loggerMock = new Mock<ILogger<CiCdService>>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);
        _service = new CiCdService(_loggerMock.Object, _httpClient);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            new CiCdService(_loggerMock.Object, _httpClient));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task TriggerBuildAsync_WithUnsupportedType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "UnsupportedCI",
            PipelineId = "123",
            ApiEndpoint = "https://example.com",
            AuthToken = "token"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.TriggerBuildAsync(config, "test-branch"));
    }

    [Fact]
    public async Task GetBuildResultAsync_WithUnsupportedType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "UnsupportedCI",
            PipelineId = "123",
            ApiEndpoint = "https://example.com",
            AuthToken = "token"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.GetBuildResultAsync(config, "build-123"));
    }

    [Fact]
    public async Task TriggerBuildAsync_AzureDevOps_WithSuccessfulResponse_ShouldReturnBuildId()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "AzureDevOps",
            PipelineId = "123",
            ApiEndpoint = "https://dev.azure.com/org/project",
            AuthToken = "test-token"
        };

        var responseJson = @"{""id"": 456}";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var buildId = await _service.TriggerBuildAsync(config, "feature/test");

        // Assert
        Assert.Equal("456", buildId);
    }

    [Fact]
    public async Task GetBuildResultAsync_AzureDevOps_WithCompletedSuccessfulBuild_ShouldReturnSuccess()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "AzureDevOps",
            PipelineId = "123",
            ApiEndpoint = "https://dev.azure.com/org/project",
            AuthToken = "test-token"
        };

        var responseJson = @"{
            ""status"": ""completed"",
            ""result"": ""succeeded"",
            ""_links"": {
                ""web"": {
                    ""href"": ""https://dev.azure.com/build/456""
                }
            }
        }";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.GetBuildResultAsync(config, "456");

        // Assert
        Assert.True(result.IsComplete);
        Assert.True(result.IsSuccess);
        Assert.Equal("completed", result.Status);
        Assert.Equal("succeeded", result.Result);
    }

    [Fact]
    public async Task GetBuildResultAsync_AzureDevOps_WithFailedBuild_ShouldReturnFailure()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "AzureDevOps",
            PipelineId = "123",
            ApiEndpoint = "https://dev.azure.com/org/project",
            AuthToken = "test-token"
        };

        var responseJson = @"{
            ""status"": ""completed"",
            ""result"": ""failed""
        }";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.GetBuildResultAsync(config, "456");

        // Assert
        Assert.True(result.IsComplete);
        Assert.False(result.IsSuccess);
        Assert.Equal("failed", result.Result);
    }

    [Fact]
    public async Task GetBuildResultAsync_AzureDevOps_WithInProgressBuild_ShouldReturnIncomplete()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "AzureDevOps",
            PipelineId = "123",
            ApiEndpoint = "https://dev.azure.com/org/project",
            AuthToken = "test-token"
        };

        var responseJson = @"{
            ""status"": ""inProgress""
        }";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.GetBuildResultAsync(config, "456");

        // Assert
        Assert.False(result.IsComplete);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task TriggerBuildAsync_Jenkins_WithSuccessfulResponse_ShouldReturnBuildNumber()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "Jenkins",
            PipelineId = "MyJob",
            ApiEndpoint = "https://jenkins.example.com",
            AuthToken = "Basic dGVzdDp0b2tlbg=="
        };

        // Mock the trigger response (returns location)
        var triggerResponse = new HttpResponseMessage(HttpStatusCode.Created);
        triggerResponse.Headers.Location = new Uri("https://jenkins.example.com/queue/item/789");

        // Mock the queue item response
        var queueJson = @"{""executable"": {""number"": 123}}";
        var queueResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(queueJson)
        };

        _httpHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(triggerResponse)
            .ReturnsAsync(queueResponse);

        // Act
        var buildNumber = await _service.TriggerBuildAsync(config, "feature/test");

        // Assert
        Assert.Equal("123", buildNumber);
    }

    [Fact]
    public async Task GetBuildResultAsync_Jenkins_WithSuccessfulBuild_ShouldReturnSuccess()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "Jenkins",
            PipelineId = "MyJob",
            ApiEndpoint = "https://jenkins.example.com",
            AuthToken = "Basic dGVzdDp0b2tlbg=="
        };

        var responseJson = @"{
            ""building"": false,
            ""result"": ""SUCCESS"",
            ""url"": ""https://jenkins.example.com/job/MyJob/123""
        }";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.GetBuildResultAsync(config, "123");

        // Assert
        Assert.True(result.IsComplete);
        Assert.True(result.IsSuccess);
        Assert.Equal("SUCCESS", result.Result);
        Assert.NotNull(result.Url);
    }

    [Fact]
    public async Task GetBuildResultAsync_Jenkins_WithFailedBuild_ShouldReturnFailure()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "Jenkins",
            PipelineId = "MyJob",
            ApiEndpoint = "https://jenkins.example.com",
            AuthToken = "Basic dGVzdDp0b2tlbg=="
        };

        var responseJson = @"{
            ""building"": false,
            ""result"": ""FAILURE"",
            ""url"": ""https://jenkins.example.com/job/MyJob/123""
        }";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.GetBuildResultAsync(config, "123");

        // Assert
        Assert.True(result.IsComplete);
        Assert.False(result.IsSuccess);
        Assert.Equal("FAILURE", result.Result);
    }

    [Fact]
    public async Task GetBuildResultAsync_Jenkins_WithBuildInProgress_ShouldReturnIncomplete()
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = "Jenkins",
            PipelineId = "MyJob",
            ApiEndpoint = "https://jenkins.example.com",
            AuthToken = "Basic dGVzdDp0b2tlbg=="
        };

        var responseJson = @"{
            ""building"": true,
            ""result"": null,
            ""url"": ""https://jenkins.example.com/job/MyJob/123""
        }";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _service.GetBuildResultAsync(config, "123");

        // Assert
        Assert.False(result.IsComplete);
        Assert.False(result.IsSuccess);
        Assert.Equal("running", result.Status);
    }

    [Theory]
    [InlineData("azuredevops")]
    [InlineData("AZUREDEVOPS")]
    [InlineData("AzureDevOps")]
    public async Task TriggerBuildAsync_AzureDevOps_CaseInsensitive_ShouldWork(string ciType)
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = ciType,
            PipelineId = "123",
            ApiEndpoint = "https://dev.azure.com/org/project",
            AuthToken = "token"
        };

        var responseJson = @"{""id"": 456}";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var buildId = await _service.TriggerBuildAsync(config, "test-branch");

        // Assert
        Assert.NotNull(buildId);
    }

    [Theory]
    [InlineData("jenkins")]
    [InlineData("JENKINS")]
    [InlineData("Jenkins")]
    public async Task TriggerBuildAsync_Jenkins_CaseInsensitive_ShouldWork(string ciType)
    {
        // Arrange
        var config = new CiCdConfiguration
        {
            Type = ciType,
            PipelineId = "MyJob",
            ApiEndpoint = "https://jenkins.example.com",
            AuthToken = "token"
        };

        var triggerResponse = new HttpResponseMessage(HttpStatusCode.Created);
        triggerResponse.Headers.Location = new Uri("https://jenkins.example.com/queue/item/789");

        var queueJson = @"{""executable"": {""number"": 123}}";
        var queueResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(queueJson)
        };

        _httpHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(triggerResponse)
            .ReturnsAsync(queueResponse);

        // Act
        var buildNumber = await _service.TriggerBuildAsync(config, "test-branch");

        // Assert
        Assert.NotNull(buildNumber);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        };

        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
