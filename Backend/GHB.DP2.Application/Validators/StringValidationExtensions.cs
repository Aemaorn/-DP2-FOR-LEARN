namespace GHB.DP2.Application.Validators;

using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentValidation;
using GHB.DP2.Domain.SystemUtility;

public static partial class StringValidationExtensions
{
    [GeneratedRegex(@"^[a-zA-Z0-9\u0E00-\u0E7F\s()\-@.]*$")]
    private static partial Regex AlphanumericWithThaiRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9\u0E00-\u0E7F()\-@.]*$")]
    private static partial Regex AlphanumericWithThaiNoSpaceRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9\s()\-@.]*$")]
    private static partial Regex AlphanumericRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9()\-@.]*$")]
    private static partial Regex AlphanumericNoSpaceRegex();

    public static IRuleBuilderOptions<T, string?> MustBeAlphanumericWithThai<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        bool allowSpaces = true)
    {
        var regex = allowSpaces ? AlphanumericWithThaiRegex() : AlphanumericWithThaiNoSpaceRegex();
        var message = allowSpaces
            ? "Field must contain only alphanumeric characters, Thai characters, spaces, and special characters ( ) - @ ."
            : "Field must contain only alphanumeric characters, Thai characters, and special characters ( ) - @ .";

        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || regex.IsMatch(value))
            .WithMessage(message);
    }

    public static IRuleBuilderOptions<T, string?> MustBeAlphanumeric<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        bool allowSpaces = true)
    {
        var regex = allowSpaces ? AlphanumericRegex() : AlphanumericNoSpaceRegex();
        var message = allowSpaces
            ? "Field must contain only alphanumeric characters, spaces, and special characters ( ) - @ ."
            : "Field must contain only alphanumeric characters and special characters ( ) - @ .";

        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || regex.IsMatch(value))
            .WithMessage(message);
    }

    public static void AddAddressValidationRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, Address>> addressSelector,
        string propertyName = "Address")
        where T : class
    {
        validator.RuleFor(addressSelector)
            .NotNull()
            .WithMessage($"{propertyName} is required");

        var compiledSelector = addressSelector.Compile();

        validator.When(x => compiledSelector(x) != null, () =>
        {
            validator.RuleFor(x => compiledSelector(x).HouseNumber)
                .MustBeAlphanumericWithThai()
                .WithName($"{propertyName}.HouseNumber");

            validator.RuleFor(x => compiledSelector(x).RoomNumber)
                .MustBeAlphanumericWithThai()
                .WithName($"{propertyName}.RoomNumber");

            validator.RuleFor(x => compiledSelector(x).Floor)
                .MustBeAlphanumericWithThai()
                .WithName($"{propertyName}.Floor");

            validator.RuleFor(x => compiledSelector(x).VillageName)
                .MustBeAlphanumericWithThai()
                .WithName($"{propertyName}.VillageName");

            validator.RuleFor(x => compiledSelector(x).Moo)
                .MustBeAlphanumericWithThai()
                .WithName($"{propertyName}.Moo");

            validator.RuleFor(x => compiledSelector(x).Allay)
                .MustBeAlphanumericWithThai()
                .WithName($"{propertyName}.Allay");

            validator.RuleFor(x => compiledSelector(x).Road)
                .MustBeAlphanumericWithThai()
                .WithName($"{propertyName}.Road");

            validator.RuleFor(x => compiledSelector(x).RawProvinceCode)
                .MustBeAlphanumeric(allowSpaces: false)
                .WithName($"{propertyName}.ProvinceCode");

            validator.RuleFor(x => compiledSelector(x).RawDistrictCode)
                .MustBeAlphanumeric(allowSpaces: false)
                .WithName($"{propertyName}.DistrictCode");

            validator.RuleFor(x => compiledSelector(x).RawSubDistrictCode)
                .MustBeAlphanumeric(allowSpaces: false)
                .WithName($"{propertyName}.SubDistrictCode");

            validator.RuleFor(x => compiledSelector(x).PostalCode)
                .MustBeAlphanumeric(allowSpaces: false)
                .When(x => !string.IsNullOrEmpty(compiledSelector(x).PostalCode))
                .WithName($"{propertyName}.PostalCode");
        });
    }

    public static IRuleBuilderOptions<T, Address> MustHaveValidAddress<T>(
        this IRuleBuilder<T, Address> ruleBuilder)
    {
        return ruleBuilder
            .NotNull()
            .WithMessage("Address is required")
            .ChildRules(address =>
            {
                address.RuleFor(x => x.HouseNumber)
                    .MustBeAlphanumericWithThai();

                address.RuleFor(x => x.RoomNumber)
                    .MustBeAlphanumericWithThai();

                address.RuleFor(x => x.Floor)
                    .MustBeAlphanumericWithThai();

                address.RuleFor(x => x.VillageName)
                    .MustBeAlphanumericWithThai();

                address.RuleFor(x => x.Moo)
                    .MustBeAlphanumericWithThai();

                address.RuleFor(x => x.Allay)
                    .MustBeAlphanumericWithThai();

                address.RuleFor(x => x.Road)
                    .MustBeAlphanumericWithThai();

                address.RuleFor(x => x.RawProvinceCode)
                    .MustBeAlphanumeric(allowSpaces: false);

                address.RuleFor(x => x.RawDistrictCode)
                    .MustBeAlphanumeric(allowSpaces: false);

                address.RuleFor(x => x.RawSubDistrictCode)
                    .MustBeAlphanumeric(allowSpaces: false);

                address.RuleFor(x => x.PostalCode)
                    .MustBeAlphanumeric(allowSpaces: false)
                    .When(x => !string.IsNullOrEmpty(x.PostalCode));
            });
    }
}