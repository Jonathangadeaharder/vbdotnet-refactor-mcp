# MCP.Core

## Overview

Shared models, services, and utilities used across all MCP components. This package provides the foundational infrastructure for job processing, Git operations, compilation, and CI/CD integration.

## Architecture

```
MCP.Core/
├── Models/          # Data transfer objects and domain models
│   ├── RefactoringJobRequest.cs
│   └── RefactoringJobStatus.cs
└── Services/        # Shared service implementations
    ├── GitService.cs
    ├── CompilationService.cs
    └── CiCdService.cs
```

## Dependencies

- **MCP.Contracts**: For plugin interface definitions
- **External**: Roslyn, MSBuild, Git libraries

## Key Components

### Models

Data structures for job requests, status tracking, and validation policies.

### Services

#### GitService
Handles Git operations: branching, committing, pushing, and pull request creation.

#### CompilationService
Wraps MSBuild for programmatic compilation and validation of transformed code.

#### CiCdService
Integrates with CI/CD systems (Azure DevOps, Jenkins) to trigger test pipelines.

## Usage

All MCP services (ApiGateway, RefactoringWorker, ValidationWorker) depend on this package for shared functionality.

## Testing

See `tests/MCP.Tests/` for comprehensive unit tests of all Core services.
