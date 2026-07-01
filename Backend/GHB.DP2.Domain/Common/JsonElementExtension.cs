namespace GHB.DP2.Domain.Common;

using System.Text.Json;

public static class JsonElementExtension
{
    /// <summary>
    /// Converts a JsonElement to a boolean value if possible.
    /// </summary>
    /// <param name="element">The JsonElement to convert.</param>
    /// <returns>A boolean value represented by the JsonElement.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the JsonElement cannot be converted to a boolean.
    /// </exception>
    public static bool ToBoolean(this JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"Cannot convert {element.ValueKind} to boolean.");
    }
}