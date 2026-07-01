namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Abstract;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreatePurchaseOrderApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    string ContractType,
    PurchaseOrderApprovalStatus Status,
    IEnumerable<PurchaseOrderApprovalContractDto>? Contracts,
    IEnumerable<AcceptorRequest> Acceptors,
    IEnumerable<AssigneeRequest> Assignees);

public class Validator : FastEndpoints.Validator<CreatePurchaseOrderApprovalRequest>
{
    public Validator()
    {
        this.RuleFor(x => x.ProcurementId)
            .NotEmpty()
            .WithMessage("กรุณาระบุรหัสโครงการ");

        // TODO: Must be valid contract type from Business Logic.
        this.RuleFor(x => x.ContractType)
            .NotEmpty()
            .WithMessage("กรุณาระบุประเภทสัญญา");

        this.RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("สถานะไม่ถูกต้อง");

        this.RuleFor(r => r.Assignees)
            .Must(x => x.Any())
            .When(w => w.Status is PurchaseOrderApprovalStatus.WaitingApproval && w.ContractType == "CType001")
            .WithMessage("กรุณามอบหมายผู้รับผิดชอบสัญญา");

        this.RuleFor(r => r.Acceptors)
            .Must(x => x.Any())
            .When(w => w.Status is PurchaseOrderApprovalStatus.WaitingApproval)
            .WithMessage("กรุณาเพิ่มผู้มีอำนาจเห็นชอบ/อนุมัติ");

        this.RuleFor(r => r.Acceptors)
            .Must(x => x.Count(a => a.AcceptorType == AcceptorType.Approver) >= 1)
            .When(w => w.Status is PurchaseOrderApprovalStatus.WaitingApproval)
            .WithMessage("ต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติ อย่างน้อย 1 คน");

        this.RuleFor(r => r.Assignees)
            .Must(x => x.Count(a => a.AssigneeType == AssigneeType.Assignee) >= 1)
            .When(w => w.Status is PurchaseOrderApprovalStatus.WaitingApproval && w.ContractType == "CType001")
            .WithMessage("ต้องมีผู้รับผิดชอบสัญญา อย่างน้อย 1 คน");
    }
}

public class CreatePurchaseOrderApprovalEndpoint : PurchaseOrderApprovalEndpointBase<CreatePurchaseOrderApprovalRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePurchaseOrderApprovalEndpoint(ILogger<CreatePurchaseOrderApprovalEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/purchase-order-approval");
        this.Description(b => b
            .WithTags("Procurement/PurchaseOrderApproval")
            .WithName("CreatePurchaseOrderApproval")
            .Produces<Created<Guid>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreatePurchaseOrderApprovalRequest req, CancellationToken ct)
    {
        var procurement = await this.ValidateRequestAsync(req, ct);

        var entity = PPurchaseOrderApproval.Create(
            ProcurementId.From(req.ProcurementId),
            req.ContractType,
            req.Status);

        if (req.Acceptors.Any())
        {
            await this.UpsertAcceptors(entity, [.. req.Acceptors], procurement.DepartmentId, UserId.From(req.UserId));
        }

        if (req.Assignees.Any())
        {
            await this.UpsertAssignee(entity, req.Assignees, ct, UserId.From(req.UserId));
        }

        if (req.Contracts is not null)
        {
            this.UpsertContract(procurement, entity, req.Contracts);
        }

        _ = req.Status switch
        {
            PurchaseOrderApprovalStatus.WaitingApproval => entity.SetWaitingApproval(),
            _ => entity,
        };

        procurement.SetProcessType(ProcessType.PurchaseOrderApproval);
        procurement.SetProcurementStep(procurement.Type, ProcurementStep.Procurement);

        this.dbContext.PPurchaseOrderApprovals.Add(entity);

        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }

    private async Task<Procurement> ValidateRequestAsync(CreatePurchaseOrderApprovalRequest req, CancellationToken ct)
    {
        var procurement = await
            this.dbContext.Procurements
                .SingleOrDefaultAsync(
                    p => p.Id == ProcurementId.From(req.ProcurementId),
                    ct);

        if (procurement is null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลโครงการที่มีรหัส {req.ProcurementId}",
                StatusCodes.Status404NotFound);
        }

        return procurement;
    }
}