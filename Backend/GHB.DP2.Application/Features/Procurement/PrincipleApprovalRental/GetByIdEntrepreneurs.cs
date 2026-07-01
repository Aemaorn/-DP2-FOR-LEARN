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

public record GetByIdPrincipleApprovalRentalEntrepreneursRequest(Guid ProcurementId, Guid PrincipleApprovalRentalId, Guid Id);

public class GetByIdPrincipleApprovalRentalEntrepreneurs : EndpointBase<GetByIdPrincipleApprovalRentalEntrepreneursRequest, Results<Ok<EntrepreneursResponseDto>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetByIdPrincipleApprovalRentalEntrepreneurs(ILogger<GetByIdPrincipleApprovalRentalEntrepreneurs> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/entrepreneurs/{id:guid}");
        this.Description(b => b
            .WithTags("Procurement/PrincipleApprovalRental")
            .WithName("GetByIdPrincipleApprovalRentalEntrepreneurs")
            .AllowAnonymous()
            .Produces<Ok<Guid>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<EntrepreneursResponseDto>, NotFound<string>>> HandleRequestAsync(GetByIdPrincipleApprovalRentalEntrepreneursRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovalRentalEntrepreneurs
                               .Include(e => e.EntrepreneursShareholders)
                               .Include(pPrincipleApprovalRentalEntrepreneurs => pPrincipleApprovalRentalEntrepreneurs.Vendor)
                               .ThenInclude(suVendor => suVendor.EntrepreneurTypeInfo)
                               .SingleOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalEntrepreneursId.From(req.Id) && x.PPrincipleApprovalRental.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId) && x.PPrincipleApprovalRental.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการที่มีรหัส {req.Id}");
        }

        var response = new EntrepreneursResponseDto(
            entity.Id.Value,
            entity.Vendor.Id.Value,
            entity.Sequence,
            entity.Vendor.TaxpayerIdentificationNo,
            entity.Vendor.EntrepreneurType.Value,
            entity.Vendor.EntrepreneurTypeInfo.Label,
            entity.Vendor.EstablishmentName,
            entity.Vendor.Email,
            entity.WatchlistResult,
            entity.WatchlistResultRemark,
            entity.WatchlistResultAt,
            entity.CoiResult,
            entity.CoiResultRemark,
            entity.CoiResultAt,
            entity.EgpResult,
            entity.EgpResultRemark,
            entity.EgpResultAt,
            entity.EmailSend,
            entity.Vendor.Nationality,
            entity.Vendor.Type,
            entity.Vendor.Tel,
            entity.EntrepreneursShareholders
                  .Select(s => new ShareholderDto(
                      s.Id.Value,
                      s.Sequence,
                      s.TaxId,
                      s.FirstName,
                      s.LastName,
                      (bool)s.IsDirector,
                      s.IsShareholder,
                      s.CheckType,
                      s.WatchlistResult,
                      s.WatchlistResultRemark,
                      s.WatchlistResultAt,
                      s.CoiResult,
                      s.CoiResultRemark,
                      s.CoiResultAt,
                      s.EgpResult,
                      s.EgpRemark,
                      s.EgpResultAt)));

        return TypedResults.Ok(response);
    }
}

public record EntrepreneursResponseDto(
    Guid Id,
    Guid VendorId,
    int Sequence,
    string EntrepreneurTaxId,
    string EntrepreneurType,
    string EntrepreneurTypeLabel,
    string EntrepreneurName,
    string EntrepreneurEmail,
    bool WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool EgpResult,
    string? EgpResultRemark,
    DateTimeOffset? EgpResultAt,
    bool EmailSend,
    SuVendorNationality? Nationality,
    SuVendorType? Type,
    string? Tel,
    IEnumerable<ShareholderDto> Shareholders
);

public record ShareholderDto(
    Guid Id,
    int Sequence,
    string TaxId,
    string FirstName,
    string LastName,
    bool IsDirector,
    bool? IsShareholder,
    string? CheckType,
    bool WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpResultAt
);