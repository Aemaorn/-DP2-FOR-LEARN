namespace GHB.DP2.Application.Extensions;

public static class ConditionalHandlerExtensions
{
    public static void IfNotNull<T>(this T? value, Action<T> apply)
        where T : class
    {
        if (value is not null)
        {
            apply(value);
        }
    }
}