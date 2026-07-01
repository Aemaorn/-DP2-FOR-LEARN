namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using FluentValidation;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAcceptorDutiesStatusRequest(
    Guid ProcurementId,
    Guid Id,
    Guid AcceptorId,
    bool IsUnableToPerformDuties,
    string? Remark);

public class UpdateAcceptorDutiesStatusRequestValidator : Validator<UpdateAcceptorDutiesStatusRequest>
{
    public UpdateAcceptorDutiesStatusRequestValidator()
    {
        this.RuleFor(x => x.ProcurementId)
            .NotNull()
            .NotEmpty()
            .WithMessage("ไม่พบข้อมูลจัดซื้อจัดจ้าง");

        this.RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage("ไม่พบข้อมูลขออนุมัติเช่า");

        this.RuleFor(x => x.AcceptorId)
            .NotNull()
            .NotEmpty()
            .WithMessage("ไม่พบคณะกรรมการ");

        this.RuleFor(x => x.IsUnableToPerformDuties)
            .NotNull()
            .WithMessage("กรุณาส่งสถานะการปฎิบัติงาน");
    }
}

public class UpdateAcceptorDutiesStatusEndpoint : EndpointBase<UpdateAcceptorDutiesStatusRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateAcceptorDutiesStatusEndpoint(
        ILogger<UpdateAcceptorDutiesStatusRequest> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{procurementId:guid}/principle-approval-rental/{id:guid}/acceptor/{AcceptorId:guid}/duties-status");
        this.Description(b => b
                              .WithTags("Procurement/PrincipleApprovalRental")
                              .WithName("UpdatePrincipleApprovalRentalDutiesStatus")
                              .Produces<Ok>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAcceptorDutiesStatusRequest req, CancellationToken ct)
    {
        var principleApprovalRental = await this.dbContext.PPrincipleApprovalRentals
                                                .Include(c => c.Acceptors)
                                                .SingleOrDefaultAsync(
                                                    w =>
                                                        w.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                                        w.Id == PPrincipleApprovalRentalId.From(req.Id),
                                                    ct);

        if (principleApprovalRental is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลขออนุมัติเช่า");
        }

        var acceptor = principleApprovalRental.Acceptors.FirstOrDefault(f => f.Id == AcceptorId.From(req.AcceptorId));

        if (acceptor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผคณะกรรมการจัดเช่า");
        }

        acceptor.SetIsUnableToPerformDuties(req.IsUnableToPerformDuties);

        if (req.IsUnableToPerformDuties)
        {
            acceptor.UnableToPerformDuties(req.Remark);
        }
        else
        {
            acceptor.Pending();
        }

        if (principleApprovalRental.HasMajorityRejection())
        {
            principleApprovalRental.SetRejected();
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}