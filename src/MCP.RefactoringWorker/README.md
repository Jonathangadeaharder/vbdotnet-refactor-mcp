# MCP.RefactoringWorker

## Overview

CPU-intensive background worker that executes Roslyn-based code transformations. Loads VB.NET solutions, discovers and loads refactoring plugins, and applies semantic-preserving transformations.

## Responsibilities

1. **Load Solutions**: Use MSBuildWorkspace to load VB.NET solutions and projects
2. **Load Plugins**: Dynamically discover and load `IRefactoringProvider` implementations
3. **Execute Transformations**: Run plugin refactorings using Roslyn APIs
4. **Validate Semantics**: Pre-flight validation with SpeculativeSemanticModel
5. **Write Changes**: Persist transformed syntax trees to disk

## Plugin System

Plugins are loaded via `AssemblyLoadContext` for isolation:

```
MCP.RefactoringWorker/
├── PluginLoader.cs         # Plugin discovery and loading
├── PluginLoadContext.cs    # Assembly isolation
└── plugins/                # Plugin DLL directory
    └── MCP.Plugins.*.dll
```

### Plugin Discovery

1. Scan `plugins/` directory for DLLs
2. Load assemblies in isolated context
3. Reflect for types implementing `IRefactoringProvider`
4. Instantiate and register plugins

## Architecture

```
┌─────────────────────────────────────────┐
│         Hangfire Worker Pool            │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│      RefactoringService                 │
│  • Loads solution via MSBuildWorkspace  │
│  • Resolves plugin by name              │
│  • Executes transformation              │
│  • Validates semantics                  │
└─────────────────────────────────────────┘
```

## Configuration

See `appsettings.json`:
- Hangfire worker count
- Plugin directory path
- Roslyn workspace options

## Performance

CPU-bound workload. Scale horizontally based on queue depth.

**Recommended:** 1 worker per 2-4 CPU cores

## Testing

See `tests/MCP.Tests/RefactoringServiceTests.cs` and `PluginLoaderTests.cs`
