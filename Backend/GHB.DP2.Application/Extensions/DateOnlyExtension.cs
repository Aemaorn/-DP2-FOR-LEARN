namespace GHB.DP2.Application.Extensions;

using System.Globalization;

public static class DateOnlyExtension
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "dd-MM-yyyy",
        "M/d/yyyy",
        "d-M-yyyy"
    ];

    public static bool TryParseFlexible(this string? input, out DateOnly date)
    {
        if (input == null)
        {
            date = default;

            return false;
        }

        foreach (var format in Formats)
        {
            if (DateOnly.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        // ถ้าลอง exact ไม่สำเร็จ ลอง parse ปกติ
        return DateOnly.TryParse(input, out date);
    }
}