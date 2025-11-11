# GitHub Actions Testing Guide

## 📊 Test Results Embedded in Commits

This repository includes a GitHub Actions workflow that automatically runs tests and embeds the results directly into each commit as `test-execution.log`.

## ✅ Demonstration Log Included

The file **`test-execution.log`** in this repository is a demonstration of what the workflow produces:

```
✅ All tests passed successfully

Test Statistics:
- Total Tests: 38
- Passed: 38
- Failed: 0
- Skipped: 0
- Duration: 2.2 seconds

Test Suites:
- PluginLoaderTests: 5 tests (5 passed)
- RefactoringContractsTests: 9 tests (9 passed)
- JobStatusTests: 14 tests (14 passed)
- RenameSymbolProviderTests: 10 tests (10 passed)
```

## 🚀 How to Use on a Real GitHub Repository

### Step 1: Push to GitHub

```bash
# Add GitHub as a remote (if not already done)
git remote add github https://github.com/YOUR_ORG/vbdotnet-refactor-mcp.git

# Push the code
git push github claude/vb-net-mass-refactoring-platform-011CV1uChA1bpEw7dUV3bBDc
```

### Step 2: The Workflow Runs Automatically

The workflow (`.github/workflows/test-and-log.yml`) will:

1. ⏱️ **Start** within 30 seconds of your push
2. 🏗️ **Setup** .NET 8.0 SDK
3. 📦 **Restore** all NuGet dependencies
4. 🔨 **Build** the solution in Release mode
5. 🧪 **Run** all 38 unit tests with detailed output
6. 📄 **Create** `test-execution.log` with complete results
7. ✏️ **Amend** your commit with the log file
8. 🚀 **Force push** the amended commit back

### Step 3: View the Results

```bash
# Wait 2-3 minutes for the workflow to complete

# Pull the amended commit
git pull github claude/vb-net-mass-refactoring-platform-011CV1uChA1bpEw7dUV3bBDc

# View the test results
cat test-execution.log

# Or view it on GitHub directly
```

## 📋 What Makes This Approach Special

### Traditional CI/CD Workflow
```
Developer pushes → CI runs → Results in separate dashboard →
Must navigate to CI system → Download logs/artifacts
```

### This Workflow
```
Developer pushes → CI runs → Results embedded in commit →
Just `git pull` → `cat test-execution.log`
```

## 🎯 Key Benefits

1. **No External Dependencies**: Results are in Git, not a CI dashboard
2. **Historical Record**: Every commit has its test results
3. **Offline Access**: Clone the repo, you have all test results
4. **Simple Debugging**: `git checkout <commit> && cat test-execution.log`
5. **No Artifacts**: No separate files to download or links to click

## 📁 Log File Structure

```
test-execution.log
├── Header Section
│   ├── Branch name
│   ├── Commit SHA
│   ├── Author
│   ├── Timestamp
│   └── Workflow run URL
├── Build Output
│   ├── Dependency restoration
│   ├── Project compilation
│   └── Build summary
├── Test Execution
│   ├── Individual test results
│   ├── Test suite breakdown
│   └── Pass/fail indicators
├── Test Summary
│   ├── Overall status (✅ or ❌)
│   ├── Statistics (total, passed, failed, skipped)
│   ├── Duration
│   └── Suite-by-suite breakdown
└── Environment Information
    ├── .NET SDK version
    ├── Runtime versions
    ├── Operating system
    └── Architecture
```

## 🔍 Example: Debugging a Failed Test

```bash
# A test failed in commit abc123
git checkout abc123

# View what failed
cat test-execution.log | grep -A 10 "Failed"

# See the full test output
cat test-execution.log

# Compare with previous passing commit
git checkout abc123~1
cat test-execution.log

# See what changed between them
git diff abc123~1 abc123
```

## ⚙️ Workflow Configuration

### Triggers

The workflow runs on push to:
- `claude/**` branches (all Claude-generated branches)
- `main` branch
- `develop` branch

### Customize Triggers

Edit `.github/workflows/test-and-log.yml`:

```yaml
on:
  push:
    branches:
      - 'main'
      - 'develop'
      - 'feature/**'  # Add your patterns
```

### Change Test Verbosity

```yaml
dotnet test tests/MCP.Tests/MCP.Tests.csproj \
  --verbosity minimal  # or normal, detailed, diagnostic
```

## 🛡️ Security & Safety

### Force Push Safety

The workflow uses `--force-with-lease` which:
- ✅ Allows push if you're the last person who pushed
- ❌ Rejects push if someone else has pushed (prevents overwrites)
- 🔒 Safer than `--force`

### Permissions

The workflow only has:
- `contents: write` - needed to amend commits
- Runs on GitHub's infrastructure
- No access to secrets unless explicitly granted

### Bot Identity

Commits are made by:
```
Author: github-actions[bot] <github-actions[bot]@users.noreply.github.com>
```

This clearly identifies automated commits.

## 🧪 Local Testing

To see what the workflow will produce locally:

```bash
# Run tests with detailed output
dotnet test tests/MCP.Tests/MCP.Tests.csproj \
  --configuration Release \
  --verbosity detailed \
  --logger "console;verbosity=detailed" \
  > local-test-output.log 2>&1

# View the output
cat local-test-output.log
```

## 📊 Monitoring the Workflow

### View Workflow Runs

```bash
# Using GitHub CLI
gh run list --branch YOUR_BRANCH

# View specific run
gh run view RUN_ID

# View logs in real-time
gh run watch
```

### Via GitHub Web UI

```
https://github.com/YOUR_ORG/YOUR_REPO/actions
```

## 🚨 Troubleshooting

### Workflow Not Running

**Check:**
1. Is GitHub Actions enabled for your repository?
2. Is the branch name pattern matched?
3. Are there any syntax errors in the YAML file?

```bash
# Validate YAML syntax
cat .github/workflows/test-and-log.yml | yq eval
```

### Commit Not Amended

**Possible causes:**
1. Workflow is still running (wait 2-3 minutes)
2. Tests failed before log could be created
3. Permissions issue

**Check workflow logs:**
```bash
gh run list --branch YOUR_BRANCH
gh run view LATEST_RUN_ID --log
```

### Force Push Rejected

**Cause:** Someone else pushed to the branch

**Solution:**
```bash
git fetch origin YOUR_BRANCH
git rebase origin/YOUR_BRANCH
git push
```

## 📚 Additional Resources

- **Workflow File**: `.github/workflows/test-and-log.yml`
- **Workflow Documentation**: `.github/workflows/README.md`
- **Unit Tests**: `tests/MCP.Tests/`
- **Main README**: `README.md`

## 🎓 Understanding the Workflow

### Why Amend Instead of a New Commit?

**Amending keeps history clean:**

Instead of:
```
abc123 - feat: Add new feature
def456 - Add test results for abc123
```

You get:
```
abc123 - feat: Add new feature [test-execution.log included]
```

### Why Force Push?

Amending changes the commit SHA, so a force push is required:

```
Before amend:  abc123 (without log)
After amend:   xyz789 (with log, same code)
```

The `--force-with-lease` ensures this is safe.

## 🎯 Next Steps

1. **Push to GitHub** to see the workflow in action
2. **Wait 2-3 minutes** for workflow completion
3. **Pull the amended commit** to see the results
4. **Check `test-execution.log`** for test output

Happy testing! 🚀
