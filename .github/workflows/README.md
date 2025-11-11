# GitHub Actions Workflows

## Test and Log Results

This workflow automatically runs tests on every push to `claude/**`, `main`, or `develop` branches and embeds the test results directly into the commit.

### How It Works

1. **Triggers**: Runs on push to specified branches
2. **Executes Tests**: Runs `dotnet test` with detailed verbosity
3. **Captures Output**: Writes all test output to `test-execution.log`
4. **Amends Commit**: Adds the log file and amends the pushed commit
5. **Force Pushes**: Uses `--force-with-lease` to update the remote branch

### Log File Format

The `test-execution.log` file includes:

- **Header Section**: Branch, commit SHA, author, timestamp, workflow run link
- **Build Output**: Complete build and test execution output
- **Test Summary**: Pass/fail status with exit codes
- **Environment Info**: .NET SDK version, runtime versions

### Viewing Test Results

After a push, simply check the log file in your commit:

```bash
git pull
cat test-execution.log
```

Or view it on GitHub:
```
https://github.com/YOUR_ORG/YOUR_REPO/blob/BRANCH_NAME/test-execution.log
```

### Example Log Output

```
=====================================================================
MCP Test Execution Report
=====================================================================

Branch: claude/my-feature-branch
Commit: abc123def456
Author: developer-name
Timestamp: 2025-01-15 14:30:00 UTC
Workflow Run: https://github.com/org/repo/actions/runs/12345

=====================================================================
BUILD OUTPUT
=====================================================================

Determining projects to restore...
Restored /workspace/src/MCP.Core/MCP.Core.csproj
...

Test run for /workspace/tests/MCP.Tests/MCP.Tests.csproj (.NETCoreApp,Version=v8.0)
Microsoft (R) Test Execution Command Line Tool Version 17.8.0
...

Passed! - Failed:     0, Passed:    25, Skipped:     0, Total:    25

=====================================================================
TEST SUMMARY
=====================================================================

✅ All tests passed successfully

=====================================================================
```

### Benefits of This Approach

1. **No Separate Artifacts**: Test results are part of the commit itself
2. **Easy Debugging**: Just pull and read the log file
3. **History Tracking**: Each commit contains its own test results
4. **No External Services**: No need for test reporting tools
5. **Instant Feedback**: See results immediately after push

### Workflow Configuration

The workflow uses:
- **ubuntu-latest** runner
- **.NET 8.0 SDK**
- **continue-on-error**: Tests failures don't prevent log creation
- **force-with-lease**: Safe force push that prevents accidental overwrites

### Customization

To customize the workflow:

1. **Change Trigger Branches**: Edit the `on.push.branches` section
2. **Adjust Test Verbosity**: Modify the `--verbosity` parameter
3. **Add More Checks**: Add steps before the test execution
4. **Change Log Format**: Edit the log generation commands

### Security Notes

- Uses `GITHUB_TOKEN` with `contents: write` permission
- Force pushes are limited to the same branch (no cross-branch overwrites)
- Bot account is used for Git operations (github-actions[bot])

### Troubleshooting

**Issue**: Commit not amended
- **Cause**: Log file unchanged from previous run
- **Solution**: Workflow skips amendment if no changes detected

**Issue**: Force push rejected
- **Cause**: Branch has diverged (someone else pushed)
- **Solution**: `--force-with-lease` protects against this; pull and re-push

**Issue**: Tests fail but commit still succeeds
- **Cause**: `continue-on-error: true` allows workflow to complete
- **Solution**: Check the final step - it will fail the workflow if tests failed

### Local Testing

To test the workflow logic locally:

```bash
# Run tests and capture output
dotnet test tests/MCP.Tests/MCP.Tests.csproj \
  --verbosity detailed \
  --logger "console;verbosity=detailed" \
  > test-execution.log 2>&1

# View the log
cat test-execution.log
```

### Integration with CI/CD

This workflow complements (not replaces) the MCP's built-in CI/CD integration:

- **This Workflow**: Quick feedback for development branches
- **MCP Validation**: Full integration tests for refactored code

Use this workflow for fast iteration during development, and the MCP platform's CI/CD integration for comprehensive validation of refactorings.
