namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CreateSuVendorCommand
{
    public SuVendorNationality Nationality { get; init; }

    public SuVendorType Type { get; init; }

    public string EntrepreneurType { get; init; }

    public string TaxpayerIdentificationNo { get; init; }

    public string EstablishmentName { get; init; }

    public string PlaceName { get; init; }

    public Address Address { get; init; }

    public string? Tel { get; init; }

    public string? Fax { get; init; }

    public string SapVendorNumber { get; init; }

    public string SapBranchNumber { get; init; }

    public string Email { get; init; }
}

public class AcceptorRequestValidator : Validator<CreateSuVendorCommand>
{
    public AcceptorRequestValidator()
    {
        this.RuleFor(r => r.EntrepreneurType)
            .MustBeAlphanumericWithThai()
            .MaximumLength(150);

        this.RuleFor(r => r.TaxpayerIdentificationNo)
            .MustBeAlphanumericWithThai()
            .MaximumLength(20);

        this.RuleFor(r => r.EstablishmentName)
            .MustBeAlphanumericWithThai()
            .MaximumLength(150);

        this.RuleFor(r => r.PlaceName)
            .MustBeAlphanumericWithThai()
            .MaximumLength(150);

        this.RuleFor(r => r.Tel)
            .MustBeAlphanumericWithThai()
            .MaximumLength(25);

        this.RuleFor(r => r.Fax)
            .MustBeAlphanumericWithThai()
            .MaximumLength(25);

        this.RuleFor(r => r.SapVendorNumber)
            .MustBeAlphanumericWithThai()
            .MaximumLength(50);

        this.RuleFor(r => r.SapBranchNumber)
            .MustBeAlphanumericWithThai()
            .MaximumLength(50);

        this.RuleFor(r => r.Email)
            .MustBeAlphanumericWithThai()
            .MaximumLength(100);
    }
}

public record CreateSuVendorResponse(Guid Id);

public class CreateSuVendor : SecureEndpointBase<CreateSuVendorCommand, Results<Ok<CreateSuVendorResponse>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateSuVendor(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateSuVendor> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Post("/st/st003");
    }

    protected override async ValueTask<Results<Ok<CreateSuVendorResponse>, Conflict<string>>>
        HandleRequestAsync(CreateSuVendorCommand req, CancellationToken ct)
    {
        var isDuplicated = await this.dbContext.SuVendors
                                     .AnyAsync(c => c.TaxpayerIdentificationNo == req.TaxpayerIdentificationNo && c.SapBranchNumber == req.SapBranchNumber, ct);

        if (isDuplicated)
        {
            return TypedResults.Conflict("เลขประจำตัวผู้เสียภาษี/รหัสสาขาซ้ำ");
        }

        var createModel = SuVendor.Create(
            req.Nationality,
            req.Type,
            new VendorProfile(
                ParameterCode.From(req.EntrepreneurType),
                req.TaxpayerIdentificationNo,
                req.EstablishmentName),
            req.PlaceName,
            req.Address,
            new ContactInfo(
                req.Tel,
                req.Fax,
                req.Email),
            new SapInfo(
                req.SapVendorNumber,
                req.SapBranchNumber));

        this.dbContext.SuVendors.Add(createModel);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        var response = new CreateSuVendorResponse(createModel.Id.Value);

        return TypedResults.Ok(response);
    }
}