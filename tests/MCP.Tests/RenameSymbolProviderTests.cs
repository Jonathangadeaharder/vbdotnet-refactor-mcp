using System.Text.Json;
using MCP.Plugins.RenameSymbol;
using Xunit;

namespace MCP.Tests;

/// <summary>
/// Unit tests for the RenameSymbol plugin.
/// Tests parameter validation and plugin metadata.
/// </summary>
public class RenameSymbolProviderTests
{
    [Fact]
    public void Provider_ShouldHaveCorrectName()
    {
        // Arrange
        var provider = new RenameSymbolProvider();

        // Act & Assert
        Assert.Equal("RenameSymbol", provider.Name);
    }

    [Fact]
    public void Provider_ShouldHaveDescription()
    {
        // Arrange
        var provider = new RenameSymbolProvider();

        // Act & Assert
        Assert.NotNull(provider.Description);
        Assert.NotEmpty(provider.Description);
    }

    [Fact]
    public void ValidateParameters_WithMissingTargetFile_ShouldFail()
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse(@"{
            ""textSpanStart"": 100,
            ""textSpanLength"": 10,
            ""newName"": ""NewName""
        }");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("targetFile", result.ErrorMessage);
    }

    [Fact]
    public void ValidateParameters_WithMissingTextSpanStart_ShouldFail()
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse(@"{
            ""targetFile"": ""MyClass.vb"",
            ""textSpanLength"": 10,
            ""newName"": ""NewName""
        }");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("textSpanStart", result.ErrorMessage);
    }

    [Fact]
    public void ValidateParameters_WithMissingTextSpanLength_ShouldFail()
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse(@"{
            ""targetFile"": ""MyClass.vb"",
            ""textSpanStart"": 100,
            ""newName"": ""NewName""
        }");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("textSpanLength", result.ErrorMessage);
    }

    [Fact]
    public void ValidateParameters_WithMissingNewName_ShouldFail()
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse(@"{
            ""targetFile"": ""MyClass.vb"",
            ""textSpanStart"": 100,
            ""textSpanLength"": 10
        }");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("newName", result.ErrorMessage);
    }

    [Fact]
    public void ValidateParameters_WithEmptyNewName_ShouldFail()
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse(@"{
            ""targetFile"": ""MyClass.vb"",
            ""textSpanStart"": 100,
            ""textSpanLength"": 10,
            ""newName"": """"
        }");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("newName", result.ErrorMessage);
    }

    [Fact]
    public void ValidateParameters_WithAllRequiredFields_ShouldSucceed()
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse(@"{
            ""targetFile"": ""MyClass.vb"",
            ""textSpanStart"": 100,
            ""textSpanLength"": 10,
            ""newName"": ""NewMethodName""
        }");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateParameters_WithOptionalIncludeCommentsAndStrings_ShouldSucceed()
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse(@"{
            ""targetFile"": ""MyClass.vb"",
            ""textSpanStart"": 100,
            ""textSpanLength"": 10,
            ""newName"": ""NewMethodName"",
            ""includeCommentsAndStrings"": true
        }");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("123")]
    [InlineData("null")]
    public void ValidateParameters_WithInvalidTextSpanStartType_ShouldFail(string invalidValue)
    {
        // Arrange
        var provider = new RenameSymbolProvider();
        var json = JsonDocument.Parse($@"{{
            ""targetFile"": ""MyClass.vb"",
            ""textSpanStart"": {invalidValue},
            ""textSpanLength"": 10,
            ""newName"": ""NewName""
        }}");

        // Act
        var result = provider.ValidateParameters(json.RootElement);

        // Assert
        Assert.False(result.IsValid);
    }
}
