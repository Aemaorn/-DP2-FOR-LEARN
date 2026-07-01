namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveDeliveryAcceptancePeriodRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid DeliveryAcceptanceId,
    Guid Id,
    AcceptorType Group,
    string? Remark);

public class ApproveDeliveryAcceptancePeriodValidator : Validator<ApproveDeliveryAcceptancePeriodRequest>
{
    public ApproveDeliveryAcceptancePeriodValidator()
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ต้องระบุผู้ใช้งาน");

        this.RuleFor(x => x.DeliveryAcceptanceId)
            .NotEmpty().WithMessage("ต้องระบุการส่งมอบและตรวจรับงาน ");

        this.RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ต้องระบุงวดส่งมอบและตรวจรับงาน ");

        this.RuleFor(x => x.Group)
            .IsInEnum()
            .WithMessage("กลุ่มผู้อนุมัติไม่ถูกต้อง")
            .Must(x =>
                x == AcceptorType.AcceptanceCommittee ||
                x == AcceptorType.Approver)
            .WithMessage("กลุ่มผู้อนุมัติต้องเป็น บุคคล/คณะกรรมการตรวจรับพัสดุ หรือ ผู้มีอำนาจเห็นชอบ/อนุมัติ");
    }
}

public class ApproveDeliveryAcceptancePeriodEndpoint
    : DeliveryAcceptancePeriodEndpointBase<
        ApproveDeliveryAcceptancePeriodRequest,
        Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    private readonly IOperationService operationService;

    public ApproveDeliveryAcceptancePeriodEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<ApproveDeliveryAcceptancePeriodEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance/Period")
             .WithName("ApproveDeliveryAcceptancePeriod")
             .AllowAnonymous()
             .Accepts<ApproveDeliveryAcceptancePeriodRequest>("application/json"));
        this.Put("delivery-acceptance/{DeliveryAcceptanceId:guid?}/period/{Id:guid}/approve");
    }

    protected override async ValueTask<
        Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        ApproveDeliveryAcceptancePeriodRequest req,
        CancellationToken ct)
    {
        var periodExisting =
            await this.GetById(
                CmDeliveryAcceptanceId.From(req.DeliveryAcceptanceId),
                CmDeliveryAcceptancePeriodId.From(req.Id),
                ct);

        if (periodExisting == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลงวดส่งมอบและตรวจรับงาน");
        }

        var deliveryAcceptance = await this.dbContext.CmDeliveryAcceptances.FirstOrDefaultAsync(da => da.Id == periodExisting.CmDeliveryAcceptanceId, ct);

        if (deliveryAcceptance == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลสัญญา ทำสัญญา");
        }

        if (!IsGroupAllowedToApprove(periodExisting.Status, req.Group))
        {
            return
                TypedResults.BadRequest(
                    GetGroupNotAllowedMessage(periodExisting.Status));
        }

        var acceptors =
            periodExisting.Acceptors
                          .Where(a =>
                              a.Type == req.Group &&
                              a.IsActive &&
                              a.Status == AcceptorStatus.Pending)
                          .OrderBy(a => a.Sequence)
                          .ToList();

        var currentAcceptor = acceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (req.Group != AcceptorType.AcceptanceCommittee)
        {
            currentAcceptor = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                                       .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                           ? a.UserId == UserId.From(req.UserId)
                                           : a.Delegatee?.SuUserId == UserId.From(req.UserId));
        }

        if (currentAcceptor == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        if (!IsPreviousApproved(acceptors, currentAcceptor) &&
            periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval)
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        var currentAcceptorUser =
            periodExisting.Acceptors
                          .FirstOrDefault(a => a.Id == currentAcceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(currentAcceptor.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptor.SetCurrent(false);

        if (currentAcceptor.Type == AcceptorType.AcceptanceCommittee)
        {
            UpdateCommitteeCurrents(periodExisting);
        }
        else
        {
            UpdateSequentialCurrents(periodExisting, currentAcceptor.Type);
        }

        var allAcceptorsInGroupApproved =
            acceptors
                .Where(a =>
                    a.Type == req.Group &&
                    !a.IsUnableToPerformDuties)
                .All(a => a.Status == AcceptorStatus.Approved);

        if (!allAcceptorsInGroupApproved || !currentAcceptor.IsBoardChairman())
        {
            switch (periodExisting.Status)
            {
                case CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval:
                    periodExisting.AddActivity(
                        new ActivityInfo(
                            ActivityLogActionTypeConstant.CommitteeApproved,
                            string.Empty,
                            nameof(CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval),
                            req.Remark));

                    break;

                case CmDeliveryAcceptancePeriodStatus.WaitingAcceptance:
                    periodExisting.AddActivity(
                        new ActivityInfo(
                            ActivityLogActionTypeConstant.Approved,
                            string.Empty,
                            nameof(CmDeliveryAcceptancePeriodStatus.Approved),
                            req.Remark));

                    break;
            }
        }

        var statusBeforeTransition = periodExisting.Status;

        if (allAcceptorsInGroupApproved || currentAcceptor.IsBoardChairman())
        {
            ApplyNextStatusAfterAllApprovedNew(
                periodExisting,
                this.IsHasJorPorSection(periodExisting),
                req.Remark);
        }

        await this.ReplaceDocumentsAsync(periodExisting, req.UserId, ct);

        if (periodExisting.Status == CmDeliveryAcceptancePeriodStatus.WaitingAssign)
        {
            var jpAssigner = periodExisting.Assignees.FirstOrDefault(x => x.Type == AssigneeType.Director);

            if (jpAssigner == null)
            {
                this.ThrowError("ไม่พบผู้มอบหมายงาน");
            }

            foreach (var targetUserId in jpAssigner.GetAssigneeNotificationTargets())
            {
                _ = SendNotificationAsync(
                    periodExisting,
                    targetUserId,
                    NotificationConstant.WaitForAssignment.Title,
                    string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
            }
        }

        if (statusBeforeTransition != CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
            periodExisting.Status == CmDeliveryAcceptancePeriodStatus.WaitingAcceptance)
        {
            UpdateSequentialCurrents(periodExisting, AcceptorType.Approver);
        }

        if (periodExisting.Status == CmDeliveryAcceptancePeriodStatus.Approved)
        {
            var committeeMembers = periodExisting.Acceptors
                                                 .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                                                 .ToList();

            foreach (var member in committeeMembers)
            {
                _ = SendNotificationAsync(
                    periodExisting,
                    member.UserId,
                    NotificationConstant.InformCommittee.Title,
                    string.Format(NotificationConstant.InformCommittee.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
            }

            periodExisting.AccountingUpdateStatus(CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval);

            periodExisting.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.SubmitToAccounting,
                    string.Empty,
                    nameof(CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval),
                    "ส่งข้อมูลงวดส่งมอบและตรวจรับงานไปยังฝ่ายบัญชีเพื่อดำเนินการต่อ"));

            var sourceDepartmentOrganizationLevel = await this.GetSourceDepartmentOrganizationLevelAsync(periodExisting, ct);

            var isBranchSource =
                sourceDepartmentOrganizationLevel == EmployeeConstant.OrganizationLevel.Branch ||
                sourceDepartmentOrganizationLevel == EmployeeConstant.OrganizationLevel.Zone ||
                sourceDepartmentOrganizationLevel == EmployeeConstant.OrganizationLevel.Segment;

            var accountingAcceptors = periodExisting.Acceptors
                                                    .Where(a => (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingOperator) && a.IsActive)
                                                    .ToList();

            accountingAcceptors.Iter(a => a.SetStatus(AcceptorStatus.Pending));

            if (isBranchSource || periodExisting.Acceptors.Any(a => a.Type == AcceptorType.AccountingOperator && a.IsActive))
            {
                var firstPending = periodExisting.Acceptors
                                                 .Where(a => (a.Type == AcceptorType.Accounting || a.Type == AcceptorType.AccountingOperator) && a.IsActive && a.Status == AcceptorStatus.Pending)
                                                 .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                                 .ThenBy(a => a.Sequence)
                                                 .FirstOrDefault();

                if (firstPending is not null)
                {
                    foreach (var targetUserId in firstPending.GetNotificationTargets())
                    {
                        _ = SendNotificationAsync(
                            periodExisting,
                            targetUserId,
                            NotificationConstant.AccountingSendApprove.Title,
                            string.Format(NotificationConstant.AccountingSendApprove.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
                    }
                }
            }
            else
            {
                var segmentMembers = await this.operationService.GetSegmentAccountingMembersAsync(ct);

                foreach (var member in segmentMembers)
                {
                    _ = SendNotificationAsync(
                        periodExisting,
                        member.UserId,
                        NotificationConstant.AccountingSendApprove.Title,
                        string.Format(NotificationConstant.AccountingSendApprove.Message, ProgramConstant.ContractAcceptancePeriod.Name, periodExisting.AcceptanceNumber));
                }
            }

            UpdateSequentialCurrents(periodExisting, AcceptorType.Accounting);
        }

        this.dbContext.CmDeliveryAcceptances.Update(deliveryAcceptance);
        this.dbContext.CmDeliveryAcceptancePeriods.Update(periodExisting);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async ValueTask ReplaceDocumentsAsync(
        CmDeliveryAcceptancePeriod entity,
        Guid currentUserId,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var replaceDto =
            await this.MapToReplaceDtoAsync(entity, ct, currentUserId, null);

        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        await ReplaceDocumentAsync(CmDeliveryAcceptanceDocumentType.DeliveryAcceptance);

        async ValueTask ReplaceDocumentAsync(CmDeliveryAcceptanceDocumentType documentType)
        {
            var replaceTemplate = entity.Status is CmDeliveryAcceptancePeriodStatus.WaitingAcceptance
                                      or CmDeliveryAcceptancePeriodStatus.Approved
                ? (entity.DocumentHistories
                       .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                       .Where(dh => dh is
                       {
                           StatusState: CmDeliveryAcceptancePeriodStatus.WaitingAcceptance,
                           IsReplaced: false,
                       })
                       .OrderVersions()
                       .FirstOrDefault()
                   ?? entity.DocumentHistories
                       .Where(dh => dh.DocumentType == CmDeliveryAcceptanceDocumentType.DeliveryAcceptance)
                       .Where(dh => dh is
                       {
                           StatusState: CmDeliveryAcceptancePeriodStatus.WaitingComment,
                           IsReplaced: false,
                       })
                       .OrderVersions()
                       .FirstOrDefault()
                   ?? entity.LastedNotReplacedDocument)
                : entity.LastedNotReplacedDocument;

            if (replaceTemplate is null)
            {
                this.ThrowError($"ไม่พบข้อมูลเอกสาร {documentType.ToString()} ที่ส่งเห็นชอบ");
            }

            var finalFileId =
                await documentService.CopyDocumentTemplateAsync(
                    replaceTemplate.FileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.DeliveryAcceptancePeriod}/{entity.Id}_{documentType.ToString()}_{timeStamp}.odt",
                    cancellationToken: ct);

            if (finalFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารที่ส่งเห็นชอบ");
            }

            entity.AddDocumentHistory(documentType, finalFileId.Value, true);
        }
    }

    private static bool IsGroupAllowedToApprove(
        CmDeliveryAcceptancePeriodStatus status,
        AcceptorType group)
    {
        return (status, group) switch
        {
            (CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval, AcceptorType.AcceptanceCommittee) => true,
            (CmDeliveryAcceptancePeriodStatus.WaitingAcceptance, AcceptorType.Approver) => true,
            _ => false,
        };
    }

    private static string GetGroupNotAllowedMessage(CmDeliveryAcceptancePeriodStatus status)
    {
        return status switch
        {
            CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval =>
                "อนุมัติได้เฉพาะบุคคล/คณะกรรมการตรวจรับพัสดุเท่านั้น",

            CmDeliveryAcceptancePeriodStatus.WaitingAcceptance =>
                "อนุมัติได้เฉพาะผู้มีอำนาจเห็นชอบ/อนุมัติเท่านั้น",

            _ => "ไม่สามารถอนุมัติในสถานะนี้ได้",
        };
    }

    private static bool IsPreviousApproved(
        List<CmDeliveryAcceptancePeriodAcceptor> acceptors,
        CmDeliveryAcceptancePeriodAcceptor currentAcceptor)
    {
        if (currentAcceptor.Sequence <= 1)
        {
            return true;
        }

        var prev =
            acceptors.LastOrDefault(a =>
                a.Sequence < currentAcceptor.Sequence &&
                a.IsActive);

        return
            prev == null ||
            prev.Status == AcceptorStatus.Approved;
    }

    private static void ApplyNextStatusAfterAllApprovedNew(
        CmDeliveryAcceptancePeriod periodExisting,
        bool hasJpSection,
        string? remark = null)
    {
        _ = (periodExisting.Status, hasJorPorSection: hasJpSection) switch
        {
            (CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval, true) =>
                periodExisting.UpdateStatus(CmDeliveryAcceptancePeriodStatus.WaitingAssign, remark),
            (CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval, false) =>
                periodExisting.UpdateStatus(CmDeliveryAcceptancePeriodStatus.WaitingAcceptance, remark),
            (CmDeliveryAcceptancePeriodStatus.WaitingAcceptance, _) =>
                periodExisting.UpdateStatus(CmDeliveryAcceptancePeriodStatus.Approved, remark),
            _ => throw new NotSupportedException("ไม่รองรับการอัพเดตสถานะงวดส่งมอบและตรวจรับงานในสถานะนี้"),
        };
    }

    private static void UpdateCommitteeCurrents(CmDeliveryAcceptancePeriod period)
    {
        var committee = period.Acceptors
                              .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive && !a.IsUnableToPerformDuties)
                              .ToList();

        if (committee.Count == 0)
        {
            return;
        }

        var chairman = committee.FirstOrDefault(IsChairman);
        var nonChair = chairman is null ? committee : [.. committee.Where(a => a.Id != chairman.Id)];

        foreach (var a in committee)
        {
            a.SetCurrent(false);
        }

        var pendingNonChair = nonChair.Where(a => a.Status == AcceptorStatus.Pending).ToList();

        if (pendingNonChair.Count > 0)
        {
            foreach (var p in pendingNonChair)
            {
                p.SetCurrent(true);
            }

            return;
        }

        var allNonChairReady = nonChair.All(a => a.Status == AcceptorStatus.Approved || a.IsUnableToPerformDuties);

        if (chairman is not null && chairman.Status == AcceptorStatus.Pending && allNonChairReady)
        {
            chairman.SetCurrent(true);

            _ = SendNotificationAsync(
                period,
                chairman.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAcceptancePeriod.Name, period.AcceptanceNumber));
        }
    }

    private static bool IsChairman(CmDeliveryAcceptancePeriodAcceptor a)
    {
        if (a.CommitteePositionsCode != null && a.CommitteePositionsCode == ParameterCode.From("PosBoard001"))
        {
            return true;
        }

        return a.IsBoardChairman();
    }

    private static void UpdateSequentialCurrents(CmDeliveryAcceptancePeriod period, AcceptorType type)
    {
        var approvers = period.Acceptors
                              .Where(a => (a.Type == type ||
                                           (type == AcceptorType.Accounting && a.Type == AcceptorType.AccountingOperator))
                                          && a.IsActive
                                          && !a.IsUnableToPerformDuties)
                              .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                              .ThenBy(a => a.Sequence)
                              .ToList();

        if (approvers.Count == 0)
        {
            return;
        }

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    period,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAcceptancePeriod.Name, period.AcceptanceNumber));
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    period,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractAcceptancePeriod.Name, period.AcceptanceNumber));
            }
        }
        else if ((next.Type == AcceptorType.Accounting || next.Type == AcceptorType.AccountingOperator) && !isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    period,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.ContractAcceptancePeriod.Name, period.AcceptanceNumber));
            }
        }
        else if ((next.Type == AcceptorType.Accounting || next.Type == AcceptorType.AccountingOperator) && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    period,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ContractAcceptancePeriod.Name, period.AcceptanceNumber));
            }
        }
    }

    private static async Task SendNotificationAsync(CmDeliveryAcceptancePeriod period, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(period.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractAcceptancePeriod.Url, period.CmDeliveryAcceptance.Id, period.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}