# MCP.ValidationWorker

## Overview

Background worker that validates refactoring results through the "three-legged stool" safety guarantee: pre-flight semantic validation, post-flight compilation, and CI/CD test execution.

## Responsibilities

1. **Git Operations**: Create branches, commit changes, push to remote
2. **Compilation Validation**: Run MSBuild programmatically to verify code compiles
3. **CI/CD Integration**: Trigger test pipelines in Azure DevOps or Jenkins
4. **Result Reporting**: Update job status based on validation outcomes
5. **Cleanup**: Delete branches on failure per validation policy

## Validation Pipeline

```
┌─────────────────────────────────────────┐
│  Stage 1: Pre-Flight (Already Done)     │
│  ✓ Roslyn semantic validation           │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│  Stage 2: Post-Flight Compilation       │
│  • Create Git branch                    │
│  • Commit transformed files             │
│  • Run MSBuild                          │
│  • Verify zero errors                   │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│  Stage 3: CI/CD Test Execution          │
│  • Push branch to remote                │
│  • Trigger CI pipeline                  │
│  • Wait for test results                │
│  • Verify all tests pass                │
└─────────────────────────────────────────┘
```

## Validation Policies

Configured per job request:

```csharp
validationPolicy: {
  onSuccess: "CreatePullRequest",  // or "MergeToMain", "DeleteBranch"
  onFailure: "DeleteBranch",       // or "KeepBranch"
  steps: ["Compile", "Test"]       // or ["Compile"] for faster feedback
}
```

## Configuration

See `appsettings.json`:
- Git credentials and remotes
- CI/CD API endpoints and tokens
- MSBuild paths and options

## Testing

See `tests/MCP.Tests/CompilationServiceTests.cs`, `GitServiceTests.cs`, and `CiCdServiceTests.cs`
