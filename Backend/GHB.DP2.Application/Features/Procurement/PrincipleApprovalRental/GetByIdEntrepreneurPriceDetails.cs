namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetByIdPrincipleApprovalRentalEntrepreneursPriceDetailsRequest(Guid ProcurementId, Guid PrincipleApprovalRentalId, Guid Id);

public class GetByIdEntrepreneurPriceDetailsEndpoint : EndpointBase<GetByIdPrincipleApprovalRentalEntrepreneursPriceDetailsRequest, Results<Ok<EntrepreneursPriceDetailResponseDto>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetByIdEntrepreneurPriceDetailsEndpoint(ILogger<GetByIdPrincipleApprovalRentalEntrepreneurs> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/entrepreneurs/{id:guid}/price-details");
        this.Description(b => b
            .WithTags("Procurement/PrincipleApprovalRental")
            .WithName("GetByIdPrincipleApprovalRentalEntrepreneursPriceDetails")
            .AllowAnonymous()
            .Produces<Ok<Guid>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<EntrepreneursPriceDetailResponseDto>, NotFound<string>>> HandleRequestAsync(GetByIdPrincipleApprovalRentalEntrepreneursPriceDetailsRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovalRentalEntrepreneurs
                               .Include(e => e.EntrepreneursPriceDetails)
                               .Include(pPrincipleApprovalRentalEntrepreneurs => pPrincipleApprovalRentalEntrepreneurs.Vendor)
                               .ThenInclude(suVendor => suVendor.EntrepreneurTypeInfo)
                               .Include(pPrincipleApprovalRentalEntrepreneurs => pPrincipleApprovalRentalEntrepreneurs.EntrepreneursShareholders)
                               .AsSplitQuery()
                               .SingleOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalEntrepreneursId.From(req.Id) && x.PPrincipleApprovalRental.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId) && x.PPrincipleApprovalRental.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการที่มีรหัส {req.Id}");
        }

        var response = new EntrepreneursPriceDetailResponseDto(
            entity.Id.Value,
            entity.Vendor.Nationality,
            entity.Vendor.Type,
            entity.Vendor.TaxpayerIdentificationNo,
            entity.Vendor.EntrepreneurType.Value,
            entity.Vendor.EntrepreneurTypeInfo.Label,
            entity.Vendor.EstablishmentName,
            entity.Vendor.Tel ?? string.Empty,
            entity.Vendor.Email,
            entity.EntrepreneursPriceDetails
                  .Select(s => new EntrepreneursPriceDetailDto(
                      s.Id.Value,
                      s.Sequence,
                      s.ParcelName,
                      s.ParcelQuantity,
                      s.ParcelUnitCode.Value,
                      s.VatTypeCode.Value,
                      s.OfferedPrice,
                      s.AgreedPrice,
                      s.Description)).OrderBy(x => x.Sequence));

        return TypedResults.Ok(response);
    }
}

public record EntrepreneursPriceDetailResponseDto(
    Guid Id,
    SuVendorNationality? Nationality,
    SuVendorType? Type,
    string EntrepreneurTaxId,
    string EntrepreneurType,
    string EntrepreneurTypeLabel,
    string EntrepreneurName,
    string EntrepreneurTel,
    string EntrepreneurEmail,
    IEnumerable<EntrepreneursPriceDetailDto> EntrepreneursPriceDetails
);

public record EntrepreneursPriceDetailDto(
    Guid Id,
    int Sequence,
    string ParcelName,
    int ParcelQuantity,
    string ParcelUnitCode,
    string VatTypeCode,
    decimal OfferedPrice,
    decimal AgreedPrice,
    string Description
);