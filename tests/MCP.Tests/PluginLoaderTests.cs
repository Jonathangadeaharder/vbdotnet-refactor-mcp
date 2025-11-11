using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for the PluginLoader system.
/// Tests plugin discovery and registration functionality.
/// </summary>
public class PluginLoaderTests
{
    private readonly Mock<ILogger<RefactoringWorker.PluginLoader>> _loggerMock;

    public PluginLoaderTests()
    {
        _loggerMock = new Mock<ILogger<RefactoringWorker.PluginLoader>>();
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
            new RefactoringWorker.PluginLoader(_loggerMock.Object));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void LoadPlugins_WithNonExistentDirectory_ShouldNotThrow()
    {
        // Arrange
        var loader = new RefactoringWorker.PluginLoader(_loggerMock.Object);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var exception = Record.Exception(() =>
            loader.LoadPlugins(nonExistentPath));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void LoadPlugins_WithEmptyDirectory_ShouldLoadZeroProviders()
    {
        // Arrange
        var loader = new RefactoringWorker.PluginLoader(_loggerMock.Object);
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            loader.LoadPlugins(tempDir);
            var providerNames = loader.GetProviderNames();

            // Assert
            Assert.Empty(providerNames);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetProvider_WithNonExistentName_ShouldReturnNull()
    {
        // Arrange
        var loader = new RefactoringWorker.PluginLoader(_loggerMock.Object);

        // Act
        var provider = loader.GetProvider("NonExistentProvider");

        // Assert
        Assert.Null(provider);
    }

    [Fact]
    public void GetProviderNames_WhenNoPluginsLoaded_ShouldReturnEmptyCollection()
    {
        // Arrange
        var loader = new RefactoringWorker.PluginLoader(_loggerMock.Object);

        // Act
        var names = loader.GetProviderNames();

        // Assert
        Assert.NotNull(names);
        Assert.Empty(names);
    }
}
