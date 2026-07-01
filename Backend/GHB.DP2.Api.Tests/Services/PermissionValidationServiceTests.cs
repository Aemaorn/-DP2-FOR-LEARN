namespace GHB.DP2.Api.Tests.Services;

using GHB.DP2.Application.Services;

public class NormalizePathTests
{
    [Theory]
    [InlineData("/st/st005", "/st/st005")]
    [InlineData("/api/st/st005", "/st/st005")]
    [InlineData("st/st005", "/st/st005")]
    [InlineData("/st/st005/", "/st/st005")]
    [InlineData("/ST/ST005", "/st/st005")]
    [InlineData("/api/ST/ST005/", "/st/st005")]
    public void NormalizePath_StandardPaths_ReturnsNormalized(string input, string expected)
    {
        var result = PermissionValidationService.NormalizePath(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/st/st005/{id}", "/st/st005")]
    [InlineData("/st/st004/{Code}", "/st/st004")]
    [InlineData("/api/st/st005/{id}", "/st/st005")]
    public void NormalizePath_DynamicSegments_StripsParameters(string input, string expected)
    {
        var result = PermissionValidationService.NormalizePath(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    public void NormalizePath_NullOrEmpty_ReturnsEmpty(string? input, string expected)
    {
        var result = PermissionValidationService.NormalizePath(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizePath_ApiPrefixOnly_ReturnsEmpty()
    {
        var result = PermissionValidationService.NormalizePath("/api");

        Assert.Equal("", result);
    }

    [Fact]
    public void NormalizePath_ApiPrefixNotGreedy_PreservesApiFoo()
    {
        var result = PermissionValidationService.NormalizePath("/apifoo/bar");

        Assert.Equal("/apifoo/bar", result);
    }

    [Theory]
    [InlineData("/st/st005/{Id:guid}", "/st/st005")]
    [InlineData("/st/st003/{Id:guid}/attachments", "/st/st003")]
    [InlineData("st/st006/{Id:guid}", "/st/st006")]
    public void NormalizePath_TypedDynamicSegments_StripsFromBrace(string input, string expected)
    {
        var result = PermissionValidationService.NormalizePath(input);

        Assert.Equal(expected, result);
    }
}
