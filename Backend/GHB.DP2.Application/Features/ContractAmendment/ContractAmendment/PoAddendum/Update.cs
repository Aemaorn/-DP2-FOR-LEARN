namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoAddendum.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdatePoAddendumRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    CamContractAmendmentPoAddendumId Id,
    CamContractAmendmentId CamContractAmendmentId,
    string ContractNumber,
    string PoNumber,
    string SapNumber,
    Guid VendorId,
    Guid? ContractAddendumDocumentId,
    bool? IsContractAddendumDocumentIdReplaced,
    Guid? ContractAmendmentRequestDocumentId,
    bool? IsContractAmendmentRequestDocumentIdReplaced,
    CamContractAmendmentPoAddendumStatus Status,
    IEnumerable<AcceptorRequest>? Acceptors,
    IEnumerable<AssigneeRequest>? Assignees,
    IEnumerable<PaymentTermRequest>? PaymentTerms);

public record UpdatePoAddendumResponse(Guid Id, Guid? NewDocumentFileId);

public class UpdatePoAddendumRequestValidator : Validator<UpdatePoAddendumRequest>
{
    public UpdatePoAddendumRequestValidator()
    {
        this.RuleFor(r => r.Id).NotEmpty();
        this.RuleFor(r => r.CamContractAmendmentId).NotEmpty();
        this.RuleFor(r => r.ContractNumber).NotEmpty();
        this.RuleFor(r => r.PoNumber).NotEmpty();
        this.RuleFor(r => r.SapNumber).NotEmpty();
        this.RuleFor(r => r.VendorId).NotEmpty();
        this.RuleFor(r => r.Status).IsInEnum();
        this.RuleForEach(r => r.Acceptors).SetValidator(new AcceptorRequestValidator());
    }
}

public class UpdatePoAddendumEndpoint : PoAddendumAbstractEndpoint<UpdatePoAddendumRequest, Results<Ok<UpdatePoAddendumResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePoAddendumEndpoint(ILogger<UpdatePoAddendumEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("contract-amendment/{CamContractAmendmentId:guid}/po-addendum/{Id:guid}");
        this.Description(b => b
                              .WithTags("ContractAmendment/PoAddendum")
                              .WithName("UpdatePoAddendum")
                              .Produces<Ok<CamContractAmendmentPoAddendumId>>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<UpdatePoAddendumResponse>, NotFound<string>>> HandleRequestAsync(UpdatePoAddendumRequest req, CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var cam = await this.GetContractAmendmentAsync(req.CamContractAmendmentId, ct);
        if (cam is null || cam.ContractDraftVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการแก้ไขสัญญาหรือคู่ค้าสัญญาที่เกี่ยวข้อง");
        }

        var existingPo = await this.GetExistingPoAddendumAsync(req, ct);
        if (existingPo is null)
        {
            this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญาที่ระบุ", StatusCodes.Status404NotFound);
        }

        this.ValidateCanEdit(existingPo);

        var vendor = await this.GetVendorAsync(req.VendorId, ct);
        if (vendor is null)
        {
            this.ThrowError("ไม่พบข้อมูลผู้ประกอบการ", StatusCodes.Status404NotFound);
        }

        await this.UpdatePoAddendumDataAsync(existingPo, req, vendor!, ct);
        FileId? newDocumentFileId = await this.HandleDocumentHistoryAsync(existingPo, req, ct);
        await this.HandleStatusChangeAsync(existingPo, req, cam, ct);

        this.dbContext.CamContractAmendmentPoAddendums.Update(existingPo);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdatePoAddendumResponse(existingPo.Id.Value, newDocumentFileId?.Value));
    }

    private async Task ValidateRequestAsync(UpdatePoAddendumRequest req, CancellationToken ct)
    {
        var validator = new UpdatePoAddendumRequestValidator();
        var validation = await validator.ValidateAsync(req, ct);

        if (!validation.IsValid)
        {
            var invalidProps = validation.Errors.Select(e => e.PropertyName).Distinct();
            var message = $"ข้อมูลไม่ถูกต้อง: {string.Join(", ", invalidProps)}";
            this.ThrowError(message, StatusCodes.Status400BadRequest);
        }
    }

    private async Task<CamContractAmendment?> GetContractAmendmentAsync(CamContractAmendmentId camContractAmendmentId, CancellationToken ct)
    {
        return await this.dbContext.CamContractAmendments
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(v => v.PaymentTerms)
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(v => v.Vendor)
                            .ThenInclude(v => v.VendorInfo)
                            .Include(c => c.ContractDraftVendor)
                            .ThenInclude(cd => cd.ContractDraft)
                            .ThenInclude(p => p.Procurement)
                            .SingleOrDefaultAsync(c => c.Id == camContractAmendmentId, ct);
    }

    private async Task<CamContractAmendmentPoAddendum?> GetExistingPoAddendumAsync(UpdatePoAddendumRequest req, CancellationToken ct)
    {
        return await this.dbContext.CamContractAmendmentPoAddendums
                           .Include(p => p.Acceptors)
                           .Include(p => p.Assignees)
                           .Include(p => p.PaymentTerms)
                           .Include(camContractAmendmentPoAddendum => camContractAmendmentPoAddendum.CamContractAmendment)
                           .SingleOrDefaultAsync(p => p.Id == req.Id && p.CamContractAmendmentId == req.CamContractAmendmentId, ct);
    }

    private void ValidateCanEdit(CamContractAmendmentPoAddendum existingPo)
    {
        var canEdit = IsEditableStatus(existingPo.Status);
        if (!canEdit)
        {
            this.ThrowError($"บันทึกต่อท้ายนี้ไม่อยู่ในสถานะที่สามารถแก้ไขได้ (สถานะปัจจุบัน: {existingPo.Status})", StatusCodes.Status409Conflict);
        }
    }

    private static bool IsEditableStatus(CamContractAmendmentPoAddendumStatus status)
    {
        return status is
            CamContractAmendmentPoAddendumStatus.Edit or
            CamContractAmendmentPoAddendumStatus.Draft or
            CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval or
            CamContractAmendmentPoAddendumStatus.WaitingAssigned or
            CamContractAmendmentPoAddendumStatus.WaitingComment or
            CamContractAmendmentPoAddendumStatus.Rejected or
            CamContractAmendmentPoAddendumStatus.WaitingApproval;
    }

    private async Task<SuVendor?> GetVendorAsync(Guid vendorId, CancellationToken ct)
    {
        return await this.dbContext.SuVendors.SingleOrDefaultAsync(v => v.Id == SuVendorId.From(vendorId), ct);
    }

    private async Task UpdatePoAddendumDataAsync(CamContractAmendmentPoAddendum existingPo, UpdatePoAddendumRequest req, SuVendor vendor, CancellationToken ct)
    {
        existingPo.SetValues(req.ContractNumber, req.SapNumber, req.PoNumber, vendor);

        if (req.PaymentTerms != null)
        {
            this.UpsertPaymentTerm(existingPo, req.PaymentTerms);
        }

        if (req.Acceptors != null && req.Acceptors.Any())
        {
            await this.UpsertAcceptors(existingPo, [.. req.Acceptors], req.Status, ct);
        }

        if (req.Assignees != null && req.Assignees.Any())
        {
            await this.UpsertAssignee(existingPo, [.. req.Assignees], ct);
        }

        existingPo.SetStatus(req.Status);
        MapActivity(existingPo, req.Status);
    }

    private async Task<FileId?> HandleDocumentHistoryAsync(CamContractAmendmentPoAddendum existingPo, UpdatePoAddendumRequest req, CancellationToken ct)
    {
        FileId? newDocumentFileId = null;

        var mustSaveAddendumDocument =
            ShouldUpdateContractAddendum(req) &&
            req.Status != CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval;

        if (mustSaveAddendumDocument)
        {
            newDocumentFileId = await this.UpdateDocumentHistoryAsync(
                existingPo,
                CamContractAmendmentPoAddendumDocumentType.ContractAddendum,
                FileId.From(req.ContractAddendumDocumentId!.Value),
                req.IsContractAddendumDocumentIdReplaced,
                ct);
        }

        if (ShouldUpdateContractAmendmentRequest(req))
        {
            existingPo.AddDocumentHistory(
                CamContractAmendmentPoAddendumDocumentType.ContractAmendmentRequest,
                FileId.From(req.ContractAmendmentRequestDocumentId!.Value),
                req.IsContractAmendmentRequestDocumentIdReplaced!.Value);
        }

        return newDocumentFileId;
    }

    private static bool ShouldUpdateContractAddendum(UpdatePoAddendumRequest req)
    {
        return req is { ContractAddendumDocumentId: not null, IsContractAddendumDocumentIdReplaced: not null } &&
               req.IsContractAddendumDocumentIdReplaced.Value;
    }

    private static bool ShouldUpdateContractAmendmentRequest(UpdatePoAddendumRequest req)
    {
        return req is { ContractAmendmentRequestDocumentId: not null, IsContractAmendmentRequestDocumentIdReplaced: not null } &&
               req.IsContractAmendmentRequestDocumentIdReplaced.Value;
    }

    private async Task HandleStatusChangeAsync(CamContractAmendmentPoAddendum existingPo, UpdatePoAddendumRequest req, CamContractAmendment cam, CancellationToken ct)
    {
        switch (req.Status)
        {
            case CamContractAmendmentPoAddendumStatus.Edit:
                await HandleEditStatusAsync(existingPo);
                break;

            case CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval:
                await this.HandleWaitingCommitteeApprovalAsync(existingPo, req, cam, ct);
                break;

            case CamContractAmendmentPoAddendumStatus.WaitingComment:
                await HandleWaitingCommentAsync(existingPo);
                break;

            case CamContractAmendmentPoAddendumStatus.WaitingApproval:
                await this.HandleWaitingApprovalAsync(existingPo);
                break;
        }
    }

    private static Task HandleEditStatusAsync(CamContractAmendmentPoAddendum existingPo)
    {
        existingPo.Acceptors.Where(x => !x.IsUnableToPerformDuties).Iter(r => r.Draft());
        return Task.CompletedTask;
    }

    private async Task HandleWaitingCommitteeApprovalAsync(CamContractAmendmentPoAddendum existingPo, UpdatePoAddendumRequest req, CamContractAmendment cam, CancellationToken ct)
    {
        existingPo.SetWaitingCommitteeApproval();
        await this.UpdateDocumentAsync(cam, existingPo, UserId.From(req.UserId), isReplace: true, hasCreator: true, hasAcceptor: false, ct);
        EnsureInitialCommitteeCurrents(existingPo);
    }

    private static void EnsureInitialCommitteeCurrents(CamContractAmendmentPoAddendum entity)
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

    private static Task HandleWaitingCommentAsync(CamContractAmendmentPoAddendum existingPo)
    {
        existingPo.SetWaitingComment();
        _ = SendNotificationAssigneeAsync(existingPo, CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task HandleWaitingApprovalAsync(CamContractAmendmentPoAddendum existingPo)
    {
        existingPo.SetWaitingApproval();

        var firstPendingApprover = GetFirstPendingApprover(existingPo);
        if (firstPendingApprover != null)
        {
            await SendApprovalNotificationAsync(existingPo, firstPendingApprover);
        }
    }

    private static CamContractAmendmentPoAddendumAcceptor? GetFirstPendingApprover(CamContractAmendmentPoAddendum existingPo)
    {
        return existingPo.Acceptors
                         .Where(p => p.Type == AcceptorType.Approver)
                         .OrderBy(a => a.Sequence)
                         .FirstOrDefault(a => a.Status == AcceptorStatus.Pending && a.IsCurrent);
    }

    private static Task SendApprovalNotificationAsync(CamContractAmendmentPoAddendum existingPo, CamContractAmendmentPoAddendumAcceptor approver)
    {
        foreach (var targetUserId in approver.GetNotificationTargets())
        {
            _ = SendNotificationAsync(
                existingPo,
                targetUserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(
                    NotificationConstant.WaitForLike.Message,
                    ProgramConstant.PreProcurementTorDraft.Name,
                    existingPo.CamContractAmendment.CamContractAmendmentNumber));
        }

        return Task.CompletedTask;
    }

    private static async Task SendNotificationAsync(CamContractAmendmentPoAddendum entity, UserId userId, string title, string message)
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

    private static async Task SendNotificationAssigneeAsync(CamContractAmendmentPoAddendum entity, CancellationToken ct)
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

    private static void MapActivity(CamContractAmendmentPoAddendum entity, CamContractAmendmentPoAddendumStatus reqStatus)
    {
        if (reqStatus != entity.Status)
        {
            _ = reqStatus switch
            {
                CamContractAmendmentPoAddendumStatus.Edit => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        "เรียกคืนแก้ไข",
                        nameof(CamContractAmendmentPoAddendumStatus.Edit))),
                CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendCommitteeApprove,
                        "ส่งเห็นชอบ",
                        nameof(CamContractAmendmentPoAddendumStatus.WaitingCommitteeApproval))),
                CamContractAmendmentPoAddendumStatus.WaitingAssigned => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.WaitingComment,
                        "ยืนยันมอบหมาย",
                        nameof(CamContractAmendmentPoAddendumStatus.WaitingAssigned))),
                CamContractAmendmentPoAddendumStatus.WaitingApproval => entity.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendApprove,
                        "ส่งผู้มีอำนาจเห็นชอบ/อนุมัติ",
                        nameof(CamContractAmendmentPoAddendumStatus.WaitingApproval))),
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
}