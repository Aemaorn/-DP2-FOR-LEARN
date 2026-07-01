namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Abstact;
using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpsertEntrepreneurPriceDetailsEndpoint : PrincipleApprovalRentalEndpointBase<EntrepreneursPriceDetailRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpsertEntrepreneurPriceDetailsEndpoint(
        ILogger<UpsertEntrepreneurPriceDetailsEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/entrepreneurs/{principleApprovalRentalEntrepreneurId:guid}/price-details");
        this.Description(b => b
            .WithTags("Procurement/PrincipleApprovalRental")
            .WithName("CreatePrincipleApprovalRentalEntrepreneursPriceDetails")
            .Produces<Created<Guid>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(EntrepreneursPriceDetailRequest req, CancellationToken ct)
    {
        var approvalRental = await this.dbContext.PPrincipleApprovalRentals.Include(pPrincipleApprovalRental => pPrincipleApprovalRental.Entrepreneurs)
                                       .SingleOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (approvalRental is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลขออนุมัติเช่ารหัสที่ {req.PrincipleApprovalRentalId}");
        }

        var rentalEntrepreneurs = approvalRental.Entrepreneurs.FirstOrDefault(e => e.Id == PPrincipleApprovalRentalEntrepreneursId.From(req.PrincipleApprovalRentalEntrepreneurId));

        if (rentalEntrepreneurs is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการหัส {req.PrincipleApprovalRentalId}");
        }

        if (req.EntrepreneursPriceDetails != null)
        {
            this.UpsertEntrepreneurPriceDetail(rentalEntrepreneurs, req.EntrepreneursPriceDetails);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}