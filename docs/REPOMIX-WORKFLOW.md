# Repomix CI/CD Workflow

## Overview

This repository includes a GitHub Actions workflow that automatically generates a combined codebase file using [Repomix](https://github.com/yamadashy/repomix) and publishes it as a downloadable artifact.

## What is Repomix?

Repomix is a tool that combines your entire codebase into a single file, making it easy to:
- Share your codebase with AI assistants (Claude, ChatGPT, etc.)
- Create snapshots of your project at specific points in time
- Analyze your codebase structure and content
- Archive your code in a human-readable format

## Workflow Details

### Trigger Events

The workflow runs automatically on:
- **Push events** to branches:
  - `claude/**` (feature branches)
  - `main`
  - `develop`
- **Pull requests** targeting:
  - `main`
  - `develop`
- **Manual trigger** via workflow_dispatch (you can run it manually from GitHub Actions UI)

### What the Workflow Does

1. **Checks out the code** - Gets the latest version of your repository
2. **Sets up Node.js** - Installs Node.js 20 (required for Repomix)
3. **Installs Repomix** - Globally installs the latest version of Repomix
4. **Generates combined codebase** - Runs Repomix with the configuration from `repomix.config.json`
5. **Uploads artifact** - Publishes the combined codebase file as a GitHub Actions artifact
6. **Creates summary** - Generates a summary with file size, line count, and download link

### Configuration

The workflow uses the `repomix.config.json` file in the root directory to customize the output.

**Note:** The example below shows the key settings from the actual configuration file. See `repomix.config.json` in the repository root for the complete configuration.

```json
{
  "output": {
    "filePath": "repomix-output/codebase-combined.txt",
    "style": "plain",
    "headerText": "VB.NET to .NET MCP Server - Complete Codebase",
    "showLineNumbers": true
  },
  "include": [
    "**/*.cs",
    "**/*.csproj",
    "**/*.sln",
    "**/*.md",
    "**/*.yml",
    "**/*.yaml",
    "**/*.json",
    "**/*.xml",
    "**/*.config",
    "Dockerfile",
    ".dockerignore",
    ".gitignore"
  ],
  "ignore": {
    "useGitignore": true,
    "useDefaultPatterns": true,
    "customPatterns": [
      "**/bin/**",
      "**/obj/**",
      "**/node_modules/**",
      "**/.vs/**",
      "**/.vscode/**",
      "**/packages/**",
      "**/*.dll",
      "**/*.exe",
      "**/*.pdb",
      "**/*.user",
      "**/.git/**",
      "**/test-execution.log",
      "**/repomix-output/**"
    ]
  },
  "security": {
    "enableSecurityCheck": true
  }
}
```

**Security Note:** The configuration intentionally excludes `docker-compose.yml` to prevent accidentally including configuration files that may contain credentials or sensitive settings.

### Artifact Details

- **Artifact name**: `repomix-codebase-{commit-sha}`
- **File name**: `codebase-combined.txt`
- **Output path**: `repomix-output/codebase-combined.txt`
- **Retention**: 30 days
- **Compression**: Level 9 (maximum compression)

## How to Use

### Downloading the Artifact

1. Go to the **Actions** tab in your GitHub repository
2. Click on the **Repomix Codebase Artifact** workflow
3. Select a workflow run (usually the latest one)
4. Scroll down to the **Artifacts** section
5. Click on `repomix-codebase-{commit-sha}` to download

### Running Manually

You can trigger the workflow manually:

1. Go to the **Actions** tab
2. Click on **Repomix Codebase Artifact** in the left sidebar
3. Click the **Run workflow** button
4. Select the branch you want to run it on
5. Click **Run workflow**

### Using the Combined Codebase

The generated file can be used for:

1. **AI Assistant Context**: Copy and paste the entire codebase into Claude, ChatGPT, or other AI assistants
2. **Code Review**: Share a complete snapshot with reviewers
3. **Documentation**: Create a reference document of your codebase
4. **Archival**: Keep versioned snapshots of your project

## Customizing the Configuration

### Including/Excluding Files

Edit `repomix.config.json` to customize which files are included:

```json
{
  "include": [
    "**/*.cs",
    "**/*.md"
  ],
  "ignore": {
    "customPatterns": [
      "**/bin/**",
      "**/TestData/**"
    ]
  }
}
```

### Output Format

You can change the output style:

- `markdown` - Formatted with markdown code blocks
- `xml` - Structured XML format
- `plain` - Plain text format

### Security

The configuration includes security checks to prevent accidentally including sensitive data:

```json
{
  "security": {
    "enableSecurityCheck": true
  }
}
```

This will warn you if files containing potential secrets are detected.

## Workflow File Location

The workflow is defined in:
```
.github/workflows/repomix-artifact.yml
```

## Troubleshooting

### Workflow Not Running

- Check that your branch name matches the trigger patterns
- Ensure GitHub Actions is enabled for your repository
- Check the workflow permissions in repository settings

### Artifact Too Large

If the artifact is too large (>500MB):

1. Update `repomix.config.json` to exclude more files
2. Add build outputs and large files to the ignore patterns
3. Consider splitting into multiple artifacts

### Missing Files

If expected files are missing from the output:

1. Check the `include` patterns in `repomix.config.json`
2. Verify files aren't in `.gitignore`
3. Check the `customPatterns` in the ignore configuration

## Resources

- [Repomix GitHub Repository](https://github.com/yamadashy/repomix)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [GitHub Actions Artifacts](https://docs.github.com/en/actions/using-workflows/storing-workflow-data-as-artifacts)

## Example Output

When the workflow runs successfully, you'll see a summary like:

```
📦 Repomix Codebase Artifact

Branch: claude/feature-branch
Commit: abc123...
File Size: 2.5M
Line Count: 45,231

The combined codebase has been generated and uploaded as an artifact.
```

You can then download the artifact from the workflow run page.
