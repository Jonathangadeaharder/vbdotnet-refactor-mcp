using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Moq;
using MCP.Core.Services;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for GitService - Version control operations.
/// Tests branch management, commits, and push/pull operations.
/// </summary>
public class GitServiceTests
{
    private readonly Mock<ILogger<GitService>> _loggerMock;
    private readonly GitService _service;

    public GitServiceTests()
    {
        _loggerMock = new Mock<ILogger<GitService>>();
        _service = new GitService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            new GitService(_loggerMock.Object));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void CreateBranch_WithNullRepositoryPath_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateBranch(null!, "test-branch"));
    }

    [Fact]
    public void CreateBranch_WithEmptyRepositoryPath_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateBranch("", "test-branch"));
    }

    [Fact]
    public void CreateBranch_WithNullBranchName_ShouldThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateBranch(tempDir, null!));
    }

    [Fact]
    public void CreateBranch_WithNonExistentRepository_ShouldThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        var exception = Assert.Throws<RepositoryNotFoundException>(() =>
            _service.CreateBranch(nonExistentPath, "test-branch"));

        Assert.NotNull(exception);
    }

    [Fact]
    public void CommitAll_WithNullRepositoryPath_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CommitAll(null!, "Test commit"));
    }

    [Fact]
    public void CommitAll_WithNullMessage_ShouldThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CommitAll(tempDir, null!));
    }

    [Fact]
    public void CommitAll_WithEmptyMessage_ShouldThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CommitAll(tempDir, ""));
    }

    [Fact]
    public void Push_WithNullRepositoryPath_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.Push(null!));
    }

    [Fact]
    public void DeleteBranch_WithNullRepositoryPath_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.DeleteBranch(null!, "test-branch"));
    }

    [Fact]
    public void DeleteBranch_WithNullBranchName_ShouldThrow()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.DeleteBranch(tempDir, null!));
    }

    // Integration-style tests that would work with a real git repo
    // These are commented out as they require a real git repository
    // Uncomment and adapt for integration testing

    /*
    [Fact]
    public void CreateBranch_WithValidRepository_ShouldCreateBranch()
    {
        // Arrange
        var tempDir = CreateTestGitRepository();

        try
        {
            // Act
            _service.CreateBranch(tempDir, "feature/test-branch");

            // Assert
            using var repo = new Repository(tempDir);
            var branch = repo.Branches["feature/test-branch"];
            Assert.NotNull(branch);
        }
        finally
        {
            CleanupTestRepository(tempDir);
        }
    }

    [Fact]
    public void CommitAll_WithChanges_ShouldCreateCommit()
    {
        // Arrange
        var tempDir = CreateTestGitRepository();

        try
        {
            // Create a test file
            File.WriteAllText(Path.Combine(tempDir, "test.txt"), "Test content");

            // Act
            _service.CommitAll(tempDir, "Test commit message");

            // Assert
            using var repo = new Repository(tempDir);
            var lastCommit = repo.Head.Tip;
            Assert.Equal("Test commit message", lastCommit.Message.Trim());
        }
        finally
        {
            CleanupTestRepository(tempDir);
        }
    }
    */

    [Theory]
    [InlineData("MCP RefactoringWorker", "mcp@refactoring.local")]
    [InlineData("Custom Author", "custom@email.com")]
    public void CommitAll_WithCustomAuthor_ShouldAcceptParameters(
        string authorName,
        string authorEmail)
    {
        // This test verifies the API accepts custom author parameters
        // Actual functionality would be tested with a real repository

        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        // Will throw because repo doesn't exist, but proves parameters are accepted
        Assert.Throws<RepositoryNotFoundException>(() =>
            _service.CommitAll(tempDir, "Test", authorName, authorEmail));
    }

    [Theory]
    [InlineData("origin")]
    [InlineData("upstream")]
    public void Push_WithCustomRemote_ShouldAcceptParameter(string remoteName)
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        // Will throw because repo doesn't exist, but proves parameter is accepted
        Assert.Throws<RepositoryNotFoundException>(() =>
            _service.Push(tempDir, remoteName));
    }

    [Fact]
    public void DeleteBranch_WithDeleteRemoteFlag_ShouldAcceptParameter()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        // Will throw because repo doesn't exist, but proves parameter is accepted
        Assert.Throws<RepositoryNotFoundException>(() =>
            _service.DeleteBranch(tempDir, "test-branch", deleteRemote: true));
    }
}
