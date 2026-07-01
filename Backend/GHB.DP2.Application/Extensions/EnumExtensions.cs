namespace GHB.DP2.Application.Extensions;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);

        if (name is null)
        {
            return value.ToString();
        }

        var field = type.GetField(name);

        if (field is null)
        {
            return name;
        }

        // [Description("...")]
        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute da &&
            !string.IsNullOrWhiteSpace(da.Description))
        {
            return da.Description;
        }

        // [Display(Name="...", Description="...")]
        if (Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) is DisplayAttribute dp)
        {
            return dp.Description ?? dp.Name ?? name;
        }

        return name;
    }

    // Nice for [Flags] enums: joins each flag's description
    public static string GetFlagsDescription(this Enum value, string separator = ", ")
    {
        var type = value.GetType();

        if (!type.IsDefined(typeof(FlagsAttribute), false))
        {
            return value.GetDescription();
        }

        var vv = Convert.ToUInt64(value);

        if (vv == 0)
        {
            return value.GetDescription();
        }

        var parts = new List<string>();

        foreach (Enum flag in Enum.GetValues(type))
        {
            var fv = Convert.ToUInt64(flag);
            if (fv != 0 && (vv & fv) == fv)
            {
                parts.Add(flag.GetDescription());
            }
        }

        return string.Join(separator, parts);
    }
}