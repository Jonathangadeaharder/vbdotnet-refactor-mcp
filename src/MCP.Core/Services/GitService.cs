using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace MCP.Core.Services;

/// <summary>
/// Provides Git operations for the validation workflow.
///
/// Implements Stage 1 of the validation pipeline (Section 8.1):
/// - Creates temporary branches for refactored code
/// - Commits changes
/// - Pushes to remote for CI/CD validation
/// </summary>
public class GitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new branch for the refactoring job.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="branchName">Name of the branch to create (e.g., mcp/job-123-rename-foo)</param>
    /// <param name="baseBranch">Base branch to branch from (default: main)</param>
    public void CreateBranch(string repositoryPath, string branchName, string baseBranch = "main")
    {
        using var repo = new Repository(repositoryPath);

        _logger.LogInformation(
            "Creating branch '{BranchName}' from '{BaseBranch}' in {RepoPath}",
            branchName,
            baseBranch,
            repositoryPath);

        // Ensure we're on the base branch
        var baseBranchRef = repo.Branches[baseBranch];
        if (baseBranchRef == null)
        {
            throw new InvalidOperationException($"Base branch '{baseBranch}' not found");
        }

        // Create the new branch
        var branch = repo.CreateBranch(branchName, baseBranchRef.Tip);

        // Checkout the new branch
        Commands.Checkout(repo, branch);

        _logger.LogInformation("Branch '{BranchName}' created and checked out", branchName);
    }

    /// <summary>
    /// Commits all changes in the repository.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="message">Commit message</param>
    /// <param name="authorName">Author name</param>
    /// <param name="authorEmail">Author email</param>
    public void CommitAll(
        string repositoryPath,
        string message,
        string authorName = "MCP RefactoringWorker",
        string authorEmail = "mcp@refactoring.local")
    {
        using var repo = new Repository(repositoryPath);

        _logger.LogInformation("Staging all changes in {RepoPath}", repositoryPath);

        // Stage all changes
        Commands.Stage(repo, "*");

        // Create the commit
        var author = new Signature(authorName, authorEmail, DateTimeOffset.Now);
        var committer = author;

        var commit = repo.Commit(message, author, committer);

        _logger.LogInformation(
            "Committed changes: {CommitSha} - {Message}",
            commit.Sha.Substring(0, 8),
            message);
    }

    /// <summary>
    /// Pushes the current branch to the remote repository.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="remoteName">Name of the remote (default: origin)</param>
    /// <param name="credentials">Optional credentials for authentication</param>
    public void Push(
        string repositoryPath,
        string remoteName = "origin",
        UsernamePasswordCredentials? credentials = null)
    {
        using var repo = new Repository(repositoryPath);

        var currentBranch = repo.Head;

        _logger.LogInformation(
            "Pushing branch '{BranchName}' to remote '{RemoteName}'",
            currentBranch.FriendlyName,
            remoteName);

        var remote = repo.Network.Remotes[remoteName];
        if (remote == null)
        {
            throw new InvalidOperationException($"Remote '{remoteName}' not found");
        }

        var pushOptions = new PushOptions();

        if (credentials != null)
        {
            pushOptions.CredentialsProvider = (url, user, cred) =>
                new UsernamePasswordCredentials
                {
                    Username = credentials.Username,
                    Password = credentials.Password
                };
        }

        repo.Network.Push(currentBranch, pushOptions);

        _logger.LogInformation("Branch '{BranchName}' pushed successfully", currentBranch.FriendlyName);
    }

    /// <summary>
    /// Deletes a branch (used when validation fails).
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository</param>
    /// <param name="branchName">Name of the branch to delete</param>
    /// <param name="deleteRemote">Whether to delete the remote branch as well</param>
    public void DeleteBranch(
        string repositoryPath,
        string branchName,
        bool deleteRemote = false,
        UsernamePasswordCredentials? credentials = null)
    {
        using var repo = new Repository(repositoryPath);

        _logger.LogInformation("Deleting branch '{BranchName}'", branchName);

        // Delete local branch
        var branch = repo.Branches[branchName];
        if (branch != null)
        {
            repo.Branches.Remove(branch);
            _logger.LogInformation("Local branch '{BranchName}' deleted", branchName);
        }

        // Delete remote branch if requested
        if (deleteRemote)
        {
            var pushOptions = new PushOptions();

            if (credentials != null)
            {
                pushOptions.CredentialsProvider = (url, user, cred) =>
                    new UsernamePasswordCredentials
                    {
                        Username = credentials.Username,
                        Password = credentials.Password
                    };
            }

            var remote = repo.Network.Remotes["origin"];
            repo.Network.Push(remote, $":refs/heads/{branchName}", pushOptions);

            _logger.LogInformation("Remote branch '{BranchName}' deleted", branchName);
        }
    }
}
