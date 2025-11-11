using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MCP.Core.Services;

/// <summary>
/// Provides CI/CD pipeline integration for test validation.
///
/// Implements Stage 3 of the validation pipeline (Section 8.3 & 8.4):
/// - Triggers CI/CD builds for refactored code
/// - Polls for build/test results
/// - Supports Azure DevOps and Jenkins
///
/// This is the third leg of the "three-legged stool" safety guarantee.
/// </summary>
public class CiCdService
{
    private readonly ILogger<CiCdService> _logger;
    private readonly HttpClient _httpClient;

    public CiCdService(ILogger<CiCdService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Triggers a CI/CD build for the specified branch.
    /// </summary>
    /// <param name="config">CI/CD configuration</param>
    /// <param name="branchName">Branch to build</param>
    /// <returns>Build ID for polling</returns>
    public async Task<string> TriggerBuildAsync(CiCdConfiguration config, string branchName)
    {
        _logger.LogInformation(
            "Triggering {Type} build for branch '{Branch}'",
            config.Type,
            branchName);

        return config.Type.ToLowerInvariant() switch
        {
            "azuredevops" => await TriggerAzureDevOpsBuildAsync(config, branchName),
            "jenkins" => await TriggerJenkinsBuildAsync(config, branchName),
            _ => throw new NotSupportedException($"CI/CD type '{config.Type}' is not supported")
        };
    }

    /// <summary>
    /// Polls for the build result.
    /// </summary>
    /// <param name="config">CI/CD configuration</param>
    /// <param name="buildId">Build ID from TriggerBuildAsync</param>
    /// <returns>Build result</returns>
    public async Task<CiCdBuildResult> GetBuildResultAsync(CiCdConfiguration config, string buildId)
    {
        return config.Type.ToLowerInvariant() switch
        {
            "azuredevops" => await GetAzureDevOpsBuildResultAsync(config, buildId),
            "jenkins" => await GetJenkinsBuildResultAsync(config, buildId),
            _ => throw new NotSupportedException($"CI/CD type '{config.Type}' is not supported")
        };
    }

    #region Azure DevOps Implementation

    /// <summary>
    /// Triggers an Azure DevOps pipeline build.
    /// Reference: Section 8.3 of the architectural blueprint
    /// </summary>
    private async Task<string> TriggerAzureDevOpsBuildAsync(CiCdConfiguration config, string branchName)
    {
        // POST https://dev.azure.com/{org}/{project}/_apis/pipelines/{pipelineId}/runs?api-version=7.1
        var url = $"{config.ApiEndpoint}/_apis/pipelines/{config.PipelineId}/runs?api-version=7.1";

        var payload = new
        {
            resources = new
            {
                repositories = new
                {
                    self = new
                    {
                        refName = $"refs/heads/{branchName}"
                    }
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        // Azure DevOps uses PAT (Personal Access Token) authentication
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($":{config.AuthToken}")));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        var buildId = result.GetProperty("id").GetInt32().ToString();

        _logger.LogInformation("Azure DevOps build triggered: Build ID {BuildId}", buildId);

        return buildId;
    }

    /// <summary>
    /// Gets the result of an Azure DevOps build.
    /// Reference: Section 8.4 of the architectural blueprint
    /// </summary>
    private async Task<CiCdBuildResult> GetAzureDevOpsBuildResultAsync(CiCdConfiguration config, string buildId)
    {
        // GET https://dev.azure.com/{org}/{project}/_apis/build/builds/{buildId}?api-version=7.1
        var url = $"{config.ApiEndpoint}/_apis/build/builds/{buildId}?api-version=7.1";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($":{config.AuthToken}")));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        var status = result.GetProperty("status").GetString()!;
        var buildResult = result.TryGetProperty("result", out var resultProp)
            ? resultProp.GetString()
            : null;

        // Map Azure DevOps status to our result
        var isComplete = status.Equals("completed", StringComparison.OrdinalIgnoreCase);
        var isSuccess = buildResult?.Equals("succeeded", StringComparison.OrdinalIgnoreCase) == true;

        return new CiCdBuildResult
        {
            BuildId = buildId,
            IsComplete = isComplete,
            IsSuccess = isSuccess,
            Status = status,
            Result = buildResult,
            Url = result.TryGetProperty("_links", out var links) &&
                  links.TryGetProperty("web", out var web) &&
                  web.TryGetProperty("href", out var href)
                ? href.GetString()
                : null
        };
    }

    #endregion

    #region Jenkins Implementation

    /// <summary>
    /// Triggers a Jenkins job build.
    /// Reference: Section 8.3 of the architectural blueprint
    /// </summary>
    private async Task<string> TriggerJenkinsBuildAsync(CiCdConfiguration config, string branchName)
    {
        // POST http://{jenkins_url}/job/{jobName}/buildWithParameters?token={api_token}&BRANCH_NAME={branchName}
        var url = $"{config.ApiEndpoint}/job/{config.PipelineId}/buildWithParameters?" +
                  $"BRANCH_NAME={Uri.EscapeDataString(branchName)}";

        var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Jenkins uses Basic authentication with username:apiToken
        // The username can be extracted from the first part of the auth token
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            config.AuthToken!);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Jenkins returns the build location in the "Location" header
        var location = response.Headers.Location?.ToString();
        if (location == null)
        {
            throw new InvalidOperationException("Jenkins did not return a build location");
        }

        // Extract build number from the queue item
        // We need to poll the queue item to get the actual build number
        var queueItemUrl = $"{location}api/json";
        await Task.Delay(2000); // Give Jenkins a moment to queue the job

        var queueRequest = new HttpRequestMessage(HttpMethod.Get, queueItemUrl);
        queueRequest.Headers.Authorization = request.Headers.Authorization;

        var queueResponse = await _httpClient.SendAsync(queueRequest);
        queueResponse.EnsureSuccessStatusCode();

        var queueContent = await queueResponse.Content.ReadAsStringAsync();
        var queueResult = JsonSerializer.Deserialize<JsonElement>(queueContent);

        var buildNumber = queueResult.GetProperty("executable")
            .GetProperty("number")
            .GetInt32()
            .ToString();

        _logger.LogInformation("Jenkins build triggered: Build #{BuildNumber}", buildNumber);

        return buildNumber;
    }

    /// <summary>
    /// Gets the result of a Jenkins build.
    /// Reference: Section 8.4 of the architectural blueprint
    /// </summary>
    private async Task<CiCdBuildResult> GetJenkinsBuildResultAsync(CiCdConfiguration config, string buildNumber)
    {
        // GET http://{jenkins_url}/job/{jobName}/{buildNumber}/api/json
        var url = $"{config.ApiEndpoint}/job/{config.PipelineId}/{buildNumber}/api/json";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            config.AuthToken!);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        var building = result.GetProperty("building").GetBoolean();
        var buildResult = result.TryGetProperty("result", out var resultProp) && resultProp.ValueKind != JsonValueKind.Null
            ? resultProp.GetString()
            : null;

        var isComplete = !building;
        var isSuccess = buildResult?.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) == true;

        return new CiCdBuildResult
        {
            BuildId = buildNumber,
            IsComplete = isComplete,
            IsSuccess = isSuccess,
            Status = building ? "running" : "completed",
            Result = buildResult,
            Url = result.GetProperty("url").GetString()
        };
    }

    #endregion
}

/// <summary>
/// Configuration for a CI/CD system.
/// </summary>
public class CiCdConfiguration
{
    public required string Type { get; init; } // "AzureDevOps" or "Jenkins"
    public required string PipelineId { get; init; }
    public required string ApiEndpoint { get; init; }
    public required string AuthToken { get; init; }
}

/// <summary>
/// Result of a CI/CD build.
/// </summary>
public class CiCdBuildResult
{
    public required string BuildId { get; init; }
    public required bool IsComplete { get; init; }
    public required bool IsSuccess { get; init; }
    public required string Status { get; init; }
    public string? Result { get; init; }
    public string? Url { get; init; }
}
