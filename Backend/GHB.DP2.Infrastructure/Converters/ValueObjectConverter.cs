namespace GHB.DP2.Infrastructure.Converters;

using System.Reflection;
using Codehard.FileService.Contracts.Interfaces;

public class ValueObjectConverter<T>(Microsoft.EntityFrameworkCore.Storage.ValueConversion.ConverterMappingHints? mappingHints)
    : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<T, Guid>(static key => key.Value,
        static guid => CreateIdentityFrom(guid),
        mappingHints)
    where T : struct, IConvertibleIdentity<T, Guid>
{
    public ValueObjectConverter()
        : this(null)
    {
    }

    private static T CreateIdentityFrom(Guid value) => T.From(value);
}

public class ValueObjectComparer<T>() : Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<T>(static (left, right) => DoCompare(left, right),
    static instance => IsInitialized(instance) ? instance.GetHashCode() : 0)
    where T : struct, IConvertibleIdentity<T, Guid>
{
    private static readonly FieldInfo? InitializedFieldInfo =
        typeof(T).GetField("_isInitialized", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool IsInitialized(T identity)
    {
        // Safe accessibility bypass - this is required for EF Core value comparison
        // The field access is validated at startup and is part of the framework design
        if (InitializedFieldInfo == null)
        {
            throw new InvalidOperationException($"Type {typeof(T)} lacks required '_isInitialized' field for EF Core value comparison");
        }

        return (bool)InitializedFieldInfo.GetValue(identity)!;
    }

    private static bool DoCompare(T left, T right)
    {
        var leftInitialized = IsInitialized(left);
        var rightInitialized = IsInitialized(right);

        // if neither are initialized, then they're equal
        if (!leftInitialized && !rightInitialized)
        {
            return true;
        }

        return leftInitialized && rightInitialized && left.Value.Equals(right.Value);
    }
}