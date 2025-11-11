# Mass Code Platform - Architecture Documentation

## Overview

The Mass Code Platform (MCP) is a distributed system for performing safe, large-scale code refactorings on VB.NET codebases. This document provides a technical deep-dive into the architecture, design decisions, and implementation details.

## Core Architectural Principles

### 1. Semantic Preservation

**Definition**: A refactoring is "semantic-preserving" if it changes the code's structure without altering its observable behavior.

**Implementation**:
- Use Roslyn's semantic model to understand code meaning
- Pre-flight validation using `SpeculativeSemanticModel`
- Battle-tested `Renamer` API for rename operations
- Post-flight compilation verification
- Automated test execution via CI/CD

### 2. Service-Oriented Architecture

**Rationale**: Monolithic refactoring tools fail at scale due to:
- Performance bottlenecks (single process handles all work)
- Single point of failure (crash = lost work)
- Poor scalability (must scale entire monolith)

**Solution**: Decompose into specialized services:

```
┌──────────────┐
│ API Gateway  │  Lightweight, I/O-bound
└──────┬───────┘
       │ Job Queue (Hangfire + SQL)
       ▼
┌──────────────┐
│ Refactoring  │  CPU-bound, horizontally scalable
│   Workers    │  (Roslyn transformations)
└──────┬───────┘
       ▼
┌──────────────┐
│ Validation   │  CPU + I/O bound
│   Workers    │  (Compilation + CI/CD)
└──────────────┘
```

### 3. Plugin Extensibility

**Rationale**: Hardcoded refactorings require redeployment for every new tool.

**Solution**: Runtime plugin loading using `AssemblyLoadContext`:
- Each plugin is isolated from host and other plugins
- Plugins can use different dependency versions (e.g., Roslyn 4.6 vs 4.7)
- Hot-deploy new tools without service restart

## Component Deep-Dive

### API Gateway

**Technology**: ASP.NET Core Web API

**Responsibilities**:
1. Accept job submission requests (POST /api/v1/refactoringjobs)
2. Validate request payload
3. Enqueue job to Hangfire
4. Return 202 Accepted with job ID
5. Provide job status polling (GET /api/v1/refactoringjobs/{id})

**Key Design Decisions**:
- RESTful design for external clients (human-readable, standard)
- Asynchronous job pattern (no long-running HTTP connections)
- Stateless (can run multiple instances behind load balancer)

**Code Location**: `src/MCP.ApiGateway/`

### Refactoring Worker

**Technology**: .NET Worker Service + Hangfire

**Responsibilities**:
1. Register MSBuild instance (via `MSBuild.Locator`)
2. Load plugins from designated directory
3. Pick up jobs from Hangfire queue
4. Load VB.NET solution using `MSBuildWorkspace`
5. Delegate to appropriate `IRefactoringProvider` plugin
6. Write transformed solution to disk

**Key Design Decisions**:
- Uses SDK Docker image (not runtime) because MSBuild is required
- Plugin loading uses `AssemblyDependencyResolver` for correct dependency resolution
- Workspace diagnostics are logged for debugging

**Code Location**: `src/MCP.RefactoringWorker/`

### Plugin System

**Technology**: AssemblyLoadContext + Reflection

**Contract**: `IRefactoringProvider` interface

```csharp
public interface IRefactoringProvider
{
    string Name { get; }
    ValidationResult ValidateParameters(JsonElement parameters);
    Task<RefactoringResult> ExecuteAsync(RefactoringContext context);
}
```

**Plugin Lifecycle**:
1. Worker scans plugins directory for DLLs
2. Creates isolated `PluginLoadContext` for each DLL
3. Loads assembly into isolated context
4. Uses reflection to find types implementing `IRefactoringProvider`
5. Instantiates and registers by `Name` property

**Isolation Benefits**:
- Plugin A using Newtonsoft.Json 12.0 doesn't conflict with Plugin B using Newtonsoft.Json 13.0
- Plugin crash doesn't crash the host
- Plugins can be unloaded (garbage collected) if needed

**Code Location**: `src/MCP.Contracts/`, `src/MCP.RefactoringWorker/PluginLoader.cs`

### Validation Pipeline

**Stages**:

**Stage 0: Pre-Flight Semantic Validation** (in plugin)
- Technology: Roslyn `SpeculativeSemanticModel`
- Purpose: Catch semantic-changing errors before modifying files
- Example: `Renamer` API detects naming conflicts

**Stage 1: Git Operations**
- Technology: LibGit2Sharp
- Actions:
  1. Create temporary branch (e.g., `mcp/job-123`)
  2. Commit transformed files
  3. Push to remote

**Stage 2: Compilation**
- Technology: MSBuild `BuildManager` API
- Action: Programmatically compile the solution
- Success Criteria: `BuildResultCode.Success`
- Failure: Capture compile errors, delete branch, fail job

**Stage 3: CI/CD Test Execution**
- Technology: Azure DevOps REST API / Jenkins API
- Actions:
  1. Trigger pipeline build for the new branch
  2. Poll for build result
  3. Check test results
- Success Criteria: All tests pass
- Failure: Tests failed = behavior changed, fail job

**Code Location**: `src/MCP.Core/Services/`

## Data Flow

### Job Submission Flow

```
Client
  │
  │ POST /api/v1/refactoringjobs
  ▼
API Gateway
  │ Validate payload
  │ Create Hangfire job
  │ Store in SQL Server
  │
  │ Return 202 Accepted
  ▼
Client (receives jobId)
```

### Job Execution Flow

```
Hangfire Queue (SQL Server)
  │
  │ Job picked up by available worker
  ▼
RefactoringWorker
  │ Load plugins
  │ Get provider for refactoringToolName
  │ Validate parameters
  │ Load solution with MSBuildWorkspace
  │ Execute provider.ExecuteAsync()
  │ Write changes to disk
  │
  ▼ Trigger validation
ValidationWorker
  │ Git: Create branch, commit, push
  │ Compilation: MSBuild API
  │ CI/CD: Trigger pipeline, poll for result
  │
  │ Final verdict: Success or Failure
  ▼
Hangfire (update job status)
  │
  │ Client polls GET /api/v1/refactoringjobs/{id}
  ▼
Client (receives result)
```

## Technology Stack

| Layer | Technology | Justification |
|-------|-----------|---------------|
| Language | C# / .NET 8 | Native Roslyn support, strong typing, async/await |
| Code Analysis | Roslyn (Microsoft.CodeAnalysis.*) | Only compiler-as-a-service for VB.NET |
| Build System | MSBuild (Microsoft.Build.*) | Required for loading .vbproj files |
| Job Queue | Hangfire | Persistent, reliable, built-in retry and monitoring |
| Storage | SQL Server | Hangfire requirement, enterprise-ready |
| Git | LibGit2Sharp | Native C# Git library |
| API | ASP.NET Core | Industry standard REST framework |
| Containerization | Docker | Consistent deployment, easy scaling |

## Performance Characteristics

### Bottlenecks

1. **Solution Loading**: MSBuildWorkspace is slow for large solutions (1000+ projects)
   - Mitigation: Cache loaded solutions, reuse workspace across similar jobs

2. **Roslyn Analysis**: Semantic model computation is CPU-intensive
   - Mitigation: Horizontal scaling of RefactoringWorkers

3. **Compilation**: Full solution rebuild takes time
   - Mitigation: Consider incremental compilation for future optimization

### Scalability

**Horizontal Scaling**:
- API Gateway: Stateless, can run N instances behind load balancer
- RefactoringWorker: Add more workers based on queue depth
- ValidationWorker: Scale based on compilation workload

**Vertical Scaling**:
- RefactoringWorker benefits from more CPU cores (parallel compilation)
- SQL Server benefits from more RAM (Hangfire job storage)

**Estimated Capacity** (per RefactoringWorker with 4 cores):
- Small refactorings (rename in 10 files): ~5/minute
- Medium refactorings (rename in 100 files): ~1/minute
- Large refactorings (extract method in 1000 files): ~1/10 minutes

## Security Considerations

### Threat Model

1. **Malicious Solution Files**: User provides .sln that exploits MSBuild
   - Mitigation: Run workers in sandboxed containers, no network access

2. **Malicious Plugins**: Attacker uploads plugin DLL
   - Mitigation: Plugin directory is read-only, plugins are code-reviewed before deployment

3. **CI/CD Token Exposure**: Auth tokens logged or exposed
   - Mitigation: Use secrets management (Azure Key Vault), don't log tokens

### Recommended Security Measures

- Implement OAuth 2.0 for API Gateway
- Use HTTPS for all external communication
- Run workers as low-privilege users
- Enable audit logging for all job submissions
- Regularly update dependencies (Roslyn, Hangfire, etc.)

## Future Enhancements

### Phase 6: Advanced Features

1. **Solution Caching**: Cache loaded solutions to speed up subsequent jobs
2. **Incremental Compilation**: Only recompile changed projects
3. **Parallel Transformations**: Apply independent refactorings in parallel
4. **Real-time Progress**: WebSocket support for live job progress
5. **Conflict Resolution UI**: Interactive tool for resolving detected conflicts
6. **Plugin Marketplace**: Central registry for community-contributed plugins

## References

- [Roslyn Documentation](https://github.com/dotnet/roslyn/wiki)
- [Hangfire Documentation](https://docs.hangfire.io)
- [AssemblyLoadContext Guide](https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- [MSBuild API Reference](https://docs.microsoft.com/en-us/dotnet/api/microsoft.build)

---

For implementation details of specific components, see the inline code documentation and the architectural blueprint in the repository.
