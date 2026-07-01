namespace GHB.DP2.Domain.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using Plan;
using Xunit;

public class AnnouncementPlanFormatTests
{
    [InlineData("AN2300001")]
    [InlineData("AN2399999")]
    [InlineData("AN0000001")]
    [InlineData("AN9900001")]
    [Theory]
    public void PlanAnnouncementNumberRegex_ShouldMatchValidFormat(string input)
    {
        // Arrange
        var regex = RetrieveRegexFromPlanAnnouncement();

        // Act
        var isMatch = regex.IsMatch(input);

        // Assert
        Assert.True(isMatch);
    }

    [InlineData("DP230001", "Announcement number too short (4 digits)")]
    [InlineData("DP23000001", "Announcement number too long (6 digits)")]
    [InlineData("DP2300001X", "Extra character at the end")]
    [InlineData("XDP2300001", "Extra character at the beginning")]
    [InlineData("dp2300001", "Lowercase prefix")]
    [InlineData("DP230A001", "Non-digit in announcement number")]
    [InlineData("DP2A0001", "Non-digit in year")]
    [InlineData("AP2300001", "Wrong prefix")]
    [InlineData("", "Empty string")]
    [InlineData("DP", "Prefix only")]
    [Theory]
    public void PlanAnnouncementNumberRegex_ShouldNotMatchInvalidFormat(string input, string description)
    {
        // Arrange
        var regex = RetrieveRegexFromPlanAnnouncement();

        // Act
        var match = regex.IsMatch(input);

        // Assert
        Assert.False(match, description);
    }

    [InlineData("AN2300001", "23", "00001")]
    [InlineData("AN2399999", "23", "99999")]
    [InlineData("AN0012345", "00", "12345")]
    [Theory]
    public void PlanAnnouncementNumberRegex_ShouldCaptureCorrectGroups(string input, string expectedYear, string expectedNumber)
    {
        // Arrange
        var regex = RetrieveRegexFromPlanAnnouncement();

        // Act
        var match = regex.Match(input);

        // Assert
        Assert.True(match.Success, $"Match should succeed for valid input: {input}");
        Assert.Equal(expectedYear, match.Groups["year"].Value);
        Assert.Equal(expectedNumber, match.Groups["announcement_number"].Value);
    }

    private static Regex RetrieveRegexFromPlanAnnouncement()
    {
        var type = typeof(PlanAnnouncementNumber);
        var methodInfo =
            type.GetMethod("PlanAnnouncementNumberRegex", BindingFlags.Static | BindingFlags.NonPublic);

        var regex = (Regex)methodInfo!.Invoke(null, [])!;

        return regex;
    }
}