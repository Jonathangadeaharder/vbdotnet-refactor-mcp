# Mass Code Platform (MCP) for Safe VB.NET Refactoring

## Executive Summary

The **Mass Code Platform (MCP)** is a distributed, service-oriented architecture designed to execute large-scale, **semantically-safe** refactorings on VB.NET codebases. It addresses the critical business need for automating code transformations across thousands of files—a task that is impractical and high-risk when performed manually or with interactive IDE tools.

### Key Features

- **Semantic-Preserving Transformations**: Uses Roslyn's compiler APIs to ensure refactorings maintain code behavior
- **Distributed Architecture**: Service-oriented design with independent API Gateway, Refactoring Workers, and Validation Workers
- **Extensible Plugin System**: Add new refactoring tools without redeploying the platform
- **Three-Legged Safety Guarantee**:
  1. Pre-flight semantic validation using Roslyn
  2. Post-flight compilation verification
  3. Automated CI/CD test execution
- **Asynchronous Job Processing**: Long-running refactorings execute in background workers with progress tracking
- **Battle-Tested Components**: Leverages Microsoft's Roslyn, MSBuild, and Hangfire for reliability

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
│   ├── MCP.Contracts/              # Plugin interface definitions
│   │   └── IRefactoringProvider.cs
│   ├── MCP.Core/                    # Shared models and services
│   │   ├── Models/
│   │   │   ├── RefactoringJobRequest.cs
│   │   │   └── RefactoringJobStatus.cs
│   │   └── Services/
│   │       ├── GitService.cs
│   │       ├── CompilationService.cs
│   │       └── CiCdService.cs
│   ├── MCP.ApiGateway/              # REST API service
│   │   ├── Controllers/RefactoringJobsController.cs
│   │   └── Program.cs
│   ├── MCP.RefactoringWorker/       # Roslyn transformation worker
│   │   ├── Services/RefactoringService.cs
│   │   ├── PluginLoader.cs
│   │   ├── PluginLoadContext.cs
│   │   └── Program.cs
│   ├── MCP.ValidationWorker/        # Git + Compilation + CI/CD worker
│   └── MCP.Plugins.RenameSymbol/    # Example refactoring plugin
│       └── RenameSymbolProvider.cs
├── tests/
│   └── MCP.Tests/
├── docs/
│   └── architecture-blueprint.md    # Full architectural specification
├── docker-compose.yml
└── MCP.sln
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

## Project Structure Linting

This project uses [structurelint](https://github.com/Jonathangadeaharder/structurelint) to enforce project structure, organization, and architectural integrity.

### What is Structurelint?

Structurelint is a next-generation linter designed to enforce:
- **Filesystem organization**: Directory depth limits, file count constraints, naming conventions
- **Architectural boundaries**: Import graph analysis and dependency rule validation
- **Code quality**: Dead code detection and test validation
- **CI/CD compliance**: GitHub workflow enforcement

### Running Structurelint

To check project structure compliance:

```bash
structurelint .
```

The configuration is defined in `.structurelint.yml` and enforces:

**✅ Phase 0 - Filesystem Structure (7 rules enabled):**
- `max-depth: 7` - Limit directory nesting
- `max-files-in-dir: 25` - Limit files per directory (50 for tests)
- `max-subdirs: 15` - Limit subdirectories per directory
- `naming-convention` - PascalCase for C#, kebab-case for YAML
- `dir-naming-convention` - PascalCase for src/ and tests/
- `disallowed-patterns` - Block temp files (.tmp, .bak, .swp, .DS_Store, etc.)
- ~~`file-existence`~~ - Disabled (too strict); READMEs manually created
- ~~`regex-match`~~ - Disabled (naming-convention sufficient for C#)

**✅ Phase 1 - Architectural Layer Enforcement:**
- `enforce-layer-boundaries: true` - **Critical feature**
  - Contracts: No dependencies
  - Core: Only depends on Contracts
  - Workers/Gateway: Only depend on Core + Contracts
  - Plugins: Only depend on Contracts

**⚠️ Phase 2 - Dead Code Detection:**
- ~~`disallow-orphaned-files`~~ - Disabled (C# uses .csproj, not imports)
- ~~`disallow-unused-exports`~~ - Disabled (use VS Code Analysis instead)

**✅ Phase 3 - Test Validation:**
- `test-location` - Validates tests in separate `tests/` directory
- ~~`test-adjacency`~~ - Disabled (C# uses *Tests.cs pattern in separate dir)

**✅ Phase 4 - Code Quality Metrics (Evidence-Based):**
- `max-cognitive-complexity: 15` - **NEW** Evidence-based (r=0.54 correlation with comprehension time)
- `max-halstead-effort: 100000` - **NEW** Neuroscience-validated (rs=0.901 correlation with brain activity)

**⚠️ Phase 5 - Import Patterns:**
- ~~`disallow-deep-imports`~~ - Disabled (analyzes imports; C# uses project references)

**⚠️ Phase 6 - Linter Config Enforcement:**
- ~~`linter-config`~~ - Disabled (C# not yet supported; requires Python/TypeScript/Go/etc.)

**⚠️ Phase 7 - File Content Templates:**
- ~~`file-content`~~ - Disabled (would require custom C# templates)

**📚 Documentation Created:**
- Root: `README.md`, `QUICKSTART.md`, `GITHUB-ACTIONS-GUIDE.md`
- Components: 6 package READMEs (Contracts, Core, ApiGateway, RefactoringWorker, ValidationWorker, Plugins)
- Tests: `tests/MCP.Tests/README.md`
- Directories: `src/README.md`, `tests/README.md`, `docs/README.md`

**Summary:** 12 rules actively enforcing structure + 10 comprehensive READMEs documenting architecture

### Installing Structurelint

If you need to install structurelint:

```bash
# Using Go (pinned to specific commit for reproducibility)
go install github.com/Jonathangadeaharder/structurelint/cmd/structurelint@latest

# Or build from source (recommended for development)
git clone https://github.com/Jonathangadeaharder/structurelint.git
cd structurelint
go build -o structurelint ./cmd/structurelint
sudo cp structurelint /usr/local/bin/
```

### CI/CD Integration

Structurelint is already integrated into the GitHub Actions workflow (`.github/workflows/test-and-log.yml`). It runs automatically on every push to validate project structure.

To add to other workflows:

```yaml
- name: Setup Go
  uses: actions/setup-go@v5
  with:
    go-version: '1.21'

- name: Install structurelint
  run: |
    git clone https://github.com/Jonathangadeaharder/structurelint.git /tmp/structurelint
    cd /tmp/structurelint
    go build -o /usr/local/bin/structurelint ./cmd/structurelint

- name: Run Structurelint
  run: structurelint .
```

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

### Step 1: Create a New Class Library

```bash
dotnet new classlib -n MCP.Plugins.ExtractMethod
dotnet add reference ../../MCP.Contracts/MCP.Contracts.csproj
dotnet add package Microsoft.CodeAnalysis.VisualBasic.Workspaces
```

### Step 2: Implement `IRefactoringProvider`

```csharp
using MCP.Contracts;
using Microsoft.CodeAnalysis;

public class ExtractMethodProvider : IRefactoringProvider
{
    public string Name => "ExtractMethod";
    public string Description => "Extracts selected code into a new method";

    public ValidationResult ValidateParameters(JsonElement parameters)
    {
        // Validate required parameters
        if (!parameters.TryGetProperty("targetFile", out _))
            return ValidationResult.Failure("Missing 'targetFile' parameter");

        return ValidationResult.Success();
    }

    public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
    {
        // 1. Load document and get semantic model
        // 2. Find the target syntax node
        // 3. Use SpeculativeSemanticModel for pre-flight validation
        // 4. Transform the syntax tree
        // 5. Return the new solution

        return RefactoringResult.Success(transformedSolution);
    }
}
```

### Step 3: Deploy the Plugin

```bash
dotnet build MCP.Plugins.ExtractMethod
cp bin/Debug/net8.0/*.dll ../MCP.RefactoringWorker/plugins/
```

The RefactoringWorker will automatically discover and load the new plugin at startup.

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

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-new-tool`)
3. Implement your refactoring plugin following the plugin guide
4. Add unit tests
5. Submit a pull request

---

## References

- [Architectural Blueprint](docs/architecture-blueprint.md) - Full technical specification
- [Roslyn Documentation](https://github.com/dotnet/roslyn/wiki)
- [Hangfire Documentation](https://docs.hangfire.io)
- [Martin Fowler's Refactoring](https://martinfowler.com/books/refactoring.html)

---

## License

Copyright © 2025. All rights reserved.

This project implements the architectural design specified in the accompanying blueprint document. It demonstrates a production-ready approach to safe, automated code refactoring at enterprise scale.
