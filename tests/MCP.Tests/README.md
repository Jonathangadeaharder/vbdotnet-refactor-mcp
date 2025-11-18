# MCP.Tests

## Overview

Comprehensive test suite for the Mass Code Platform, covering all components with unit tests, integration tests, and end-to-end workflow validation.

## Test Organization

```
MCP.Tests/
├── Unit Tests
│   ├── RefactoringServiceTests.cs
│   ├── PluginLoaderTests.cs
│   ├── GitServiceTests.cs
│   ├── CompilationServiceTests.cs
│   ├── CiCdServiceTests.cs
│   ├── JobStatusTests.cs
│   ├── RefactoringContractsTests.cs
│   ├── RefactoringJobsControllerTests.cs
│   └── RenameSymbolProviderTests.cs
│
└── Integration Tests
    └── Integration/
        └── EndToEndWorkflowTests.cs
```

## Test Coverage

Current coverage: **80+ tests** across all major components

### Core Services (MCP.Core)
- ✅ GitService: Branch creation, commits, push operations
- ✅ CompilationService: MSBuild integration, error detection
- ✅ CiCdService: Pipeline triggers, status polling

### Workers
- ✅ RefactoringWorker: Plugin loading, solution loading, transformation execution
- ✅ ValidationWorker: Compilation validation, Git workflows

### API Gateway
- ✅ RefactoringJobsController: Request validation, job submission, status endpoints

### Plugins
- ✅ RenameSymbolProvider: Renaming logic, conflict detection

### Integration
- ✅ End-to-End: Full workflow from job submission to validation

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~RefactoringServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Patterns

### Unit Tests
Use mocking for external dependencies (Git, MSBuild, CI/CD APIs)

### Integration Tests
Use real file system and actual VB.NET test projects

### Naming Convention
- `MethodName_Scenario_ExpectedBehavior`
- Example: `ExecuteAsync_ValidRename_ReturnsSuccess`

## CI/CD Integration

Tests run automatically on every push via GitHub Actions. See `.github/workflows/test-and-log.yml`

## Adding New Tests

1. Follow existing naming patterns
2. Use `[Fact]` for simple tests, `[Theory]` for parameterized tests
3. Include both happy path and error scenarios
4. Add integration tests for new workflows
