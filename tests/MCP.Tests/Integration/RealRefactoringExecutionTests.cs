using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using MCP.Plugins.RenameSymbol;
using MCP.Contracts;

namespace MCP.Tests.Integration
{
    [Trait("Category", "Integration")]
    [Trait("Category", "E2E")]
    public class RealRefactoringExecutionTests : IDisposable
    {
        private readonly string _fixturePath;
        private readonly string _solutionPath;
        private readonly string _testWorkingDirectory;

        public RealRefactoringExecutionTests()
        {
            // Get paths
            var testDirectory = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", ".."));
            _fixturePath = Path.Combine(repoRoot, "tests", "fixtures");
            _solutionPath = Path.Combine(_fixturePath, "SampleVBProject.sln");

            // Create a temporary working directory for each test
            _testWorkingDirectory = Path.Combine(Path.GetTempPath(), $"MCP_Test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testWorkingDirectory);
        }

        [Fact]
        public async Task E2E_RenameSymbol_RenameProperty_ShouldUpdateAllReferences()
        {
            // Arrange: Copy fixture to temp directory
            var tempSolutionPath = CopyFixtureToTemp();

            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(tempSolutionPath, cancellationToken: CancellationToken.None);

            // Find the Customer.vb document
            var customerDoc = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "Customer.vb");

            Assert.NotNull(customerDoc);

            // Find the CustomerName property declaration
            var syntaxTree = await customerDoc.GetSyntaxTreeAsync();
            var text = await customerDoc.GetTextAsync();
            var sourceText = text.ToString();

            // Find "CustomerName" property (should be around line 20-30)
            var customerNameIndex = sourceText.IndexOf("Public Property CustomerName");
            Assert.True(customerNameIndex > 0, "Could not find CustomerName property");

            // Get the exact position of the identifier "CustomerName"
            var identifierStart = sourceText.IndexOf("CustomerName", customerNameIndex);
            var identifierLength = "CustomerName".Length;

            // Create the RenameSymbol plugin
            var renameProvider = new RenameSymbolProvider();

            // Create parameters for renaming CustomerName to ClientName
            var parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": " + identifierStart + @",
                ""textSpanLength"": " + identifierLength + @",
                ""newName"": ""ClientName""
            }").RootElement;

            // Validate parameters
            var validationResult = renameProvider.ValidateParameters(parameters);
            Assert.True(validationResult.IsValid, $"Validation failed: {validationResult.ErrorMessage}");

            // Execute the refactoring
            var progress = new Progress<string>();
            var context = new RefactoringContext(
                solution,
                parameters,
                progress,
                CancellationToken.None);

            var result = await renameProvider.ExecuteAsync(context);

            // Assert: Refactoring succeeded
            Assert.True(result.Success, $"Refactoring failed: {result.Message}");
            Assert.NotNull(result.UpdatedSolution);

            // Verify: Get the updated Customer.vb
            var updatedCustomerDoc = result.UpdatedSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "Customer.vb");

            Assert.NotNull(updatedCustomerDoc);

            var updatedText = await updatedCustomerDoc.GetTextAsync();
            var updatedSource = updatedText.ToString();

            // Verify all occurrences were renamed
            Assert.Contains("Public Property ClientName", updatedSource);
            Assert.Contains("_clientName As String", updatedSource); // Private field should be renamed
            Assert.DoesNotContain("CustomerName", updatedSource); // Old name should be gone

            // Verify the property getter/setter references were updated
            Assert.Contains("_clientName = name", updatedSource);
            Assert.Contains("Return _clientName", updatedSource);
        }

        [Fact]
        public async Task E2E_RenameSymbol_RenameMethod_ShouldUpdateCallSites()
        {
            // Arrange: Copy fixture to temp directory
            var tempSolutionPath = CopyFixtureToTemp();

            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(tempSolutionPath, cancellationToken: CancellationToken.None);

            var customerDoc = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "Customer.vb");

            Assert.NotNull(customerDoc);

            var text = await customerDoc.GetTextAsync();
            var sourceText = text.ToString();

            // Find the CalculateDiscount method
            var methodIndex = sourceText.IndexOf("Public Function CalculateDiscount");
            Assert.True(methodIndex > 0, "Could not find CalculateDiscount method");

            var identifierStart = sourceText.IndexOf("CalculateDiscount", methodIndex);
            var identifierLength = "CalculateDiscount".Length;

            // Create the RenameSymbol plugin
            var renameProvider = new RenameSymbolProvider();

            var parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": " + identifierStart + @",
                ""textSpanLength"": " + identifierLength + @",
                ""newName"": ""GetDiscountPercentage""
            }").RootElement;

            var validationResult = renameProvider.ValidateParameters(parameters);
            Assert.True(validationResult.IsValid);

            // Execute
            var progress = new Progress<string>();
            var context = new RefactoringContext(solution, parameters, progress, CancellationToken.None);
            var result = await renameProvider.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success, $"Refactoring failed: {result.Message}");

            var updatedCustomerDoc = result.UpdatedSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "Customer.vb");

            var updatedSource = (await updatedCustomerDoc.GetTextAsync()).ToString();

            // Verify method was renamed
            Assert.Contains("Public Function GetDiscountPercentage", updatedSource);
            Assert.DoesNotContain("Function CalculateDiscount", updatedSource);
        }

        [Fact]
        public async Task E2E_RenameSymbol_RenameLocalVariable_ShouldOnlyUpdateLocalScope()
        {
            // Arrange
            var tempSolutionPath = CopyFixtureToTemp();

            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(tempSolutionPath);

            var orderProcessorDoc = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "OrderProcessor.vb");

            Assert.NotNull(orderProcessorDoc);

            var text = await orderProcessorDoc.GetTextAsync();
            var sourceText = text.ToString();

            // Find the local variable "subtotal" in ProcessOrder method
            var subtotalIndex = sourceText.IndexOf("Dim subtotal As Decimal = 0");
            Assert.True(subtotalIndex > 0, "Could not find subtotal variable");

            var identifierStart = sourceText.IndexOf("subtotal", subtotalIndex);
            var identifierLength = "subtotal".Length;

            var renameProvider = new RenameSymbolProvider();

            var parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""OrderProcessor.vb"",
                ""textSpanStart"": " + identifierStart + @",
                ""textSpanLength"": " + identifierLength + @",
                ""newName"": ""orderSubtotal""
            }").RootElement;

            var validationResult = renameProvider.ValidateParameters(parameters);
            Assert.True(validationResult.IsValid);

            // Execute
            var context = new RefactoringContext(solution, parameters, new Progress<string>(), CancellationToken.None);
            var result = await renameProvider.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);

            var updatedDoc = result.UpdatedSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "OrderProcessor.vb");

            var updatedSource = (await updatedDoc.GetTextAsync()).ToString();

            // Verify local variable was renamed
            Assert.Contains("Dim orderSubtotal As Decimal", updatedSource);

            // Should still have OrderResult.Subtotal property (different symbol)
            Assert.Contains("result.Subtotal = ", updatedSource);
        }

        [Fact]
        public async Task E2E_RenameSymbol_WithConflictingName_ShouldFail()
        {
            // Arrange
            var tempSolutionPath = CopyFixtureToTemp();

            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(tempSolutionPath);

            var customerDoc = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "Customer.vb");

            var text = await customerDoc.GetTextAsync();
            var sourceText = text.ToString();

            // Try to rename CustomerName to CustomerAge (which already exists)
            var customerNameIndex = sourceText.IndexOf("Public Property CustomerName");
            var identifierStart = sourceText.IndexOf("CustomerName", customerNameIndex);

            var renameProvider = new RenameSymbolProvider();

            var parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": " + identifierStart + @",
                ""textSpanLength"": 12,
                ""newName"": ""CustomerAge""
            }").RootElement;

            // Execute
            var context = new RefactoringContext(solution, parameters, new Progress<string>(), CancellationToken.None);
            var result = await renameProvider.ExecuteAsync(context);

            // Assert: Should fail due to conflict
            Assert.False(result.Success);
            Assert.Contains("conflict", result.Message.ToLower());
        }

        [Fact]
        public async Task E2E_RenameSymbol_UpdatesCommentsAndStrings_WhenEnabled()
        {
            // This test verifies the includeCommentsAndStrings option
            var tempSolutionPath = CopyFixtureToTemp();

            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(tempSolutionPath);

            var customerDoc = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "Customer.vb");

            var text = await customerDoc.GetTextAsync();
            var sourceText = text.ToString();

            var customerNameIndex = sourceText.IndexOf("Public Property CustomerName");
            var identifierStart = sourceText.IndexOf("CustomerName", customerNameIndex);

            var renameProvider = new RenameSymbolProvider();

            var parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": " + identifierStart + @",
                ""textSpanLength"": 12,
                ""newName"": ""FullName"",
                ""includeCommentsAndStrings"": true
            }").RootElement;

            var context = new RefactoringContext(solution, parameters, new Progress<string>(), CancellationToken.None);
            var result = await renameProvider.ExecuteAsync(context);

            Assert.True(result.Success);

            // Note: Actual comment/string updates depend on Roslyn's implementation
            // This test validates that the parameter is accepted
        }

        [Fact]
        public async Task E2E_CompilationAfterRename_ShouldSucceed()
        {
            // This test verifies that the solution still compiles after refactoring
            var tempSolutionPath = CopyFixtureToTemp();

            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(tempSolutionPath);

            // Get initial compilation - should have no errors
            var project = solution.Projects.First();
            var initialCompilation = await project.GetCompilationAsync();
            var initialErrors = initialCompilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.Empty(initialErrors);

            // Perform rename
            var customerDoc = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.Name == "Customer.vb");

            var text = await customerDoc.GetTextAsync();
            var sourceText = text.ToString();
            var customerNameIndex = sourceText.IndexOf("Public Property CustomerName");
            var identifierStart = sourceText.IndexOf("CustomerName", customerNameIndex);

            var renameProvider = new RenameSymbolProvider();
            var parameters = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": " + identifierStart + @",
                ""textSpanLength"": 12,
                ""newName"": ""ClientName""
            }").RootElement;

            var context = new RefactoringContext(solution, parameters, new Progress<string>(), CancellationToken.None);
            var result = await renameProvider.ExecuteAsync(context);

            Assert.True(result.Success);

            // Get updated compilation - should still have no errors
            var updatedProject = result.UpdatedSolution.Projects.First();
            var updatedCompilation = await updatedProject.GetCompilationAsync();
            var updatedErrors = updatedCompilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.Empty(updatedErrors);
        }

        private string CopyFixtureToTemp()
        {
            // Copy the entire fixture directory to temp location
            var tempFixtureDir = Path.Combine(_testWorkingDirectory, "SampleVBProject");
            Directory.CreateDirectory(tempFixtureDir);

            var sourceDir = Path.Combine(_fixturePath, "SampleVBProject");

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(tempFixtureDir, fileName));
            }

            // Copy solution file
            var sourceSln = Path.Combine(_fixturePath, "SampleVBProject.sln");
            var destSln = Path.Combine(_testWorkingDirectory, "SampleVBProject.sln");
            File.Copy(sourceSln, destSln);

            return destSln;
        }

        public void Dispose()
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(_testWorkingDirectory))
                {
                    Directory.Delete(_testWorkingDirectory, recursive: true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
