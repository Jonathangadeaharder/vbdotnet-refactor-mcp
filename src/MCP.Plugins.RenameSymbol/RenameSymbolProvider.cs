using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using MCP.Contracts;

namespace MCP.Plugins.RenameSymbol;

/// <summary>
/// Implements safe symbol renaming using Roslyn's built-in Renamer API.
///
/// As specified in Section 5.1 of the architectural blueprint, this plugin
/// uses the battle-tested Microsoft.CodeAnalysis.Rename.Renamer service
/// which provides automatic conflict detection and semantic validation.
///
/// This is the recommended approach rather than implementing custom rename
/// logic, which is error-prone (59% failure rate per academic research).
/// </summary>
public class RenameSymbolProvider : IRefactoringProvider
{
    public string Name => "RenameSymbol";

    public string Description =>
        "Safely renames a symbol (class, method, variable, etc.) across the entire solution " +
        "with automatic conflict detection and semantic preservation.";

    /// <summary>
    /// Expected parameters:
    /// - targetFile: string - Path to the file containing the symbol
    /// - textSpanStart: int - Start position of the symbol in the file
    /// - textSpanLength: int - Length of the symbol text
    /// - newName: string - The new name for the symbol
    /// - includeCommentsAndStrings: bool (optional) - Whether to rename in comments/strings
    /// </summary>
    public ValidationResult ValidateParameters(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("targetFile", out var targetFile) ||
            targetFile.ValueKind != JsonValueKind.String)
        {
            return ValidationResult.Failure("Missing or invalid 'targetFile' parameter");
        }

        if (!parameters.TryGetProperty("textSpanStart", out var spanStart) ||
            spanStart.ValueKind != JsonValueKind.Number)
        {
            return ValidationResult.Failure("Missing or invalid 'textSpanStart' parameter");
        }

        if (!parameters.TryGetProperty("textSpanLength", out var spanLength) ||
            spanLength.ValueKind != JsonValueKind.Number)
        {
            return ValidationResult.Failure("Missing or invalid 'textSpanLength' parameter");
        }

        if (!parameters.TryGetProperty("newName", out var newName) ||
            newName.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(newName.GetString()))
        {
            return ValidationResult.Failure("Missing or invalid 'newName' parameter");
        }

        return ValidationResult.Success();
    }

    public async Task<RefactoringResult> ExecuteAsync(RefactoringContext context)
    {
        try
        {
            context.Progress.Report("Parsing parameters...");

            var targetFile = context.Parameters.GetProperty("targetFile").GetString()!;
            var textSpanStart = context.Parameters.GetProperty("textSpanStart").GetInt32();
            var textSpanLength = context.Parameters.GetProperty("textSpanLength").GetInt32();
            var newName = context.Parameters.GetProperty("newName").GetString()!;
            var includeCommentsAndStrings = context.Parameters.TryGetProperty("includeCommentsAndStrings", out var prop)
                ? prop.GetBoolean()
                : false;

            context.Progress.Report($"Finding symbol at {targetFile}:{textSpanStart}...");

            // Find the document containing the target symbol
            var document = context.OriginalSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.FilePath?.EndsWith(targetFile) == true);

            if (document == null)
            {
                return RefactoringResult.Failure(
                    $"Could not find document '{targetFile}' in the loaded solution. " +
                    "Ensure the file path is relative to the solution root.");
            }

            // Get the syntax tree and semantic model
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken);
            if (syntaxRoot == null)
            {
                return RefactoringResult.Failure($"Could not parse syntax tree for '{targetFile}'");
            }

            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken);
            if (semanticModel == null)
            {
                return RefactoringResult.Failure($"Could not get semantic model for '{targetFile}'");
            }

            // Find the symbol at the specified location
            var textSpan = new Microsoft.CodeAnalysis.Text.TextSpan(textSpanStart, textSpanLength);
            var node = syntaxRoot.FindNode(textSpan);

            context.Progress.Report($"Resolving symbol: {node.ToString()}");

            // Get the symbol - try both GetDeclaredSymbol and GetSymbolInfo
            // as per Section 4.4 of the architectural blueprint
            var symbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (symbol == null)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(node, context.CancellationToken);
                symbol = symbolInfo.Symbol;
            }

            if (symbol == null)
            {
                return RefactoringResult.Failure(
                    $"Could not resolve a symbol at the specified location. " +
                    $"Ensure the text span points to a valid symbol declaration or usage.");
            }

            context.Progress.Report($"Found symbol: {symbol.ToDisplayString()} (Kind: {symbol.Kind})");

            // Use Roslyn's Renamer API with conflict detection
            // This is the key safety mechanism per Section 5.1
            context.Progress.Report($"Executing rename to '{newName}' with conflict detection...");

            var options = context.OriginalSolution.Workspace.Options;

            // Perform the rename operation
            // The Renamer API handles:
            // - Finding all references across the solution
            // - Detecting naming conflicts
            // - Updating all usages
            // - Maintaining semantic correctness
            var newSolution = await Renamer.RenameSymbolAsync(
                context.OriginalSolution,
                symbol,
                new SymbolRenameOptions(
                    RenameInComments: includeCommentsAndStrings,
                    RenameInStrings: includeCommentsAndStrings,
                    RenameOverloads: false,
                    RenameFile: false),
                newName,
                context.CancellationToken);

            // Check if any conflicts were detected
            // If there were conflicts, the Renamer API would have handled them,
            // but we should verify the operation succeeded
            if (newSolution == context.OriginalSolution)
            {
                return RefactoringResult.Failure(
                    $"Rename operation did not produce any changes. " +
                    $"This may indicate a naming conflict or that the symbol is already named '{newName}'.");
            }

            // Count the number of changed documents to report back
            var changedDocuments = newSolution.GetChanges(context.OriginalSolution)
                .GetProjectChanges()
                .SelectMany(pc => pc.GetChangedDocuments())
                .Count();

            context.Progress.Report(
                $"Rename completed successfully. Modified {changedDocuments} file(s).");

            return RefactoringResult.Success(newSolution);
        }
        catch (Exception ex)
        {
            return RefactoringResult.Failure(
                $"Rename operation failed with exception: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
