using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MCP.Tests.Integration
{
    [Trait("Category", "Integration")]
    [Trait("Category", "RealVBNet")]
    public class RealVBNetRefactoringTests : IDisposable
    {
        private readonly string _fixturePath;
        private readonly string _solutionPath;

        public RealVBNetRefactoringTests()
        {
            // Get the path to the VB.NET fixture solution
            var testDirectory = Directory.GetCurrentDirectory();
            var repoRoot = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", ".."));
            _fixturePath = Path.Combine(repoRoot, "tests", "fixtures");
            _solutionPath = Path.Combine(_fixturePath, "SampleVBProject.sln");
        }

        [Fact]
        public void VBNetFixture_SolutionFile_ShouldExist()
        {
            // Verify the fixture solution exists
            Assert.True(File.Exists(_solutionPath), $"Solution file should exist at {_solutionPath}");
        }

        [Fact]
        public void VBNetFixture_ProjectFile_ShouldExist()
        {
            // Verify the project file exists
            var projectPath = Path.Combine(_fixturePath, "SampleVBProject", "SampleVBProject.vbproj");
            Assert.True(File.Exists(projectPath), $"Project file should exist at {projectPath}");
        }

        [Fact]
        public void VBNetFixture_SourceFiles_ShouldExist()
        {
            // Verify all VB source files exist
            var projectDir = Path.Combine(_fixturePath, "SampleVBProject");

            Assert.True(File.Exists(Path.Combine(projectDir, "Customer.vb")));
            Assert.True(File.Exists(Path.Combine(projectDir, "OrderProcessor.vb")));
            Assert.True(File.Exists(Path.Combine(projectDir, "StringHelpers.vb")));
        }

        [Fact]
        public async Task VBNetFixture_LoadSolution_ShouldSucceed()
        {
            // Test that we can load the VB.NET solution using MSBuildWorkspace
            using var workspace = MSBuildWorkspace.Create();

            var solution = await workspace.OpenSolutionAsync(_solutionPath, cancellationToken: CancellationToken.None);

            Assert.NotNull(solution);
            Assert.Single(solution.Projects); // Should have one project
        }

        [Fact]
        public async Task VBNetFixture_LoadSolution_ShouldContainExpectedClasses()
        {
            // Test that the solution contains our expected types
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(_solutionPath, cancellationToken: CancellationToken.None);

            var project = Assert.Single(solution.Projects);
            var compilation = await project.GetCompilationAsync();

            Assert.NotNull(compilation);

            // Check for our expected types
            var customerType = compilation.GetTypeByMetadataName("Business.Customer");
            var orderProcessorType = compilation.GetTypeByMetadataName("Business.OrderProcessor");

            Assert.NotNull(customerType);
            Assert.NotNull(orderProcessorType);
        }

        [Fact]
        public async Task VBNetFixture_CustomerClass_ShouldHaveExpectedMembers()
        {
            // Test that Customer class has the expected structure
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(_solutionPath, cancellationToken: CancellationToken.None);

            var project = Assert.Single(solution.Projects);
            var compilation = await project.GetCompilationAsync();
            var customerType = compilation.GetTypeByMetadataName("Business.Customer");

            Assert.NotNull(customerType);

            // Check for expected members
            var members = customerType.GetMembers();
            var memberNames = members.Select(m => m.Name).ToHashSet();

            Assert.Contains("CustomerName", memberNames);
            Assert.Contains("CustomerAge", memberNames);
            Assert.Contains("EmailAddress", memberNames);
            Assert.Contains("IsEligibleForPremium", memberNames);
            Assert.Contains("GetDisplayName", memberNames);
            Assert.Contains("CalculateDiscount", memberNames);
        }

        [Fact]
        public async Task VBNetFixture_FindSymbol_CustomerName_ShouldSucceed()
        {
            // Test that we can find a specific symbol (CustomerName property)
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(_solutionPath, cancellationToken: CancellationToken.None);

            var project = Assert.Single(solution.Projects);
            var compilation = await project.GetCompilationAsync();
            var customerType = compilation.GetTypeByMetadataName("Business.Customer");

            var customerNameProperty = customerType.GetMembers("CustomerName").FirstOrDefault();

            Assert.NotNull(customerNameProperty);
            Assert.Equal("CustomerName", customerNameProperty.Name);
            Assert.Equal(SymbolKind.Property, customerNameProperty.Kind);
        }

        [Fact]
        public async Task VBNetFixture_GetSyntaxTree_ShouldContainVBCode()
        {
            // Test that we can access the syntax tree
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(_solutionPath, cancellationToken: CancellationToken.None);

            var project = Assert.Single(solution.Projects);
            var documents = project.Documents.ToList();

            Assert.NotEmpty(documents);
            Assert.True(documents.Count >= 3); // Should have at least 3 VB files

            // Check that one of them is Customer.vb
            var customerDoc = documents.FirstOrDefault(d => d.Name == "Customer.vb");
            Assert.NotNull(customerDoc);

            var syntaxTree = await customerDoc.GetSyntaxTreeAsync();
            Assert.NotNull(syntaxTree);

            var root = await syntaxTree.GetRootAsync();
            var sourceText = root.ToFullString();

            // Verify it contains VB.NET code
            Assert.Contains("Public Class Customer", sourceText);
            Assert.Contains("CustomerName", sourceText);
        }

        [Fact]
        public async Task VBNetFixture_CompilationDiagnostics_ShouldHaveNoErrors()
        {
            // Test that the VB.NET code compiles without errors
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(_solutionPath, cancellationToken: CancellationToken.None);

            var project = Assert.Single(solution.Projects);
            var compilation = await project.GetCompilationAsync();

            var diagnostics = compilation.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            // Should have no compilation errors
            Assert.Empty(errors);
        }

        [Fact]
        public async Task VBNetFixture_StringHelpersModule_ShouldExist()
        {
            // Test that the StringHelpers module exists and has expected functions
            using var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(_solutionPath, cancellationToken: CancellationToken.None);

            var project = Assert.Single(solution.Projects);
            var compilation = await project.GetCompilationAsync();

            var stringHelpersType = compilation.GetTypeByMetadataName("Utilities.StringHelpers");

            Assert.NotNull(stringHelpersType);

            var members = stringHelpersType.GetMembers();
            var memberNames = members.Select(m => m.Name).ToHashSet();

            Assert.Contains("ToTitleCase", memberNames);
            Assert.Contains("TruncateString", memberNames);
            Assert.Contains("IsValidEmail", memberNames);
            Assert.Contains("CountWords", memberNames);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
