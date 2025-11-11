# Mass Code Platform (MCP) for Safe VB.NET Refactoring

## Executive Summary

The **Mass Code Platform (MCP)** is a distributed, service-oriented architecture designed to execute large-scale, **semantically-safe** refactorings on VB.NET codebases. It addresses the critical business need for automating code transformations across thousands of files—a task that is impractical and high-risk when performed manually or with interactive IDE tools.

### Key Features

- **Semantic-Preserving Transformations**: Uses Roslyn's compiler APIs to ensure refactorings maintain code behavior
- **Distributed Architecture**: Service-oriented design with independent API Gateway, Refactoring Workers, and Validation Workers
- **Extensible Plugin System**: Add new refactoring tools without redeploying the platform
  - **2 Production Plugins**: RenameSymbol (using Roslyn Renamer API), ExtractMethod (custom code generation)
  - **Comprehensive Plugin Guide**: Full documentation for creating custom plugins
- **Three-Legged Safety Guarantee**:
  1. Pre-flight semantic validation using Roslyn
  2. Post-flight compilation verification
  3. Automated CI/CD test execution
- **Asynchronous Job Processing**: Long-running refactorings execute in background workers with progress tracking
- **Battle-Tested Components**: Leverages Microsoft's Roslyn, MSBuild, and Hangfire for reliability
- **Production-Ready Testing**: 153 comprehensive tests including real VB.NET refactoring scenarios (~75% coverage)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                                 │
│  (IDE Plugins, CLI Tools, Web Dashboard, CI/CD Pipelines)           │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ REST API (HTTPS/JSON)
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         API GATEWAY                                  │
│  • REST endpoints (POST /jobs, GET /jobs/{id})                      │
│  • Request validation                                                │
│  • Job submission to Hangfire queue                                 │
│  • Lightweight, I/O-bound service                                   │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ Hangfire Job Queue
                                 │ (SQL Server persistent storage)
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    REFACTORING WORKER(S)                             │
│  • Loads VB.NET solutions with MSBuildWorkspace                     │
│  • Dynamically loads refactoring plugins (AssemblyLoadContext)      │
│  • Executes Roslyn transformations                                  │
│  • Writes modified files to disk                                    │
│  • CPU-bound, horizontally scalable                                 │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ Job completion triggers validation
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    VALIDATION WORKER(S)                              │
│  • Git operations (branch, commit, push)                            │
│  • Programmatic compilation (MSBuild)                               │
│  • CI/CD integration (Azure DevOps, Jenkins)                        │
│  • Final verdict: Success or Failure                                │
└─────────────────────────────────────────────────────────────────────┘
```

### Design Rationale

This **Service-Oriented Architecture (SOA)** provides:

- **No Bottlenecks**: Each service handles specific workload types (I/O vs CPU)
- **Elastic Scalability**: Scale workers independently based on queue depth
- **Fault Tolerance**: Single worker failure doesn't crash the entire system
- **Maintainability**: Update individual services without full system redeployment

---

## Project Structure

```
vbdotnet-refactor-mcp/
├── src/
│   ├── MCP.Contracts/                  # Plugin interface definitions
│   │   └── IRefactoringProvider.cs
│   ├── MCP.Core/                        # Shared models and services
│   │   ├── Models/
│   │   │   ├── RefactoringJobRequest.cs
│   │   │   └── RefactoringJobStatus.cs
│   │   └── Services/
│   │       ├── GitService.cs
│   │       ├── CompilationService.cs
│   │       └── CiCdService.cs
│   ├── MCP.ApiGateway/                  # REST API service
│   │   ├── Controllers/RefactoringJobsController.cs
│   │   └── Program.cs
│   ├── MCP.RefactoringWorker/           # Roslyn transformation worker
│   │   ├── Services/RefactoringService.cs
│   │   ├── PluginLoader.cs
│   │   ├── PluginLoadContext.cs
│   │   └── Program.cs
│   ├── MCP.ValidationWorker/            # Git + Compilation + CI/CD worker
│   ├── MCP.Plugins.RenameSymbol/        # Rename refactoring plugin (Roslyn Renamer API)
│   │   └── RenameSymbolProvider.cs
│   └── MCP.Plugins.ExtractMethod/       # Extract method refactoring plugin
│       └── ExtractMethodProvider.cs
├── tests/
│   ├── MCP.Tests/                       # Comprehensive test suite (153 tests)
│   │   ├── Integration/
│   │   │   ├── EndToEndWorkflowTests.cs          # 10 workflow tests
│   │   │   ├── RealVBNetRefactoringTests.cs      # 10 VB.NET fixture tests
│   │   │   └── RealRefactoringExecutionTests.cs  # 7 E2E refactoring tests
│   │   ├── CompilationServiceTests.cs
│   │   ├── CiCdServiceTests.cs
│   │   ├── GitServiceTests.cs
│   │   ├── RefactoringServiceTests.cs
│   │   ├── PluginLoaderTests.cs
│   │   ├── RenameSymbolProviderTests.cs
│   │   └── ExtractMethodProviderTests.cs
│   └── fixtures/                        # Real VB.NET code for testing
│       ├── SampleVBProject.sln
│       └── SampleVBProject/
│           ├── Customer.vb              # Sample business class
│           ├── OrderProcessor.vb        # Complex business logic
│           └── StringHelpers.vb         # VB.NET module
├── docs/
│   ├── ARCHITECTURE.md                  # Full architectural specification
│   ├── PLUGIN-DEVELOPMENT-GUIDE.md      # Plugin creation guide
│   ├── GITHUB-ACTIONS-GUIDE.md          # CI/CD workflow documentation
│   └── TEST-COVERAGE-ANALYSIS.md        # Test coverage report
├── docker-compose.yml
└── MCP.sln                              # 8 projects
```

---

## Getting Started

### Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server** (for Hangfire persistent queue)
- **Git** (for validation workflow)
- **VB.NET projects** to refactor

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-org/vbdotnet-refactor-mcp.git
   cd vbdotnet-refactor-mcp
   ```

2. **Build the solution**:
   ```bash
   dotnet build MCP.sln
   ```

3. **Set up the database**:
   ```bash
   # Create the Hangfire database
   # Update connection strings in appsettings.json for each service
   ```

4. **Deploy plugins**:
   ```bash
   # Copy plugin DLLs to the RefactoringWorker plugins directory
   mkdir -p src/MCP.RefactoringWorker/plugins
   cp src/MCP.Plugins.RenameSymbol/bin/Debug/net8.0/*.dll \
      src/MCP.RefactoringWorker/plugins/
   ```

### Running with Docker Compose

```bash
docker-compose up -d
```

This will start:
- API Gateway (port 5000)
- RefactoringWorker (background service)
- SQL Server (port 1433)
- Hangfire Dashboard (http://localhost:5000/hangfire)

---

## Usage

### Submitting a Refactoring Job

**POST** `http://localhost:5000/api/v1/refactoringjobs`

```json
{
  "solutionPath": "/path/to/MyLegacyApp.sln",
  "refactoringToolName": "RenameSymbol",
  "parameters": {
    "targetFile": "MyProject/MyClass.vb",
    "textSpanStart": 150,
    "textSpanLength": 12,
    "newName": "MyRenamedMethod",
    "includeCommentsAndStrings": true
  },
  "validationPolicy": {
    "onSuccess": "CreatePullRequest",
    "onFailure": "DeleteBranch",
    "steps": ["Compile", "Test"]
  },
  "ciPipelineTrigger": {
    "type": "AzureDevOps",
    "pipelineId": "123",
    "apiEndpoint": "https://dev.azure.com/myorg/myproject",
    "authToken": "your-pat-token"
  }
}
```

**Response**: `202 Accepted`
```json
{
  "jobId": "abc-123-def",
  "status": "Accepted",
  "message": "Job has been accepted and queued for processing",
  "statusUrl": "http://localhost:5000/api/v1/refactoringjobs/abc-123-def"
}
```

### Polling for Job Status

**GET** `http://localhost:5000/api/v1/refactoringjobs/{jobId}`

```json
{
  "jobId": "abc-123-def",
  "status": "Succeeded",
  "message": "Refactoring completed successfully. Modified 15 file(s).",
  "resultUrl": "https://github.com/myorg/myrepo/pull/456",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-15T10:35:00Z",
  "executionLog": [
    "[10:30:05] Job started",
    "[10:30:10] Solution loaded. Projects: 12",
    "[10:32:00] Refactoring transformation completed",
    "[10:33:15] Build succeeded",
    "[10:35:00] All tests passed"
  ]
}
```

---

## The "Three-Legged Stool" Safety Guarantee

Every refactoring job undergoes a three-stage validation pipeline to ensure **semantic preservation**:

### Stage 1: Pre-Flight Semantic Validation

**Technology**: Roslyn's `Renamer` API with conflict detection and `SpeculativeSemanticModel`

**Purpose**: Detect naming conflicts and semantic changes *before* modifying files

**Example**: If renaming `DoWork` would collide with an existing `DoWork` overload, the job fails immediately

### Stage 2: Post-Flight Compilation

**Technology**: MSBuild `BuildManager` API

**Purpose**: Verify the transformed code compiles without errors

**Outcome**: If compilation fails, the refactoring introduced a syntax or type error. Job fails, branch deleted.

### Stage 3: Post-Flight Test Execution

**Technology**: CI/CD integration (Azure DevOps, Jenkins)

**Purpose**: Execute the project's existing test suite to verify behavioral correctness

**Outcome**: If tests fail, the refactoring changed program behavior. Job fails, branch deleted.

---

## Creating Custom Refactoring Plugins

MCP provides a powerful plugin architecture for creating custom refactoring tools. The platform includes **two production plugins** demonstrating different approaches:

1. **RenameSymbol** - Uses Roslyn's built-in Renamer API for semantic-preserving symbol renaming
2. **ExtractMethod** - Custom code generation with data flow analysis

### Quick Start

```bash
# Create a new plugin project
dotnet new classlib -n MCP.Plugins.YourPluginName
cd MCP.Plugins.YourPluginName

# Add required dependencies
dotnet add reference ../MCP.Contracts/MCP.Contracts.csproj
dotnet add package Microsoft.CodeAnalysis.VisualBasic.Workspaces --version 4.8.0

# Implement the IRefactoringProvider interface
# See docs/PLUGIN-DEVELOPMENT-GUIDE.md for detailed instructions
```

### Example Plugin Structure

```csharp
using MCP.Contracts;
using Microsoft.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCP.Plugins.YourPluginName;

public class YourPluginNameProvider : IRefactoringProvider
{
    public string Name => "YourPluginName";

    public string Description => "Brief description of what this plugin does";

    public ValidationResult ValidateParameters(JsonElement parameters)
    {
        // Validate required parameters before execution
        var errors = new List<string>();

        if (!parameters.TryGetProperty("targetFile", out var file) ||
            string.IsNullOrWhiteSpace(file.GetString()))
        {
            errors.Add("'targetFile' is required");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(string.Join("; ", errors));
    }

    public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
    {
        try
        {
            // 1. Extract parameters from context.Parameters
            // 2. Report progress via context.Progress.Report("message")
            // 3. Load document and get semantic model
            // 4. Perform refactoring using Roslyn APIs
            // 5. Return updated solution

            context.Progress.Report("Refactoring completed successfully");
            return RefactoringResult.Success(
                updatedSolution,
                "Success message");
        }
        catch (Exception ex)
        {
            return RefactoringResult.Failure(
                context.Solution,
                $"Refactoring failed: {ex.Message}");
        }
    }
}
```

### Deployment

```bash
# Build your plugin
dotnet build

# Copy to the plugins directory
cp bin/Debug/net8.0/*.dll ../../MCP.RefactoringWorker/plugins/

# Restart the RefactoringWorker to load the new plugin
```

### Complete Documentation

See **[docs/PLUGIN-DEVELOPMENT-GUIDE.md](docs/PLUGIN-DEVELOPMENT-GUIDE.md)** for:
- Detailed interface documentation
- Parameter validation patterns
- Using Roslyn APIs effectively
- Testing strategies
- Real-world examples from RenameSymbol and ExtractMethod plugins
- Best practices and common pitfalls

---

## Configuration

### API Gateway (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Data Source=localhost;Initial Catalog=MCPHangfire;..."
  }
}
```

### Refactoring Worker (`appsettings.json`)

```json
{
  "Hangfire": {
    "WorkerCount": 4
  },
  "PluginDirectory": "plugins",
  "ConnectionStrings": {
    "HangfireConnection": "..."
  }
}
```

---

## Monitoring and Observability

### Hangfire Dashboard

Access the job queue dashboard at: `http://localhost:5000/hangfire`

- View pending, running, and completed jobs
- See job execution history and retry attempts
- Monitor worker health

### Application Logs

All services use structured logging with `Microsoft.Extensions.Logging`. Configure log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MCP": "Debug"
    }
  }
}
```

---

## Performance and Scalability

### Horizontal Scaling

- **API Gateway**: Stateless, can run multiple instances behind a load balancer
- **Refactoring Worker**: Scale based on CPU usage and queue depth
- **Validation Worker**: Scale based on compilation and CI/CD workload

### Recommended Deployment

- **Small team** (< 50 developers): 1 API Gateway, 2 Refactoring Workers
- **Medium team** (50-200 developers): 2 API Gateways, 4-8 Refactoring Workers
- **Large team** (200+ developers): Kubernetes with auto-scaling policies

---

## Security Considerations

### Authentication

- Implement OAuth 2.0 or API keys for the API Gateway
- Use HTTPS for all external communication
- Store CI/CD tokens in Azure Key Vault or similar

### Input Validation

- Solution paths must be validated (no directory traversal)
- Plugin loading uses AssemblyLoadContext isolation to prevent malicious code execution

### Audit Logging

- All job submissions and completions are logged
- Git commits include MCP service account attribution

---

## Troubleshooting

### Common Issues

**Issue**: "Solution loaded but contains no projects"

**Solution**: Ensure `Microsoft.CodeAnalysis.VisualBasic.Workspaces` is present and MSBuild is registered correctly

**Issue**: "Plugin not found"

**Solution**: Verify the plugin DLL is in the `plugins` directory and implements `IRefactoringProvider`

**Issue**: "Compilation failed after refactoring"

**Solution**: This indicates a Roslyn transformation bug. Review the execution log for specific compile errors.

---

## Testing

MCP includes a comprehensive test suite ensuring production readiness and reliability.

### Test Statistics

- **Total Tests**: 153 tests
- **Test Coverage**: ~75% (up from initial 25%)
- **Test Types**: Unit, Integration, End-to-End

### Test Organization

```
tests/MCP.Tests/
├── Integration/
│   ├── EndToEndWorkflowTests.cs           # 10 tests - Job lifecycle workflows
│   ├── RealVBNetRefactoringTests.cs       # 10 tests - VB.NET fixture validation
│   └── RealRefactoringExecutionTests.cs   # 7 tests - Real refactoring scenarios
├── CompilationServiceTests.cs             # 11 tests - MSBuild integration
├── CiCdServiceTests.cs                    # 13 tests - CI/CD integration
├── GitServiceTests.cs                     # 14 tests - Git operations
├── RefactoringServiceTests.cs             # 12 tests - Core refactoring engine
├── RefactoringJobsControllerTests.cs      # 14 tests - REST API endpoints
├── PluginLoaderTests.cs                   # 10 tests - Plugin discovery/loading
├── RenameSymbolProviderTests.cs           # 10 tests - RenameSymbol plugin
├── ExtractMethodProviderTests.cs          # 25 tests - ExtractMethod plugin
└── ... (other test files)
```

### Real VB.NET Test Fixture

The platform includes a complete VB.NET solution for realistic testing:

```
tests/fixtures/SampleVBProject/
├── Customer.vb           # Business class with properties and methods
├── OrderProcessor.vb     # Complex business logic
└── StringHelpers.vb      # VB.NET module with utility functions
```

These fixtures enable:
- Real MSBuildWorkspace loading tests
- Actual Roslyn refactoring execution
- Compilation verification after refactoring
- Symbol resolution and semantic model testing

### Safety Guarantee Test Coverage

All three legs of the safety guarantee are thoroughly tested:

✅ **Leg 1 (Roslyn Pre-flight)**: Tested via RenameSymbolProviderTests, RefactoringServiceTests
✅ **Leg 2 (MSBuild Compilation)**: CompilationServiceTests (11 comprehensive tests)
✅ **Leg 3 (CI/CD Testing)**: CiCdServiceTests (13 tests for Azure DevOps & Jenkins)

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter "Category=Integration"

# Run E2E tests only
dotnet test --filter "Category=E2E"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Test-Driven Development

When creating new plugins:
1. Write validation tests first (parameter checking)
2. Add integration tests with the VB.NET fixture
3. Test error scenarios (conflicts, compilation failures)
4. Verify compilation succeeds after refactoring

See existing plugin tests (RenameSymbolProviderTests, ExtractMethodProviderTests) as examples.

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-new-tool`)
3. Implement your refactoring plugin following the [Plugin Development Guide](docs/PLUGIN-DEVELOPMENT-GUIDE.md)
4. Add comprehensive unit and integration tests (target 75%+ coverage)
5. Ensure all tests pass: `dotnet test`
6. Update documentation if adding new features
7. Submit a pull request

---

## References

### Project Documentation
- [Architecture Documentation](docs/ARCHITECTURE.md) - Full technical specification
- [Plugin Development Guide](docs/PLUGIN-DEVELOPMENT-GUIDE.md) - Complete guide for creating refactoring plugins
- [GitHub Actions Guide](docs/GITHUB-ACTIONS-GUIDE.md) - CI/CD workflow documentation
- [Test Coverage Analysis](docs/TEST-COVERAGE-ANALYSIS.md) - Detailed coverage report

### External Resources
- [Roslyn Documentation](https://github.com/dotnet/roslyn/wiki) - .NET Compiler Platform
- [Hangfire Documentation](https://docs.hangfire.io) - Background job processing
- [Martin Fowler's Refactoring](https://martinfowler.com/books/refactoring.html) - Refactoring principles
- [MSBuild API Documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-api) - Programmatic compilation

---

## License

Copyright © 2025. All rights reserved.

This project implements the architectural design specified in the accompanying blueprint document. It demonstrates a production-ready approach to safe, automated code refactoring at enterprise scale.
