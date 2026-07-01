namespace GHB.DP2.Application.Features.SystemUtility.SuParameter.Validator;

using GHB.DP2.Application.Features.SystemUtility.SuParameter.DTO;
using FluentValidation;

public class CreateParameterRequestValidator : Validator<CreateParameterRequest>
{
    public CreateParameterRequestValidator()
    {
        this.RuleFor(x => x.Parameters)
            .Must(ValidateUniqueSequences)
            .WithMessage("Duplicate parameter sequences found");
    }

    private static bool ValidateUniqueSequences(ParameterKeyValue[] parameters)
    {
        var duplicateKeys = parameters
            .Where(p => p.Value != null)
            .GroupBy(p => p.Value!.Sequence)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        return !duplicateKeys.Any();
    }
}
