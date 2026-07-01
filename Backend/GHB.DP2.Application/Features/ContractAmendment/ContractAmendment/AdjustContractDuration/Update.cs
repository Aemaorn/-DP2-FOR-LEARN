namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Abstract;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAdjustContractDurationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Guid CamContractAmendmentId,
    Guid? ContractAddendumDocumentId,
    bool? IsContractAddendumDocumentIdReplaced,
    Guid? ContractAmendmentRequestDocumentId,
    bool? IsContractAmendmentRequestDocumentIdReplaced,
    AdjustContractDurationInfo AdjustContractDurationOld,
    AdjustContractDurationInfo AdjustContractDurationNew,
    AcceptorRequest[]? Acceptors,
    AssigneeRequest[]? Assignees,
    ContractAmendmentExtendChangeStatus Status);

public record UpdateAdjustContractDurationResponse(Guid? NewDocumentFileId);

public class UpdateAdjustContractDurationEndpoint : AdjustContractDurationEndpointBase<UpdateAdjustContractDurationRequest, Results<Ok<UpdateAdjustContractDurationResponse>, NotFound<string>, BadRequest<string>>>
{
    public UpdateAdjustContractDurationEndpoint(ILogger<UpdateAdjustContractDurationEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/adjust-contract-duration/{Id:guid}");
        this.Description(b =>
            b.WithTags("ContractAmendment/AdjustContractDuration")
             .WithName("UpdateAdjustContractDuration")
             .Produces<Ok>()
             .Produces<NotFound<string>>(StatusCodes.Status404NotFound)
             .Produces<BadRequest<string>>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<UpdateAdjustContractDurationResponse>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateAdjustContractDurationRequest req, CancellationToken ct)
    {
        var entity =
            await this.DbContext.CamContractAmendmentExtendChanges.Include(camContractAmendmentExtendChange => camContractAmendmentExtendChange.Acceptors)
                      .Include(camContractAmendmentExtendChange => camContractAmendmentExtendChange.CamContractAmendment)
                      .FirstOrDefaultAsync(
                          e => e.Id == ContractAmendmentExtendChangeId.From(req.Id) &&
                               e.CamContractAmendmentId == CamContractAmendmentId.From(req.CamContractAmendmentId),
                          ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาที่ระบุ");
        }

        var adjustContractDurationNew = req.AdjustContractDurationNew;

        entity.SetValues(
            adjustContractDurationNew.ChangeType!.Value,
            adjustContractDurationNew.WorkStartDate!.Value,
            adjustContractDurationNew.NewEndDate!.Value);

        if (adjustContractDurationNew.PaymentTypeCode is not null)
        {
            entity.SetPaymentType(adjustContractDurationNew.PaymentTypeCode);
        }

        this.UpsertPaymentTerms(entity, adjustContractDurationNew.PaymentTerms);

        if (req.Acceptors is not null)
        {
            await this.UpsertAcceptors(
                entity,
                req.Acceptors,
                entity.Status,
                ct);
        }

        if (req.Assignees is not null)
        {
            await this.UpsertAssignee(
                entity,
                req.Assignees,
                ct);
        }

        entity.SetStatus(req.Status);

        MapActivity(entity, req.Status);

        FileId? newDocumentFileId = null;

        var mustSaveAddendumDocument =
            req.ContractAddendumDocumentId.HasValue &&
            req.IsContractAddendumDocumentIdReplaced.GetValueOrDefault() &&
            req.Status != ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval;

        if (mustSaveAddendumDocument)
        {
            newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                ExtendChangeAcceptorDocumentType.ExtendChange,
                FileId.From(req.ContractAddendumDocumentId!.Value),
                req.IsContractAddendumDocumentIdReplaced,
                ct);
        }

        if (req is { ContractAmendmentRequestDocumentId: not null, IsContractAmendmentRequestDocumentIdReplaced: not null } && req.IsContractAmendmentRequestDocumentIdReplaced.Value)
        {
            entity.AddDocumentHistory(
                ExtendChangeAcceptorDocumentType.Approved,
                FileId.From(req.ContractAmendmentRequestDocumentId!.Value),
                req.IsContractAmendmentRequestDocumentIdReplaced.Value);
        }

        switch (req.Status)
        {
            case ContractAmendmentExtendChangeStatus.Edit:
                entity.SetEdit();

                break;

            case ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval:
                entity.SetWaitingCommitteeApproval();
                await this.UpdateDocumentAsync(entity, UserId.From(req.UserId), isReplace: true, hasCreator: true, hasAcceptor: false, ct);
                EnsureInitialCommitteeCurrents(entity);

                break;

            case ContractAmendmentExtendChangeStatus.WaitingComment:
                entity.SetWaitingComment();
                _ = SendNotificationAssigneeAsync(entity, CancellationToken.None);

                break;

            case ContractAmendmentExtendChangeStatus.WaitingApproval:
                entity.SetWaitingApproval();

                var approvers = entity.Acceptors
                                      .Where(p => p.Type == AcceptorType.Approver)
                                      .OrderBy(a => a.Sequence)
                                      .ToList();

                var firstPending = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);

                if (firstPending != null)
                {
                    foreach (var targetUserId in firstPending.GetNotificationTargets())
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
                }

                break;
        }

        this.DbContext.CamContractAmendmentExtendChanges.Update(entity);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateAdjustContractDurationResponse(newDocumentFileId?.Value));
    }

    private static void MapActivity(CamContractAmendmentExtendChange entity, ContractAmendmentExtendChangeStatus reqStatus)
    {
        if (reqStatus != entity.Status)
        {
            _ = reqStatus switch
            {
                ContractAmendmentExtendChangeStatus.Edit => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        "เรียกคืนแก้ไข",
                        nameof(ContractAmendmentExtendChangeStatus.Edit))),
                ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        "ส่งเห็นชอบ",
                        nameof(ContractAmendmentExtendChangeStatus.WaitingCommitteeApproval))),
                ContractAmendmentExtendChangeStatus.WaitingAssigned => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.WaitingComment,
                        "ยืนยันมอบหมาย",
                        nameof(ContractAmendmentExtendChangeStatus.WaitingAssigned))),
                ContractAmendmentExtendChangeStatus.WaitingApproval => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendApprove,
                        "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                        nameof(ContractAmendmentExtendChangeStatus.WaitingApproval))),
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

    private static void EnsureInitialCommitteeCurrents(CamContractAmendmentExtendChange entity)
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

    private static async Task SendNotificationAsync(CamContractAmendmentExtendChange entity, UserId userId, string title, string message)
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

    private static async Task SendNotificationAssigneeAsync(CamContractAmendmentExtendChange entity, CancellationToken ct)
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