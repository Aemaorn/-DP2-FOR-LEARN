namespace GHB.DP2.Application.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// This method converts a string to Thai number.
    /// </summary>
    /// <param name="src">The string to convert</param>
    /// <returns>The converted string</returns>
    public static string ToThaiNumber(this string src)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return src;
        }

        if (src.Length == 0)
        {
            return src;
        }

        // Up to 100kB
        const uint limit = 102_400;
        const int charSize = sizeof(char);
        const int startRange = 48;
        const int endRange = 57;

        return src.Length * charSize > limit
            ? ConvertToThaiNumberOnHeap(src)
            : ConvertToThaiNumberOnStack(src);

        static string ConvertToThaiNumberOnStack(string src)
        {
            Span<char> chars = stackalloc char[src.Length];

            for (var idx = 0; idx < src.Length; idx++)
            {
                chars[idx] = Convert(src[idx]);
            }

            return new string(chars);
        }

        static string ConvertToThaiNumberOnHeap(string src)
        {
            var chars = src.ToCharArray();

            for (var idx = 0; idx < src.Length; idx++)
            {
                chars[idx] = Convert(src[idx]);
            }

            return new string(chars);
        }

        static char Convert(char c)
        {
            var charCode = (int)c;

            return charCode is < startRange or > endRange ? c : (char)(charCode + 3616);
        }
    }
}