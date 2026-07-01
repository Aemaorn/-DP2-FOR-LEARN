namespace GHB.DP2.Application.Features.Procurement.Jp005;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Jp005.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using global::GHB.DP2.Application.Constants;
using global::GHB.DP2.Application.EventHandlers.SuNotifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RejectJp005Request
{
    public Guid ProcurementId { get; init; }

    public Guid Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class RejectJp005Endpoint : Jp005EndpointBase<RejectJp005Request, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectJp005Endpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<RejectJp005Endpoint> logger)
        : base(dbContext, operationService, commandTextService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/jp005/{Id:guid}/reject");
        this.Options(b =>
            b.WithTags("Procurement/JorPor005")
             .WithName("Reject")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        RejectJp005Request req,
        CancellationToken ct)
    {
        var (procurement, jp005Existing) = await this.ValidateRequestAsync(req, ct);

        this.AcceptorReject(jp005Existing, req);

        jp005Existing.SetRejected(req.Remark);

        var jp004 = await this.dbContext.PpPurchaseRequisitions
                              .Include(p => p.Assignees)
                              .FirstOrDefaultAsync(w => w.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        UserId[] assigneeUserIds = jp004?.LastedAssignee is { } lastedAssignee ? [lastedAssignee.UserId] : [];
        _ = SendNotificationAsync(jp005Existing, assigneeUserIds);

        jp005Existing.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Reject,
            "ตีกลับแก้ไข",
            jp005Existing.Status.ToString(),
            req.Remark));

        await this.UpdateDocumentRejected(jp005Existing, req.UserId, procurement, ct);
        this.dbContext.PJp005S.Update(jp005Existing);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<(Procurement, PJp005)> ValidateRequestAsync(
        RejectJp005Request req,
        CancellationToken ct)
    {
        var procurement = await this.GetProcurementById(
            ProcurementId.From(req.ProcurementId),
            ct);

        var jp005Existing = this.GetJp005ById(
            procurement.Jp005,
            PJp005Id.From(req.Id),
            ProcurementId.From(req.ProcurementId));

        var canReject =
            jp005Existing.Status is PJp005Status.WaitingApproval;

        if (!canReject)
        {
            this.ThrowError(
                r =>
                    req.Id,
                $"จพ.005 ที่ระบุไม่อยู่ในสถานะที่สามารถปฏิเสธได้ (สถานะปัจจุบัน: {jp005Existing.Status})",
                StatusCodes.Status404NotFound);
        }

        return (procurement, jp005Existing);
    }

    private void AcceptorReject(
        PJp005 jp005Existing,
        RejectJp005Request req)
    {
        var draftAcceptors =
            jp005Existing.Acceptors
                         .Where(a =>
                             a is
                             {
                                 Type: AcceptorType.Approver or AcceptorType.DepartmentDirectorAgree,
                                 Status: AcceptorStatus.Pending,
                                 IsActive: true
                             })
                         .Map(DelegatorExtensions.DelegatorToAcceptor)
                         .OrderBy(a => a.Sequence)
                         .ToList();

        var currentAcceptor =
            draftAcceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                ? a.UserId == req.UserId
                : a.Delegatee?.SuUserId == UserId.From(req.UserId)
                  && a.Status == AcceptorStatus.Pending);

        if (currentAcceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser =
            jp005Existing.Acceptors
                         .FirstOrDefault(a => a.Id == currentAcceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!currentAcceptorUser.ArePreviousAcceptorsApproved(jp005Existing.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Reject(remark: req.Remark);
    }

    private static async Task SendNotificationAsync(PJp005 jp05, IEnumerable<UserId> assigneeUserIds)
    {
        var recipients = new[] { UserId.From(jp05.AuditInfo.CreatedBy) }
            .Concat(assigneeUserIds)
            .Distinct();

        foreach (var userId in recipients)
        {
            await Notification
                  .Crate(
                      userId,
                      NotificationConstant.ReturnToCreator.Title,
                      string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.PreProcurementJorPor05.Name, jp05.PJp005Number),
                      NotificationProgram.Procurement)
                  .SetReferenceId(jp05.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.PreProcurementAppointment.Url, jp05.Procurement.Id), "ดูรายละเอียด")
                  .PublishAsync(CancellationToken.None);
        }
    }

    private async Task UpdateDocumentRejected(
        PJp005 jp005,
        Guid userId,
        Domain.Procurement.Procurement procurement,
        CancellationToken ct)
    {
        if (procurement.SupplyMethodCode == SupplyMethodConstant.Sixty && procurement.Budget > 100000)
        {
            return;
        }

        var documentService =
            this.Resolve<IDocumentService>();

        var approvalDoc = jp005.LastedWaitingApprovalIsReplaceApprovalDocument;

        if (approvalDoc is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารร่าง จพ.005 ที่ต้องการอัปโหลด",
                StatusCodes.Status404NotFound);
        }

        var approvalFileId = await ReplaceDocument(approvalDoc.FileId, PJp005DocumentType.Approval);

        jp005.AddDocumentHistory(
            PJp005DocumentType.Approval,
            approvalFileId,
            true);

        if (jp005.Procurement.SupplyMethodCode == SupplyMethodConstant.Eighty)
        {
            var lastedDraftCommandDocument = jp005.LastedWaitingApprovalCommandDocument;

            if (lastedDraftCommandDocument is null)
            {
                this.ThrowError(
                    $"ไม่พบเอกสารร่าง จพ.005 ที่ต้องการอัปโหลด",
                    StatusCodes.Status404NotFound);
            }

            var commandFileId = await ReplaceDocument(lastedDraftCommandDocument.FileId, PJp005DocumentType.Command);

            jp005.AddDocumentHistory(
                PJp005DocumentType.Command,
                commandFileId,
                true);
        }

        return;

        async Task<FileId> ReplaceDocument(FileId fileId, PJp005DocumentType documentType)
        {
            var replaceDto =
                await this.GetJp005MapToResponseMappingDtoAsync(jp005, procurement, userId, false, false, false, cancellationToken: ct);

            var parentDirectory =
                $"{DocumentTemplateGroups.PlanAnnouncement}/{jp005.Id}_{documentType.ToString()}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var copyFileId =
                await documentService.CopyDocumentTemplateAsync(
                    fileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: parentDirectory,
                    cancellationToken: ct);

            if (copyFileId is null)
            {
                this.ThrowError(
                    DocumentErrorMessages.CopyDocumentFailed,
                    StatusCodes.Status500InternalServerError);
            }

            return copyFileId.Value;
        }
    }
}