# MCP.Plugins.RenameSymbol

## Overview

Reference implementation of a refactoring plugin using Roslyn's `Renamer` API. Demonstrates how to build a plugin that implements `IRefactoringProvider`.

## Functionality

Renames symbols (methods, classes, variables) across an entire VB.NET solution with:
- Conflict detection
- Comment and string literal updates (optional)
- Pre-flight semantic validation
- Proper handling of overloads and inheritance

## Parameters

```json
{
  "targetFile": "MyProject/MyClass.vb",
  "textSpanStart": 150,
  "textSpanLength": 12,
  "newName": "MyRenamedMethod",
  "includeCommentsAndStrings": true
}
```

## Implementation Highlights

### Pre-Flight Validation

```csharp
// Use SpeculativeSemanticModel to detect conflicts before modifying files
var speculativeModel = document.GetSemanticModelAsync().Result;
var conflicts = Renamer.FindConflicts(...);
if (conflicts.Any())
    return RefactoringResult.Failure("Naming conflict detected");
```

### Transformation

```csharp
// Use Renamer API for safe renaming
var solution = await Renamer.RenameSymbolAsync(
    solution,
    symbol,
    newName,
    optionSet
);
```

## Testing

See `tests/MCP.Tests/RenameSymbolProviderTests.cs` for:
- Basic renaming scenarios
- Conflict detection tests
- Integration tests with full solutions

## Creating Your Own Plugin

1. Create new class library project
2. Reference `MCP.Contracts`
3. Implement `IRefactoringProvider`
4. Build and copy DLL to RefactoringWorker's `plugins/` directory

See root README.md for detailed plugin development guide.
