namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateSuVendorRequest
{
    public Guid Id { get; init; }

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

public class UpdateSuVendor : SecureEndpointBase<UpdateSuVendorRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateSuVendor(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateSuVendor> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Put("/st/st003/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateSuVendorRequest req, CancellationToken ct)
    {
        var updateModel = await this.dbContext.SuVendors
                                    .FirstOrDefaultAsync(x => x.Id == SuVendorId.From(req.Id), ct);

        if (updateModel is null)
        {
            return TypedResults.NotFound($"SuVendor with Id {req.Id} not found");
        }

        updateModel.Update(
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

        this.dbContext.SuVendors
            .Update(updateModel);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok();
    }
}