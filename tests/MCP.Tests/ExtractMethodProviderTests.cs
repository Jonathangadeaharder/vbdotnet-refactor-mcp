using System;
using System.Text.Json;
using Xunit;
using MCP.Plugins.ExtractMethod;

namespace MCP.Tests
{
    public class ExtractMethodProviderTests
    {
        private readonly ExtractMethodProvider _provider;

        public ExtractMethodProviderTests()
        {
            _provider = new ExtractMethodProvider();
        }

        [Fact]
        public void Provider_ShouldHaveCorrectName()
        {
            Assert.Equal("ExtractMethod", _provider.Name);
        }

        [Fact]
        public void Provider_ShouldHaveDescription()
        {
            Assert.NotNull(_provider.Description);
            Assert.NotEmpty(_provider.Description);
            Assert.Contains("Extracts", _provider.Description);
        }

        [Fact]
        public void ValidateParameters_WithMissingTargetFile_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("targetFile", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithEmptyTargetFile_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": """",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("targetFile", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithMissingTextSpanStart_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("textSpanStart", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithInvalidTextSpanStart_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": ""not a number"",
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("textSpanStart", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithMissingTextSpanLength_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""newMethodName"": ""ExtractedMethod""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("textSpanLength", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithInvalidTextSpanLength_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": ""not a number"",
                ""newMethodName"": ""ExtractedMethod""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("textSpanLength", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithMissingNewMethodName_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("newMethodName", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithEmptyNewMethodName_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": """"
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("newMethodName", result.ErrorMessage);
        }

        [Theory]
        [InlineData("123Invalid")]
        [InlineData("Invalid-Name")]
        [InlineData("Invalid.Name")]
        [InlineData("Invalid Name")]
        public void ValidateParameters_WithInvalidMethodName_ShouldFail(string invalidName)
        {
            var json = JsonDocument.Parse($@"{{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""{invalidName}""
            }}").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("valid identifier", result.ErrorMessage);
        }

        [Fact]
        public void ValidateParameters_WithAllRequiredFields_ShouldSucceed()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateParameters_WithValidOptionalMakeStatic_ShouldSucceed()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod"",
                ""makeStatic"": true
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateParameters_WithInvalidMakeStatic_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod"",
                ""makeStatic"": ""yes""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("makeStatic", result.ErrorMessage);
        }

        [Theory]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("friend")]
        public void ValidateParameters_WithValidAccessModifier_ShouldSucceed(string accessModifier)
        {
            var json = JsonDocument.Parse($@"{{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod"",
                ""accessModifier"": ""{accessModifier}""
            }}").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateParameters_WithInvalidAccessModifier_ShouldFail()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod"",
                ""accessModifier"": ""invalid""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.False(result.IsValid);
            Assert.Contains("accessModifier", result.ErrorMessage);
        }

        [Theory]
        [InlineData("_ValidName")]
        [InlineData("ValidName123")]
        [InlineData("valid_name")]
        [InlineData("VALIDNAME")]
        public void ValidateParameters_WithValidMethodNames_ShouldSucceed(string methodName)
        {
            var json = JsonDocument.Parse($@"{{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""{methodName}""
            }}").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateParameters_WithAllOptionalParameters_ShouldSucceed()
        {
            var json = JsonDocument.Parse(@"{
                ""targetFile"": ""Customer.vb"",
                ""textSpanStart"": 100,
                ""textSpanLength"": 50,
                ""newMethodName"": ""ExtractedMethod"",
                ""makeStatic"": false,
                ""accessModifier"": ""private""
            }").RootElement;

            var result = _provider.ValidateParameters(json);

            Assert.True(result.IsValid);
        }
    }
}
