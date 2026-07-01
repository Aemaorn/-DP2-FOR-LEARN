namespace GHB.DP2.Application.Features.Procurement.Jp006;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record SendApproveJp006Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    PurchaseOrderId Jp006Id,
    ProcurementId ProcurementId,
    IEnumerable<UpdateJp006Entrepreneur> Entrepreneurs,
    IEnumerable<Jp006AcceptorInfo>? Acceptors,
    IEnumerable<Jp006AssigneeInfo>? Assignees);

public class SendApproveJp006Endpoint : Jp006EndpointBase<SendApproveJp006Request, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public SendApproveJp006Endpoint(
        ILogger<SendApproveJp006Endpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp006/{Jp006Id:guid}/send-approve");
        this.Description(b => b
                              .WithTags(nameof(Jp006))
                              .WithName("SendApproveJp006")
                              .Produces<Ok>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(SendApproveJp006Request req, CancellationToken ct)
    {
        // 1. Validate procurement exists
        var procurementExisting = await this.ValidateProcurementAsync(req.ProcurementId.Value, ct);

        // 2. Load jp006 with all includes
        var jp006 = await this.GetByIdAsync(req.ProcurementId, req.Jp006Id, ct);

        if (jp006 is null)
        {
            return TypedResults.NotFound($"ไม่พบรายการจัดซื้อจัดจ้างที่มีรหัส {req.Jp006Id}");
        }

        // 3. Validate status is WaitingComment
        if (jp006.Status != PurchaseOrderStatus.WaitingComment)
        {
            return TypedResults.BadRequest($"สถานะปัจจุบันไม่ใช่ WaitingComment ไม่สามารถส่งอนุมัติได้");
        }

        // 4. Update form data
        await UpdateJp006Endpoint.UpsertEntrepreneursAsync(jp006, req.Entrepreneurs);

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(jp006, req.Acceptors, procurementExisting.DepartmentId, UserId.From(req.UserId));
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(jp006, req.Assignees, CancellationToken.None, UserId.From(req.UserId));
        }

        // 5. Set status: WaitingComment → WaitingApproval
        jp006.SetWaitingAcceptor();

        jp006.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            $"ส่งอนุมัติสั่งซื้อ/สั่งจ้าง(จพ.006)",
            jp006.Status.ToString()));

        // 6. Set initial approver currents and send notifications
        var approvers = jp006.Acceptors
                             .Where(p => p.Type == AcceptorType.Approver)
                             .OrderBy(a => a.Sequence)
                             .ToList();

        var firstPending = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                                    .FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

        if (firstPending != null)
        {
            foreach (var targetUserId in firstPending.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    jp006,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.PreProcurementJorPor06.Name, jp006.PurchaseOrderNumber));
            }
        }

        await this.StampCheckPointLastedDocument(jp006.Procurement, jp006, PurchaseOrderDocumentType.Jp006, ct);
        await this.StampCheckPointLastedDocument(jp006.Procurement, jp006, PurchaseOrderDocumentType.Winner, ct);

        // 12. Final save
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(PPurchaseOrder purchaseOrder, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(purchaseOrder.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, purchaseOrder.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}