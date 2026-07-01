namespace GHB.DP2.Application.Extensions;

using System.Globalization;

public static class CurrencyExtension
{
    public static string ToCurrencyStringWithComma(this decimal value)
    {
        // Use N2 format with InvariantCulture which already uses comma for thousands and period for decimal
        var result = value.ToString("N2", CultureInfo.InvariantCulture);

        return result;
    }

    public static string? ToCurrencyStringWithComma(this decimal? value)
    {
        if (value.HasValue)
        {
            return ToCurrencyStringWithComma(value.Value);
        }

        return null;
    }

    public static string ToCurrencyStringNoDecimal(this decimal value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    public static string? ToCurrencyStringNoDecimal(this decimal? value) =>
        value.HasValue ? ToCurrencyStringNoDecimal(value.Value) : null;

    public static string ToCurrencyStringWithCommaTwoDigit(this decimal value)
    {
        // Convert the decimal value to a string with two decimal places
        var formattedValue = value.ToString("N2", CultureInfo.InvariantCulture);

        // Replace the decimal point with a comma
        formattedValue = formattedValue.Replace('.', ',');

        // Remove the last two digits after the comma
        var parts = formattedValue.Split(',');

        if (parts.Length > 1)
        {
            formattedValue = $"{parts[0]},{parts[1].Substring(0, 2)}";
        }

        // Return the formatted string
        return formattedValue;
    }

    public static string? ToCurrencyStringWithCommaTwoDigit(this decimal? value)
    {
        if (value.HasValue)
        {
            return ToCurrencyStringWithCommaTwoDigit(value.Value);
        }

        return null;
    }
}