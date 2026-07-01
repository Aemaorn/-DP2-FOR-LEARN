namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval;

using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Abstract;
using GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

public class UpdatePurchaseOrderApprovalEndpoint : PurchaseOrderApprovalEndpointBase<UpdatePurchaseOrderApprovalRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePurchaseOrderApprovalEndpoint(
        ILogger<UpdatePurchaseOrderApprovalEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/purchase-order-approval/{id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/PurchaseOrderApproval")
                              .WithName("UpdatePurchaseOrderApproval")
                              .Produces<Ok<Guid>>(StatusCodes.Status200OK)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdatePurchaseOrderApprovalRequest req, CancellationToken ct)
    {
        var procurement = await this.ValidateRequestAsync(req, ct);

        var entity = await this.dbContext.PPurchaseOrderApprovals
                               .Include(x => x.Acceptors)
                               .Include(x => x.Assignees)
                               .Include(x => x.Contracts)
                               .Include(x => x.PurchaseOrderApprovalBudget)
                               .Include(x => x.PurchaseOrderApprovalEntrepreneurs)
                               .Include(pPurchaseOrderApproval => pPurchaseOrderApproval.Procurement)
                               .SingleOrDefaultAsync(x => x.Id == PurchaseOrderApprovalId.From(req.Id) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลอนุมัติใบสั่งซื้อที่มีรหัส {req.Id}");
        }

        entity.SetContractType(req.ContractType);

        // Upsert acceptors
        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(entity, [.. req.Acceptors], procurement.DepartmentId, UserId.From(req.UserId));
        }

        // Upsert assignees
        if (req.Assignees is not null)
        {
            var newAssignees = req.Assignees.Where(x => x is { AssigneeType: AssigneeType.Assignee, Id: null });

            var programName = entity.Procurement.Type == ProcurementType.Rent
                ? ProgramConstant.BranchSpaceRent.Name
                : ProgramConstant.ProcurementPurchaseOrderApproval.Name;

            foreach (var inComing in newAssignees)
            {
                await SendNotificationAsync(
                    entity,
                    UserId.From(inComing.UserId),
                    NotificationConstant.Assignment.Title,
                    string.Format(NotificationConstant.Assignment.Message, programName, entity.Procurement.ProcurementNumber));
            }

            await this.UpsertAssignee(entity, req.Assignees, ct, UserId.From(req.UserId));
        }

        // Upsert committees
        if (req.Committees?.Count() > 0)
        {
            await this.UpdateCommittees(entity, req.Committees, ct);
        }

        // Upsert contracts
        if (req.Contracts is not null)
        {
            this.UpsertContract(procurement, entity, req.Contracts);
        }

        if (req.Status == PurchaseOrderApprovalStatus.WaitingApproval
         && entity.DocumentDate is null)
        {
            entity.SetDocumentDate();
        }

        // Update status
        if (entity.Status == req.Status)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูล",
                entity.Status.ToString()));
        }
        else
        {
            switch (req.Status)
            {
                case PurchaseOrderApprovalStatus.WaitingApproval:
                    entity.SetWaitingApproval();
                    var approvers = entity.Acceptors
                                          .Where(p => p.Type == AcceptorType.Approver)
                                          .OrderBy(a => a.Sequence)
                                          .ToList();

                    var firstPending = approvers.Select(DelegatorExtensions.DelegatorToAcceptor).FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

                    if (firstPending != null)
                    {
                        var programName = entity.Procurement.Type == ProcurementType.Rent
                            ? ProgramConstant.BranchSpaceRent.Name
                            : ProgramConstant.ProcurementPurchaseOrderApproval.Name;

                        foreach (var targetUserId in firstPending.GetNotificationTargets())
                        {
                            _ = SendNotificationAsync(
                                entity,
                                targetUserId,
                                NotificationConstant.WaitForLike.Title,
                                string.Format(NotificationConstant.WaitForLike.Message, programName, entity.Procurement.ProcurementNumber));
                        }
                    }

                    break;

                case PurchaseOrderApprovalStatus.Edit:
                    entity.SetEdit();

                    break;

                case PurchaseOrderApprovalStatus.Assigned:
                    entity.SetAssigned();
                    _ = SendNotificationAssigneeAsync(entity, CancellationToken.None);

                    break;

                default:
                    break;
            }
        }

        this.dbContext.PPurchaseOrderApprovals.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<Procurement> ValidateRequestAsync(UpdatePurchaseOrderApprovalRequest req, CancellationToken ct)
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

    private static async Task SendNotificationAsync(PPurchaseOrderApproval entity, UserId userId, string title, string message)
    {
        var notificationProgram = entity.Procurement.Type == ProcurementType.Rent
            ? NotificationProgram.BranchSpaceRent
            : NotificationProgram.Procurement;

        var programUrl = entity.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  notificationProgram)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(programUrl, entity.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(PPurchaseOrderApproval entity, CancellationToken ct)
    {
        var programName = entity.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Name
            : ProgramConstant.ProcurementPurchaseOrderApproval.Name;

        var notificationProgram = NotificationProgram.Procurement;

        var programUrl = entity.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.Procurement.Url;

        foreach (var targetUserId in entity.Assignees.Where(w => w.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, programName, entity.Procurement.ProcurementNumber),
                      notificationProgram)
                  .SetReferenceId(entity.Id.Value)
                  .SetLinkUrl(
                      string.Format(programUrl, entity.Procurement.Id),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}

public record UpdatePurchaseOrderApprovalRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid Id,
    string ContractType,
    PurchaseOrderApprovalStatus Status,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    IEnumerable<PurchaseOrderApprovalContractDto>? Contracts,
    IEnumerable<PurchaseOrderApprovalCommittee>? Committees
);

public class UpdatePurchaseOrderApprovalRequestValidator : Validator<UpdatePurchaseOrderApprovalRequest>
{
    public UpdatePurchaseOrderApprovalRequestValidator()
    {
        this.RuleFor(x => x)
            .Must(x =>
                x.Status != PurchaseOrderApprovalStatus.Draft ||
                x.Status != PurchaseOrderApprovalStatus.Rejected ||
                (x.Acceptors != null && x.Acceptors.Any()))
            .WithMessage("ต้องระบุผู้มีอำนาจอนุมัติอย่างน้อย 1 คน")
            .Must(x =>
                x.Status == PurchaseOrderApprovalStatus.Draft ||
                x.Status == PurchaseOrderApprovalStatus.Rejected ||
                (x.Acceptors != null && x.Acceptors.Any()))
            .WithMessage("ต้องมอบหมายผู้รับผิดชอบสัญญาอย่างน้อย 1 คน")
            .Must(x =>
                x.Status == PurchaseOrderApprovalStatus.Draft ||
                x.Status == PurchaseOrderApprovalStatus.Rejected ||
                (x.Contracts != null && x.Contracts.Any()))
            .WithMessage("ต้องกำหนดข้อมูลผู้ชนะการเสนอราคาอย่างน้อย 1 รายการ");
    }
}