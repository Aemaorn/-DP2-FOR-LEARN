namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateWaiveOrReducePenaltyRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid CamContractAmendmentId,
    Guid WaiveOrReducePenaltyId,
    bool WaiveAll,
    Guid? ContractAddendumDocumentId,
    bool? IsContractAddendumDocumentIdReplaced,
    Guid? ContractAmendmentRequestDocumentId,
    bool? IsContractAmendmentRequestDocumentIdReplaced,
    PenaltyInfo? PenaltyOld,
    PenaltyInfo? PenaltyNew,
    CamContractAmendmentWaiveOrReducePenaltyStatus Status,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees);

public record UpdateWaiveOrReducePenaltyResponse(Guid? NewDocumentFileId);

public class UpdateWaiveOrReducePenaltyEndpoint : WaiveOrReducePenaltyEndpointBase<UpdateWaiveOrReducePenaltyRequest, Results<Ok<UpdateWaiveOrReducePenaltyResponse>, NotFound<string>, BadRequest<string>>>
{
    public UpdateWaiveOrReducePenaltyEndpoint(ILogger<UpdateWaiveOrReducePenaltyEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/waive-or-reduce-penalty/{WaiveOrReducePenaltyId:guid}");
        this.Description(b =>
            b.WithTags("ContractAmendment/WaiveOrReducePenalty")
             .WithName("UpdateWaiveOrReducePenalty")
             .Produces<Ok>()
             .Produces<NotFound<string>>(StatusCodes.Status404NotFound)
             .Produces<BadRequest<string>>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<UpdateWaiveOrReducePenaltyResponse>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        var entity = await this.GetEntityAsync(req.WaiveOrReducePenaltyId, ct);
        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลบันทึกต่อท้ายสัญญาที่ระบุ");
        }

        if (IsEntityApproved(entity))
        {
            return TypedResults.BadRequest("ไม่สามารถแก้ไขรายการนี้ได้ เนื่องจากสถานะเป็น อนุมัติแล้ว");
        }

        await this.UpdateEntityDataAsync(entity, req, ct);
        FileId? newDocumentFileId = await this.HandleDocumentHistoryAsync(entity, req, ct);
        await this.HandleStatusChangeAsync(entity, req, ct);

        this.DbContext.CamContractAmendmentWaiveOrReducePenalties.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateWaiveOrReducePenaltyResponse(newDocumentFileId?.Value));
    }

    private async Task<CamContractAmendmentWaiveOrReducePenalty?> GetEntityAsync(Guid waiveOrReducePenaltyId, CancellationToken ct)
    {
        var id = WaiveOrReducePenaltyId.From(waiveOrReducePenaltyId);
        return await this.DbContext.CamContractAmendmentWaiveOrReducePenalties
                         .Include(e => e.CamContractAmendment)
                         .Include(e => e.Acceptors)
                         .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    private static bool IsEntityApproved(CamContractAmendmentWaiveOrReducePenalty entity)
    {
        return entity.Status == CamContractAmendmentWaiveOrReducePenaltyStatus.Approved;
    }

    private async Task UpdateEntityDataAsync(CamContractAmendmentWaiveOrReducePenalty entity, UpdateWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        entity.SetWaiveAll(req.WaiveAll);

        if (req.PenaltyNew is not null)
        {
            UpdatePenaltyNew(entity, req.PenaltyNew);
        }

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(entity, [.. req.Acceptors], entity.Status, ct);
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(entity, [.. req.Assignees], ct);
        }

        entity.SetStatus(req.Status);
        MapActivity(entity, req.Status);
    }

    private static void UpdatePenaltyNew(CamContractAmendmentWaiveOrReducePenalty entity, PenaltyInfo penaltyNew)
    {
        entity.SetPenaltyNew(
            penaltyNew?.PenaltyTypeCode is not null ? ParameterCode.From(penaltyNew.PenaltyTypeCode) : null,
            penaltyNew?.Rate,
            penaltyNew?.Amount,
            penaltyNew?.RateTypeCode is not null ? ParameterCode.From(penaltyNew.RateTypeCode) : null);
    }

    private async Task<FileId?> HandleDocumentHistoryAsync(CamContractAmendmentWaiveOrReducePenalty entity, UpdateWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        FileId? newDocumentFileId = null;

        var mustSaveAddendumDocument =
            ShouldUpdateContractAddendum(req) &&
            req.Status != CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval;

        if (mustSaveAddendumDocument)
        {
            newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                WaiveOrReducePenaltyDocumentType.WaiveOrReducePenalty,
                FileId.From(req.ContractAddendumDocumentId!.Value),
                req.IsContractAddendumDocumentIdReplaced,
                ct);
        }

        if (ShouldUpdateContractAmendmentRequest(req))
        {
            entity.AddDocumentHistory(
                WaiveOrReducePenaltyDocumentType.Approved,
                FileId.From(req.ContractAmendmentRequestDocumentId!.Value),
                req.IsContractAmendmentRequestDocumentIdReplaced!.Value);
        }

        return newDocumentFileId;
    }

    private static bool ShouldUpdateContractAddendum(UpdateWaiveOrReducePenaltyRequest req)
    {
        return req is { ContractAddendumDocumentId: not null, IsContractAddendumDocumentIdReplaced: not null } &&
               req.IsContractAddendumDocumentIdReplaced.Value;
    }

    private static bool ShouldUpdateContractAmendmentRequest(UpdateWaiveOrReducePenaltyRequest req)
    {
        return req is { ContractAmendmentRequestDocumentId: not null, IsContractAmendmentRequestDocumentIdReplaced: not null } &&
               req.IsContractAmendmentRequestDocumentIdReplaced.Value;
    }

    private async Task HandleStatusChangeAsync(CamContractAmendmentWaiveOrReducePenalty entity, UpdateWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        switch (req.Status)
        {
            case CamContractAmendmentWaiveOrReducePenaltyStatus.Edit:
                entity.SetEdit();
                break;

            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval:
                await this.HandleWaitingCommitteeApprovalAsync(entity, req, ct);
                break;

            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingComment:
                await HandleWaitingCommentAsync(entity);
                break;

            case CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingApproval:
                await this.HandleWaitingApprovalAsync(entity);
                break;
        }
    }

    private async Task HandleWaitingCommitteeApprovalAsync(CamContractAmendmentWaiveOrReducePenalty entity, UpdateWaiveOrReducePenaltyRequest req, CancellationToken ct)
    {
        entity.SetWaitingCommitteeApproval();
        await this.UpdateDocumentAsync(entity, UserId.From(req.UserId), isReplace: true, hasCreator: true, hasAcceptor: false, ct: ct);
        EnsureInitialCommitteeCurrents(entity);
    }

    private static void EnsureInitialCommitteeCurrents(CamContractAmendmentWaiveOrReducePenalty entity)
    {
        var committee = entity.Acceptors?
                              .Where(a => a.Type == AcceptorType.TorDraftCommittee && a.IsActive && !a.IsUnableToPerformDuties && a.Status == AcceptorStatus.Pending)
                              .ToList();

        if (committee == null || committee.Count == 0)
        {
            return;
        }

        if (entity.Acceptors!.Any(a => a.Type == AcceptorType.TorDraftCommittee && a.Status == AcceptorStatus.Approved))
        {
            return;
        }

        var chairman = committee.FirstOrDefault(a => a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
                       ?? committee.FirstOrDefault(a => a.IsBoardChairman());

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var nonChair = committee.Where(a => chairman == null || a.Id != chairman.Id).ToList();

        if (nonChair.Count == 0 && chairman != null)
        {
            chairman.SetCurrent(true);

            _ = SendNotificationAsync(
                entity,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAmendment.Name, string.Empty));

            return;
        }

        foreach (var a in nonChair)
        {
            _ = SendNotificationAsync(
                entity,
                a.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAmendment.Name, string.Empty));
            a.SetCurrent(true);
        }

        if (chairman != null)
        {
            chairman.SetCurrent(false);
        }
    }

    private static Task HandleWaitingCommentAsync(CamContractAmendmentWaiveOrReducePenalty entity)
    {
        entity.SetWaitingComment();
        _ = SendNotificationAssigneeAsync(entity, CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task HandleWaitingApprovalAsync(CamContractAmendmentWaiveOrReducePenalty entity)
    {
        entity.SetWaitingApproval();

        var firstPendingApprover = GetFirstPendingApprover(entity);
        if (firstPendingApprover != null)
        {
            await SendApprovalNotificationAsync(entity, firstPendingApprover);
        }
    }

    private static CamContractAmendmentWaiveOrReducePenaltyAcceptor? GetFirstPendingApprover(CamContractAmendmentWaiveOrReducePenalty entity)
    {
        return entity.Acceptors
                     .Where(p => p.Type == AcceptorType.Approver)
                     .OrderBy(a => a.Sequence)
                     .FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);
    }

    private static Task SendApprovalNotificationAsync(CamContractAmendmentWaiveOrReducePenalty entity, CamContractAmendmentWaiveOrReducePenaltyAcceptor approver)
    {
        foreach (var targetUserId in approver.GetNotificationTargets())
        {
            _ = SendNotificationAsync(
                entity,
                targetUserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(
                    NotificationConstant.WaitForLike.Message,
                    ProgramConstant.PreProcurementTorDraft.Name,
                    entity.CamContractAmendment.CamContractAmendmentNumber));
        }

        return Task.CompletedTask;
    }

    private static void MapActivity(CamContractAmendmentWaiveOrReducePenalty entity, CamContractAmendmentWaiveOrReducePenaltyStatus reqStatus)
    {
        if (reqStatus != entity.Status)
        {
            _ = reqStatus switch
            {
                CamContractAmendmentWaiveOrReducePenaltyStatus.Edit => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        "เรียกคืนแก้ไข",
                        nameof(CamContractAmendmentWaiveOrReducePenaltyStatus.Edit))),
                CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        "ส่งเห็นชอบ",
                        nameof(CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingCommitteeApproval))),
                CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingAssigned => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.WaitingComment,
                        "ยืนยันมอบหมาย",
                        nameof(CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingAssigned))),
                CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingApproval => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendApprove,
                        "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                        nameof(CamContractAmendmentWaiveOrReducePenaltyStatus.WaitingApproval))),
                _ => unit,
            };

            return;
        }

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                "แก้ไขข้อมูล",
                entity.Status.ToString()));
    }

    private static async Task SendNotificationAsync(CamContractAmendmentWaiveOrReducePenalty entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractAmendment)
              .SetReferenceId(entity.CamContractAmendmentId.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractAmendment.Url, entity.CamContractAmendmentId), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static async Task SendNotificationAssigneeAsync(CamContractAmendmentWaiveOrReducePenalty entity, CancellationToken ct)
    {
        foreach (var targetUserId in entity.Assignees.Where(x => x.Type != AssigneeType.Director).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.Assignment.Title,
                      string.Format(NotificationConstant.Assignment.Message, ProgramConstant.ContractAmendment.Name, entity.CamContractAmendment.CamContractAmendmentNumber),
                      NotificationProgram.ContractAmendment)
                  .SetReferenceId(entity.CamContractAmendmentId.Value)
                  .SetLinkUrl(
                      string.Format(ProgramConstant.ContractAmendment.Url, entity.CamContractAmendmentId),
                      "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}