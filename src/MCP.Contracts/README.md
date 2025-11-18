# MCP.Contracts

## Overview

This package defines the core plugin interface for the Mass Code Platform refactoring system. It contains only interface definitions and has **zero dependencies** on other MCP components.

## Purpose

`MCP.Contracts` serves as the contract between the core platform and refactoring plugins. By keeping this package dependency-free, plugins can be developed, tested, and versioned independently from the platform.

## Key Interfaces

### `IRefactoringProvider`

The primary interface that all refactoring plugins must implement:

```csharp
public interface IRefactoringProvider
{
    string Name { get; }
    string Description { get; }
    ValidationResult ValidateParameters(JsonElement parameters);
    Task<RefactoringResult> ExecuteAsync(RefactoringContext context);
}
```

## Architectural Principles

1. **Zero Dependencies**: This package references no other MCP components
2. **Stable Interface**: Breaking changes here affect all plugins
3. **Version Carefully**: Use semantic versioning strictly
4. **Document Thoroughly**: Interface changes require migration guides

## Usage

Plugin developers reference only this package:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\MCP.Contracts\MCP.Contracts.csproj" />
</ItemGroup>
```

## Related Documentation

- See `src/MCP.Plugins.RenameSymbol` for a reference implementation
- See root README.md for the full architectural overview
