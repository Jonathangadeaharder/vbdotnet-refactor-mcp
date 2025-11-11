# Plugin Development Guide

This guide explains how to create custom refactoring plugins for the Mass Code Platform (MCP).

## Table of Contents

- [Overview](#overview)
- [Plugin Architecture](#plugin-architecture)
- [Creating a New Plugin](#creating-a-new-plugin)
- [Plugin Interface](#plugin-interface)
- [Parameter Validation](#parameter-validation)
- [Executing Refactorings](#executing-refactorings)
- [Testing Your Plugin](#testing-your-plugin)
- [Examples](#examples)
- [Best Practices](#best-practices)

## Overview

MCP uses a plugin architecture to support extensible refactoring operations. Each plugin is a self-contained .NET assembly that implements the `IRefactoringProvider` interface.

**Key Benefits:**
- **Isolation** - Plugins run in separate AssemblyLoadContexts to prevent DLL Hell
- **Discoverability** - Plugins are automatically discovered from the plugins directory
- **Type Safety** - Strong contracts via `IRefactoringProvider` interface
- **Testing** - Plugins can be unit tested independently

## Plugin Architecture

```
┌─────────────────────────────────────────┐
│       RefactoringWorker                 │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │      PluginLoader                │  │
│  │  - Discovers plugins             │  │
│  │  - Loads via AssemblyLoadContext │  │
│  │  - Maintains provider registry   │  │
│  └──────────────────────────────────┘  │
│               │                         │
│               ▼                         │
│  ┌──────────────────────────────────┐  │
│  │   IRefactoringProvider           │  │
│  │   - ValidateParameters()         │  │
│  │   - ExecuteAsync()               │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
           │                    │
           ▼                    ▼
┌──────────────────┐  ┌──────────────────┐
│  RenameSymbol    │  │  ExtractMethod   │
│  Plugin          │  │  Plugin          │
└──────────────────┘  └──────────────────┘
```

## Creating a New Plugin

### Step 1: Create a New Project

```bash
cd src
mkdir MCP.Plugins.YourPluginName
cd MCP.Plugins.YourPluginName
```

Create `MCP.Plugins.YourPluginName.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild" Version="4.8.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.VisualBasic.Workspaces" Version="4.8.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MCP.Contracts\MCP.Contracts.csproj" />
  </ItemGroup>
</Project>
```

### Step 2: Add to Solution

Add your plugin to `MCP.sln`:

```bash
dotnet sln add src/MCP.Plugins.YourPluginName/MCP.Plugins.YourPluginName.csproj
```

### Step 3: Implement IRefactoringProvider

Create `YourPluginNameProvider.cs`:

```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MCP.Contracts;

namespace MCP.Plugins.YourPluginName;

public class YourPluginNameProvider : IRefactoringProvider
{
    public string Name => "YourPluginName";

    public string Description => "Brief description of what this plugin does";

    public ValidationResult ValidateParameters(JsonElement parameters)
    {
        // Validate required and optional parameters
        var errors = new List<string>();

        // Example: Check required parameter
        if (!parameters.TryGetProperty("requiredParam", out var param) ||
            param.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(param.GetString()))
        {
            errors.Add("'requiredParam' is required");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(string.Join("; ", errors));
    }

    public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
    {
        try
        {
            // Extract parameters
            var param = context.Parameters.GetProperty("requiredParam").GetString()!;

            // Report progress
            context.Progress.Report("Starting refactoring...");

            // Perform the refactoring using Roslyn APIs
            var updatedSolution = await PerformRefactoringAsync(
                context.Solution,
                param,
                context.Progress,
                context.CancellationToken);

            context.Progress.Report("Refactoring completed successfully");

            return RefactoringResult.Success(
                updatedSolution,
                "Refactoring completed successfully");
        }
        catch (Exception ex)
        {
            return RefactoringResult.Failure(
                context.Solution,
                $"Refactoring failed: {ex.Message}");
        }
    }

    private async Task<Solution> PerformRefactoringAsync(
        Solution solution,
        string param,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        // Your refactoring logic here
        return solution;
    }
}
```

## Plugin Interface

The `IRefactoringProvider` interface defines three members:

### Name Property

```csharp
public string Name { get; }
```

**Purpose:** Unique identifier for your plugin. Used by clients to request specific refactorings.

**Guidelines:**
- Use PascalCase (e.g., "RenameSymbol", "ExtractMethod")
- Keep it concise and descriptive
- Avoid special characters or spaces

### Description Property

```csharp
public string Description { get; }
```

**Purpose:** Human-readable description of the plugin's functionality.

**Guidelines:**
- 1-2 sentences maximum
- Describe WHAT it does, not HOW
- Mention supported languages if relevant

**Example:**
```csharp
public string Description =>
    "Extracts a selection of code into a new method. " +
    "Analyzes data flow to determine parameters and return values. " +
    "Supports both VB.NET and C# code.";
```

### ValidateParameters Method

```csharp
public ValidationResult ValidateParameters(JsonElement parameters);
```

**Purpose:** Validates the request parameters before execution.

**Guidelines:**
- Check all required parameters exist and have correct types
- Validate parameter values (ranges, formats, etc.)
- Return detailed error messages for invalid input
- Don't perform expensive operations (no I/O, no compilation)

**Example:**
```csharp
public ValidationResult ValidateParameters(JsonElement parameters)
{
    var errors = new List<string>();

    // Required: targetFile
    if (!parameters.TryGetProperty("targetFile", out var fileElement) ||
        fileElement.ValueKind != JsonValueKind.String ||
        string.IsNullOrWhiteSpace(fileElement.GetString()))
    {
        errors.Add("'targetFile' is required and must be a non-empty string");
    }

    // Required: textSpanStart (must be non-negative number)
    if (!parameters.TryGetProperty("textSpanStart", out var startElement) ||
        startElement.ValueKind != JsonValueKind.Number)
    {
        errors.Add("'textSpanStart' is required and must be a number");
    }
    else if (startElement.GetInt32() < 0)
    {
        errors.Add("'textSpanStart' must be non-negative");
    }

    // Optional: includeComments (boolean if provided)
    if (parameters.TryGetProperty("includeComments", out var commentsElement) &&
        commentsElement.ValueKind != JsonValueKind.True &&
        commentsElement.ValueKind != JsonValueKind.False)
    {
        errors.Add("'includeComments' must be a boolean if provided");
    }

    return errors.Count == 0
        ? ValidationResult.Success()
        : ValidationResult.Failure(string.Join("; ", errors));
}
```

### ExecuteAsync Method

```csharp
public Task<RefactoringResult> ExecuteAsync(RefactoringContext context);
```

**Purpose:** Performs the actual refactoring operation.

**Context Object:**
```csharp
public class RefactoringContext
{
    public Solution Solution { get; }              // Current Roslyn solution
    public JsonElement Parameters { get; }          // Request parameters
    public IProgress<string> Progress { get; }      // Progress reporting
    public CancellationToken CancellationToken { get; }
}
```

**Guidelines:**
- Always handle exceptions and return `RefactoringResult.Failure()` on error
- Report progress for long-running operations
- Respect the `CancellationToken`
- Return the updated `Solution` on success
- Never modify the input `Solution` directly (Roslyn solutions are immutable)

## Parameter Validation

### Common Parameter Types

#### File Paths
```csharp
if (!parameters.TryGetProperty("targetFile", out var fileElement) ||
    fileElement.ValueKind != JsonValueKind.String ||
    string.IsNullOrWhiteSpace(fileElement.GetString()))
{
    errors.Add("'targetFile' is required");
}
```

#### Text Spans
```csharp
if (!parameters.TryGetProperty("textSpanStart", out var startElement) ||
    startElement.ValueKind != JsonValueKind.Number)
{
    errors.Add("'textSpanStart' is required and must be a number");
}

if (!parameters.TryGetProperty("textSpanLength", out var lengthElement) ||
    lengthElement.ValueKind != JsonValueKind.Number)
{
    errors.Add("'textSpanLength' is required and must be a number");
}
```

#### Identifiers
```csharp
if (!parameters.TryGetProperty("newName", out var nameElement) ||
    nameElement.ValueKind != JsonValueKind.String ||
    string.IsNullOrWhiteSpace(nameElement.GetString()))
{
    errors.Add("'newName' is required");
}
else
{
    var name = nameElement.GetString()!;
    if (!IsValidIdentifier(name))
    {
        errors.Add("'newName' must be a valid identifier");
    }
}

private bool IsValidIdentifier(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        return false;

    if (!char.IsLetter(name[0]) && name[0] != '_')
        return false;

    return name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
```

#### Booleans
```csharp
if (parameters.TryGetProperty("makeStatic", out var staticElement) &&
    staticElement.ValueKind != JsonValueKind.True &&
    staticElement.ValueKind != JsonValueKind.False)
{
    errors.Add("'makeStatic' must be a boolean if provided");
}
```

#### Enums/Restricted Values
```csharp
if (parameters.TryGetProperty("accessModifier", out var accessElement) &&
    accessElement.ValueKind == JsonValueKind.String)
{
    var modifier = accessElement.GetString()!.ToLower();
    var validModifiers = new[] { "public", "private", "protected", "internal" };
    if (!validModifiers.Contains(modifier))
    {
        errors.Add($"'accessModifier' must be one of: {string.Join(", ", validModifiers)}");
    }
}
```

## Executing Refactorings

### Finding Documents

```csharp
var targetFile = parameters.GetProperty("targetFile").GetString()!;

var document = context.Solution.Projects
    .SelectMany(p => p.Documents)
    .FirstOrDefault(d => d.FilePath?.EndsWith(targetFile, StringComparison.OrdinalIgnoreCase) == true);

if (document == null)
{
    return RefactoringResult.Failure(
        context.Solution,
        $"Could not find document: {targetFile}");
}
```

### Getting Syntax Trees and Semantic Models

```csharp
var syntaxTree = await document.GetSyntaxTreeAsync(context.CancellationToken);
var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken);

if (syntaxTree == null || semanticModel == null)
{
    return RefactoringResult.Failure(
        context.Solution,
        "Could not get syntax tree or semantic model");
}
```

### Finding Symbols

```csharp
var textSpanStart = parameters.GetProperty("textSpanStart").GetInt32();
var textSpanLength = parameters.GetProperty("textSpanLength").GetInt32();
var selection = new TextSpan(textSpanStart, textSpanLength);

var root = await syntaxTree.GetRootAsync(context.CancellationToken);
var node = root.FindNode(selection);

var symbol = semanticModel.GetDeclaredSymbol(node) ??
             semanticModel.GetSymbolInfo(node).Symbol;

if (symbol == null)
{
    return RefactoringResult.Failure(
        context.Solution,
        "Could not find symbol at the specified location");
}
```

### Using Roslyn Rename API

```csharp
var newName = parameters.GetProperty("newName").GetString()!;

// Use Roslyn's Renamer API for semantic-preserving renames
var updatedSolution = await Microsoft.CodeAnalysis.Rename.Renamer.RenameSymbolAsync(
    context.Solution,
    symbol,
    new Microsoft.CodeAnalysis.Rename.SymbolRenameOptions(),
    newName,
    context.CancellationToken);

return RefactoringResult.Success(
    updatedSolution,
    $"Successfully renamed '{symbol.Name}' to '{newName}'");
```

### Modifying Documents

```csharp
// Get the current text
var text = await document.GetTextAsync(context.CancellationToken);

// Make changes
var newText = text.Replace(textSpan, replacementText);

// Create updated document
var updatedDocument = document.WithText(newText);

// Get updated solution
var updatedSolution = updatedDocument.Project.Solution;
```

### Progress Reporting

```csharp
context.Progress.Report("Loading solution...");
// ... perform work ...

context.Progress.Report("Analyzing symbol references...");
// ... perform work ...

context.Progress.Report($"Found {referenceCount} references");
// ... perform work ...

context.Progress.Report("Applying refactoring...");
// ... perform work ...

context.Progress.Report("Refactoring completed successfully");
```

### Cancellation Support

```csharp
public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
{
    // Check for cancellation periodically
    context.CancellationToken.ThrowIfCancellationRequested();

    var documents = context.Solution.Projects.SelectMany(p => p.Documents);

    foreach (var document in documents)
    {
        // Check before each expensive operation
        context.CancellationToken.ThrowIfCancellationRequested();

        await ProcessDocumentAsync(document, context.CancellationToken);
    }

    return RefactoringResult.Success(/* ... */);
}
```

## Testing Your Plugin

### Unit Tests for Parameter Validation

```csharp
using System.Text.Json;
using Xunit;

public class YourPluginNameProviderTests
{
    private readonly YourPluginNameProvider _provider;

    public YourPluginNameProviderTests()
    {
        _provider = new YourPluginNameProvider();
    }

    [Fact]
    public void Provider_ShouldHaveCorrectName()
    {
        Assert.Equal("YourPluginName", _provider.Name);
    }

    [Fact]
    public void ValidateParameters_WithMissingRequiredParam_ShouldFail()
    {
        var json = JsonDocument.Parse(@"{
            ""someOtherParam"": ""value""
        }").RootElement;

        var result = _provider.ValidateParameters(json);

        Assert.False(result.IsValid);
        Assert.Contains("requiredParam", result.ErrorMessage);
    }

    [Fact]
    public void ValidateParameters_WithAllRequiredFields_ShouldSucceed()
    {
        var json = JsonDocument.Parse(@"{
            ""requiredParam"": ""value"",
            ""targetFile"": ""Customer.vb"",
            ""textSpanStart"": 100,
            ""textSpanLength"": 50
        }").RootElement;

        var result = _provider.ValidateParameters(json);

        Assert.True(result.IsValid);
    }
}
```

### Integration Tests

```csharp
[Fact]
public async Task E2E_YourPluginName_ShouldRefactorCorrectly()
{
    // Load the test fixture solution
    using var workspace = MSBuildWorkspace.Create();
    var solution = await workspace.OpenSolutionAsync("path/to/test/solution.sln");

    // Create parameters
    var parameters = JsonDocument.Parse(@"{
        ""targetFile"": ""TestFile.vb"",
        ""requiredParam"": ""value""
    }").RootElement;

    // Execute refactoring
    var provider = new YourPluginNameProvider();
    var context = new RefactoringContext(
        solution,
        parameters,
        new Progress<string>(),
        CancellationToken.None);

    var result = await provider.ExecuteAsync(context);

    // Assert
    Assert.True(result.Success);

    // Verify the changes
    var updatedDoc = result.UpdatedSolution.Projects
        .SelectMany(p => p.Documents)
        .First(d => d.Name == "TestFile.vb");

    var updatedText = (await updatedDoc.GetTextAsync()).ToString();
    Assert.Contains("expected content", updatedText);
}
```

## Examples

### Example 1: Simple Find and Replace

```csharp
public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
{
    try
    {
        var targetFile = context.Parameters.GetProperty("targetFile").GetString()!;
        var findText = context.Parameters.GetProperty("find").GetString()!;
        var replaceText = context.Parameters.GetProperty("replace").GetString()!;

        context.Progress.Report($"Finding '{findText}' in {targetFile}...");

        var document = context.Solution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => d.FilePath?.EndsWith(targetFile) == true);

        if (document == null)
        {
            return RefactoringResult.Failure(
                context.Solution,
                $"Could not find: {targetFile}");
        }

        var text = await document.GetTextAsync(context.CancellationToken);
        var newText = text.ToString().Replace(findText, replaceText);

        var updatedDocument = document.WithText(SourceText.From(newText));
        var updatedSolution = updatedDocument.Project.Solution;

        context.Progress.Report("Replacement completed");

        return RefactoringResult.Success(
            updatedSolution,
            $"Replaced all occurrences of '{findText}' with '{replaceText}'");
    }
    catch (Exception ex)
    {
        return RefactoringResult.Failure(
            context.Solution,
            $"Refactoring failed: {ex.Message}");
    }
}
```

### Example 2: Using Roslyn Code Fixes

```csharp
public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
{
    var document = GetTargetDocument(context);
    var syntaxTree = await document.GetSyntaxTreeAsync();
    var root = await syntaxTree.GetRootAsync();

    // Find nodes matching your criteria
    var nodesToFix = root.DescendantNodes()
        .OfType<YourSyntaxNodeType>()
        .Where(n => ShouldFix(n));

    var editor = await DocumentEditor.CreateAsync(document);

    foreach (var node in nodesToFix)
    {
        var replacement = GenerateReplacement(node);
        editor.ReplaceNode(node, replacement);
    }

    var updatedDocument = editor.GetChangedDocument();
    return RefactoringResult.Success(
        updatedDocument.Project.Solution,
        "Applied code fixes");
}
```

## Best Practices

### 1. Prefer Roslyn APIs Over String Manipulation

❌ **Bad:**
```csharp
var source = text.ToString().Replace("oldName", "newName");
```

✅ **Good:**
```csharp
var updatedSolution = await Renamer.RenameSymbolAsync(
    solution, symbol, options, newName, cancellationToken);
```

### 2. Validate Thoroughly

❌ **Bad:**
```csharp
var file = parameters.GetProperty("file").GetString();
// Assumes property exists and is non-null
```

✅ **Good:**
```csharp
if (!parameters.TryGetProperty("file", out var fileElement) ||
    fileElement.ValueKind != JsonValueKind.String ||
    string.IsNullOrWhiteSpace(fileElement.GetString()))
{
    return ValidationResult.Failure("'file' is required");
}
```

### 3. Report Progress

❌ **Bad:**
```csharp
// Long-running operation with no feedback
await ProcessAllDocumentsAsync(documents);
```

✅ **Good:**
```csharp
context.Progress.Report($"Processing {documents.Count} documents...");
for (int i = 0; i < documents.Count; i++)
{
    context.Progress.Report($"Processing document {i + 1}/{documents.Count}");
    await ProcessDocumentAsync(documents[i]);
}
```

### 4. Handle Errors Gracefully

❌ **Bad:**
```csharp
// Let exceptions propagate
var symbol = semanticModel.GetSymbol(node);
await Renamer.RenameSymbolAsync(...);
```

✅ **Good:**
```csharp
try
{
    var symbol = semanticModel.GetSymbol(node);
    if (symbol == null)
    {
        return RefactoringResult.Failure(
            solution,
            "Could not find symbol");
    }

    return await SafeRenameAsync(solution, symbol, newName);
}
catch (Exception ex)
{
    return RefactoringResult.Failure(
        solution,
        $"Rename failed: {ex.Message}");
}
```

### 5. Respect Cancellation Tokens

✅ **Good:**
```csharp
foreach (var document in documents)
{
    context.CancellationToken.ThrowIfCancellationRequested();
    await ProcessAsync(document, context.CancellationToken);
}
```

### 6. Test Edge Cases

- Empty/null parameters
- Files that don't exist
- Invalid text spans (out of bounds)
- Conflicting renames
- Compilation errors after refactoring

### 7. Support Both VB.NET and C#

```csharp
private SyntaxNode GenerateNode(string language)
{
    if (language == LanguageNames.VisualBasic)
    {
        return SyntaxFactory./* VB.NET syntax */;
    }
    else
    {
        return CSharp.SyntaxFactory./* C# syntax */;
    }
}
```

## Deployment

### Plugin Discovery

Plugins are automatically discovered from the configured plugins directory. The default location is:

```
/app/plugins/
```

### Plugin Loading

1. PluginLoader scans the directory for `.dll` files
2. Each assembly is loaded into a separate AssemblyLoadContext
3. Types implementing `IRefactoringProvider` are registered
4. Plugins are available by their `Name` property

### Hot Reload

Currently, plugins require a worker restart to reload. Future versions may support hot reload.

## Additional Resources

- [Roslyn API Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [MCP Architecture Documentation](./ARCHITECTURE.md)
- [Example Plugins](../src/MCP.Plugins.RenameSymbol/)
- [Contract Definitions](../src/MCP.Contracts/)

## Support

For questions or issues with plugin development:
- Review existing plugins in `src/MCP.Plugins.*`
- Check the test suite in `tests/MCP.Tests/`
- Refer to the main documentation in `docs/`
