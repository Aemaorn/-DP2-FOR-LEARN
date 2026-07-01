namespace GHB.DP2.Api.Tests;

using System.Reflection;
using Microsoft.Extensions.Logging;
using Middlewares;
using Moq;

public class JsonFlattenTests
{
    [Theory]
    [InlineData("{\"password\":\"secretpass\"}", "password", "***")]
    [InlineData("{\"email\":\"user@example.com\"}", "email", "***")]
    [InlineData("{\"token\":\"abc123\"}", "token", "***")]
    [InlineData("{\"access_token\":\"abc123\"}", "access_token", "***")]
    [InlineData("{\"refresh_token\":\"abc123\"}", "refresh_token", "***")]
    [InlineData("{\"secret\":\"mysecret\"}", "secret", "***")]
    [InlineData("{\"api_key\":\"key123\"}", "api_key", "***")]
    [InlineData("{\"apikey\":\"key123\"}", "apikey", "***")]
    [InlineData("{\"citizen\":\"123456\"}", "citizen", "***")]

    // Some weird naming keys, but ok...
    [InlineData("{\"user_password\":\"mypw\"}", "user_password", "***")] // partial match
    [InlineData("{\"EMAIL_ADDRESS\":\"me@example.com\"}", "EMAIL_ADDRESS", "***")] // case-insensitive match
    [InlineData("{\"token_value\":\"abc123\"}", "token_value", "***")] // token in middle
    [InlineData("{\"myapikey\":\"abc123\"}", "myapikey", "***")] // prefix match
    [InlineData("{\"access_token_2\":\"abc123\"}", "access_token_2", "***")] // suffix match
    [InlineData("{\"cITiZEn\":\"123456\"}", "cITiZEn", "***")]

    // Control tests for non-sensitive keys
    [InlineData("{\"name\":\"John\"}", "name", "John")]
    [InlineData("{\"age\":30}", "age", "30")]
    [InlineData("{\"status\":\"active\"}", "status", "active")]
    public void Masking_Should_Work_On_Simple_Values(string json, string expectedKey, string expectedValue)
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var method = typeof(LogContextEnrichMiddleware)
            .GetMethod("GetFlattenedRequestJson", BindingFlags.Static | BindingFlags.NonPublic)!;

        // Act
        var result = (Dictionary<string, string>)method.Invoke(null, [json, logger.Object])!;

        // Assert
        Assert.True(result.ContainsKey(expectedKey));
        Assert.Equal(expectedValue, result[expectedKey]);
    }

    [Fact]
    public void Flatten_Should_Handle_Nested_Objects_And_Masking()
    {
        // Arrange
        const string json = @"
        {
            ""user"": {
                ""username"": ""user1"",
                ""profile"": {
                    ""email"": ""user@example.com"",
                    ""name"": ""John Doe""
                },
                ""password"": ""123456""
            },
            ""status"": ""active""
        }";

        var logger = new Mock<ILogger>();
        var method = typeof(LogContextEnrichMiddleware)
            .GetMethod("GetFlattenedRequestJson", BindingFlags.Static | BindingFlags.NonPublic)!;

        // Act
        var result = (Dictionary<string, string>)method.Invoke(null, [json, logger.Object])!;

        // Assert
        Assert.Equal("user1", result["user_username"]);
        Assert.Equal("***", result["user_profile_email"]);
        Assert.Equal("John Doe", result["user_profile_name"]);
        Assert.Equal("active", result["status"]);
        Assert.Equal("***", result["user_password"]);
    }

    [Fact]
    public void Flatten_Should_Handle_Arrays_With_Masking()
    {
        // Arrange
        const string json = @"
        {
            ""tokens"": [""token1"", ""token2""],
            ""names"": [""Alice"", ""Bob""]
        }";

        var logger = new Mock<ILogger>();
        var method = typeof(LogContextEnrichMiddleware)
            .GetMethod("GetFlattenedRequestJson", BindingFlags.Static | BindingFlags.NonPublic)!;

        // Act
        var result = (Dictionary<string, string>)method.Invoke(null, [json, logger.Object])!;

        // Assert
        Assert.Equal("***", result["tokens_0"]);
        Assert.Equal("***", result["tokens_1"]);

        Assert.Equal("Alice", result["names_0"]);
        Assert.Equal("Bob", result["names_1"]);
    }
}