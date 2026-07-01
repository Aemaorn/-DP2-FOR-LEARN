namespace GHB.DP2.Application.Features.Plan.Plan;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Plan.Plan.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdatePlanRequest(
    Guid Id,
    PlanStatus Status,
    string DepartmentCode,
    PlanType Type,
    string SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    int BudgetYear,
    string Name,
    decimal Budget,
    DateTimeOffset ExpectingProcurementAt,
    string? Remark,
    string? Telephone,
    bool IsStock,
    string? AssignSegmentCode,
    string? GroupEgpNumber,
    string? EgpNumber,
    DateTimeOffset? DocumentDate,
    bool? IsCommercialMaterial,
    Guid? PlanDocumentId,
    bool? IsPlanDocumentIdReplace,
    Guid? PlanAnnouncementDocumentId,
    bool? IsPlanAnnouncementDocumentIdReplace,
    string? CancelReason,
    string? ChangeReason,
    AcceptorRequest[]? Acceptors,
    AttachmentsDtoWithId[]? Attachments,
    DateTimeOffset? LastModifiedAt);

public class UpdatePlanRequestValidator : Validator<UpdatePlanRequest>
{
    public UpdatePlanRequestValidator()
    {
        this.RuleFor(p => p.DepartmentCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุ ฝ่าย/ภาค/เขต");

        this.RuleFor(p => p.Type)
            .IsInEnum()
            .WithMessage("ประเภทแผนจัดซื้อจัดจ้างไม่ถูกต้อง");

        this.RuleFor(p => p.SupplyMethodCode)
            .NotEmpty()
            .WithMessage("กรุณาระบุวิธีการจัดหา");

        this.RuleFor(p => p.BudgetYear)
            .GreaterThan(0)
            .WithMessage("ปีงบประมาณต้องมากกว่าศูนย์");

        this.RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage("กรุณาระบุชื่อโครงการ");

        this.RuleFor(p => p.Budget)
            .GreaterThan(0)
            .WithMessage("วงเงินงบประมาณต้องมากกว่าศูนย์");

        this.RuleFor(p => p.Telephone)
            .MaximumLength(20)
            .WithMessage("ไม่สามารถกรอกเบอร์โทรเกิน 20 ตัวได้");

        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()))
            .When(x => x.Attachments is not null);
    }
}

public record UpdatePlanResponse(Guid? NewPlanDocumentFileId, Guid? NewPlanAnnouncementDocumentFileId);

public class UpdatePlanEndpoint : PlanEndpointBase<UpdatePlanRequest, Results<Ok<UpdatePlanResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePlanEndpoint(
        ILogger<UpdatePlanEndpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Plan")
             .WithName("UpdatePlan")
             .Produces<Ok<UpdatePlanResponse>>()
             .Produces<NotFound>()
             .Accepts<UpdatePlanRequest>("application/json"));
        this.Put("plan/{Id:guid}");
        this.AuditLog("รายการจัดซื้อจัดจ้าง", "แก้ไขแผนจัดซื้อจัดจ้าง");
    }

    protected override async ValueTask<Results<Ok<UpdatePlanResponse>, NotFound<string>>> HandleRequestAsync(
        UpdatePlanRequest req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var data = await this.dbContext.Plans
                             .Include(plan => plan.Acceptors)
                             .ThenInclude(a => a.Delegatee)
                             .Include(plan => plan.Assignees)
                             .ThenInclude(a => a.User)
                             .ThenInclude(u => u.Employee)
                             .Include(plan => plan.Attachments)
                             .Include(auditableEntity => auditableEntity.AuditInfo)
                             .Include(plan => plan.DocumentHistories)
                             .SingleOrDefaultAsync(p => p.Id == PlanId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"Plan with Id {req.Id} not found");
        }

        if (req.LastModifiedAt.HasValue &&
            data.AuditInfo.LastModifiedAt?.ToUnixTimeMilliseconds() != req.LastModifiedAt.Value.ToUnixTimeMilliseconds())
        {
            this.ThrowError("ข้อมูลถูกแก้ไขโดยผู้อื่นแล้ว", StatusCodes.Status409Conflict);
        }

        await this.UpdateAcceptorAsync(req, data, ct);

        data.SetDepartment(BusinessUnitId.From(req.DepartmentCode))
            .SetSupplyMethod(ParameterCode.From(req.SupplyMethodCode))
            .SetName(req.Name)
            .SetBudget(req.Budget)
            .SetBudgetYear(req.BudgetYear)
            .SetExpectingProcurementAt(req.ExpectingProcurementAt)
            .SetRemark(req.Remark)
            .SetTelephone(req.Telephone)
            .SetIsStock(req.IsStock)
            .SetIsCommercialMaterial(req.IsCommercialMaterial)
            .SetType(req.Type);

        if (data.IsChange)
        {
            data.SetChangeReason(req.ChangeReason);
        }

        if (data.IsCancel)
        {
            data.SetCancelReason(req.CancelReason);
        }

        var createByUserData = await this.dbContext.SuUsers
                                         .FirstOrDefaultAsync(x => x.Id == UserId.From(data.AuditInfo.CreatedBy), ct);

        SetOptionalProperties(data, req);

        switch (req.Status)
        {
            case PlanStatus.DraftPlan:
                data.SetDraft();

                break;

            case PlanStatus.EditPlan:
                data.SetEdit(req.Status);

                break;

            case PlanStatus.WaitingApprovePlan:
                data.SetWaitingApprovePlan();
                await this.SendNotificationAsync(data, AcceptorType.DepartmentDirectorAgree, ct);

                break;

            case PlanStatus.WaitingAssign:
                data.SetWaitingAssign(req.Status);
                await this.SendNotificationAssigneeAsync(data, AssigneeType.Assignee, ct);

                break;

            case PlanStatus.Assigned:
                data.SetAssigned();

                break;

            case PlanStatus.DraftRecordDocument:
                data.SetDraftRecordDocument(req.Status);

                break;

            case PlanStatus.WaitingAcceptor:
                data.SetWaitingAcceptor();

                break;

            case PlanStatus.RejectToAssignee:
                data.SetRejectToAssignee(data.Status);

                break;

            case PlanStatus.ApprovePlan:
                data.SetApprovePlan();
                await this.SendNotificationCreateByAsync(data, createByUserData, ct);

                break;

            case PlanStatus.WaitingAnnouncement:
                data.SetWaitingAnnouncement();

                break;

            case PlanStatus.Announcement:
                data.SetAnnouncement();
                await this.SendNotificationCreateByAnnouncementAsync(data, createByUserData, ct);

                break;

            case PlanStatus.RejectPlan:
                data.SetRejected(req.Status);

                break;
        }

        var statusToUpdateDocument = data.Status == PlanStatus.WaitingAssign || data.Status == PlanStatus.Assigned || data.Status == PlanStatus.DraftRecordDocument || data.Status == PlanStatus.RejectToAssignee;

        FileId? newPlanDocumentFileId = null;
        FileId? newPlanAnnouncementDocumentFileId = null;

        if (req is { PlanDocumentId: not null, IsPlanDocumentIdReplace: true }
            && statusToUpdateDocument)
        {
            newPlanDocumentFileId = await this.UpdateDocumentHistoryAsync(
                data,
                PlanDocumentType.Plan,
                FileId.From(req.PlanDocumentId.Value),
                true,
                ct);
        }

        if (req is { PlanAnnouncementDocumentId: not null, IsPlanAnnouncementDocumentIdReplace: true }
            && statusToUpdateDocument)
        {
            newPlanAnnouncementDocumentFileId = await this.UpdateDocumentHistoryAsync(
                data,
                PlanDocumentType.Announcement,
                FileId.From(req.PlanAnnouncementDocumentId.Value),
                true,
                ct);
        }

        if (data.Type == PlanType.InYearPlan && data.Status == PlanStatus.WaitingAssign)
        {
            await this.SetDefaultDocumentTemplate(data, ct);

            if (this.PlanDocumentCondition(data))
            {
                await this.UpdateDocumentAsync(
                    data,
                    data.Status == PlanStatus.WaitingAssign,
                    isPlanAnnouncementDocumentIdReplace: req.IsPlanAnnouncementDocumentIdReplace ?? false,
                    cancellationToken: ct);
            }
        }

        if (req.Attachments != null)
        {
            await this.ValidateDocumentTypeCode(req.Attachments, ct);
            await this.UpsertAttachments(data, req.Attachments);
        }

        this.dbContext.Plans.Update(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdatePlanResponse(
            newPlanDocumentFileId?.Value,
            newPlanAnnouncementDocumentFileId?.Value));
    }

    private static void SetOptionalProperties(Plan plan, UpdatePlanRequest req)
    {
        if (req.SupplyMethodTypeCode is not null)
        {
            plan.SetSupplyMethodType(ParameterCode.From(req.SupplyMethodTypeCode));
        }

        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            plan.SetSupplyMethodSpecialType(ParameterCode.From(req.SupplyMethodSpecialTypeCode));
        }

        if (req.GroupEgpNumber is not null)
        {
            plan.SetGroupEgpNumber(req.GroupEgpNumber);
        }

        if (req.EgpNumber is not null)
        {
            plan.SetEgpNumber(req.EgpNumber);
        }

        if (req.DocumentDate is not null)
        {
            plan.SetDocumentDate(req.DocumentDate);
        }

        if (req.AssignSegmentCode is not null)
        {
            plan.SetAssignSegment(ParameterCode.From(req.AssignSegmentCode));
        }
    }

    private async Task ValidateRequestAsync(UpdatePlanRequest req, CancellationToken ct)
    {
        // Check if the department exists
        var department = await this.dbContext.RawBusinessUnits
                                   .FirstOrDefaultAsync(d => d.Id == BusinessUnitId.From(req.DepartmentCode), ct);

        if (department is null)
        {
            this.ThrowError(
                r => r.DepartmentCode,
                $"Department with code {req.DepartmentCode} not found.",
                StatusCodes.Status404NotFound);
        }

        // Validate supply method parameter
        var supplyMethod = await this.dbContext.SuParameters
                                     .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodCode), ct);

        if (supplyMethod is null)
        {
            this.ThrowError(
                r => r.SupplyMethodCode,
                $"Supply method with code {req.SupplyMethodCode} not found.",
                StatusCodes.Status404NotFound);
        }

        // Validate optional parameter codes
        if (req.SupplyMethodTypeCode is not null)
        {
            var supplyMethodType = await this.dbContext.SuParameters
                                             .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodTypeCode), ct);

            if (supplyMethodType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodTypeCode,
                    $"Supply method type with code {req.SupplyMethodTypeCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }

        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            var supplyMethodSpecialType = await this.dbContext.SuParameters
                                                    .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);

            if (supplyMethodSpecialType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodSpecialTypeCode,
                    $"Supply method special type with code {req.SupplyMethodSpecialTypeCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }

        if (req.AssignSegmentCode is not null)
        {
            var assignSegment = await this.dbContext.SuParameters
                                          .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.AssignSegmentCode), ct);

            if (assignSegment is null)
            {
                this.ThrowError(
                    r => r.AssignSegmentCode,
                    $"Assign segment with code {req.AssignSegmentCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }
    }

    private async Task UpdateAcceptorAsync(UpdatePlanRequest req, Plan data, CancellationToken ct)
    {
        if (req is { Acceptors: not null } && data.Status is PlanStatus.DraftPlan or PlanStatus.EditPlan or PlanStatus.RejectPlan)
        {
            await this.UpsertAcceptors(data, req.Acceptors, data.Status, ct);
        }

        if (req is { Status: PlanStatus.WaitingApprovePlan, Acceptors: not null } && data.Status is PlanStatus.RejectPlan or PlanStatus.EditPlan or PlanStatus.DraftPlan)
        {
            UpdatePendingStatusAcceptor(data);
        }

        if (req is { Status: PlanStatus.WaitingApprovePlan } && data.Status == PlanStatus.RejectPlan)
        {
            UpdateAcceptorWhenSendApprove(data);
        }
    }

    private static void UpdatePendingStatusAcceptor(Plan plan)
    {
        plan.Acceptors.Where(w => w.Type == AcceptorType.DepartmentDirectorAgree).Iter(a => a.Pending());
    }

    private static void UpdateAcceptorWhenSendApprove(
        Plan plan)
    {
        plan.Acceptors.Where(w => w.Type == AcceptorType.DepartmentDirectorAgree).Iter(a => a.Pending());
    }

    private async Task SendNotificationAsync(Plan plan, AcceptorType type, CancellationToken ct)
    {
        var acceptor = plan.Acceptors
                           .Where(x => x.Type == type)
                           .Select(DelegatorExtensions.DelegatorToAcceptor)
                           .FirstOrDefault(a => a.IsCurrent);

        if (acceptor is null)
        {
            return;
        }

        foreach (var targetUserId in await this.dbContext.GetNotificationTargetsWithSecretariesAsync(acceptor, ct))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.WaitForLike.Title,
                      string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.Plan.Name, plan.PlanNumber),
                      NotificationProgram.Plan)
                  .SetReferenceId(plan.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private async Task SendNotificationAssigneeAsync(Plan plan, AssigneeType type, CancellationToken ct)
    {
        var assignee = plan.Assignees
                           .Where(x => x.Type == type)
                           .OrderBy(x => x.Sequence)
                           .FirstOrDefault();

        if (assignee is null)
        {
            return;
        }

        foreach (var targetUserId in await this.dbContext.GetAssigneeNotificationTargetsWithSecretariesAsync(assignee, ct))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.WaitForAssignment.Title,
                      string.Format(NotificationConstant.WaitForAssignment.Message, ProgramConstant.Plan.Name, plan.PlanNumber),
                      NotificationProgram.Plan)
                  .SetReferenceId(plan.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private async Task SendNotificationCreateByAsync(Plan plan, SuUser? user, CancellationToken ct)
    {
        if (user is null)
        {
            return;
        }

        foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(user.Id, ct))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.InformCommittee.Title,
                      string.Format(NotificationConstant.InformCommittee.Message, ProgramConstant.Plan.Name, plan.PlanNumber),
                      NotificationProgram.Plan)
                  .SetReferenceId(plan.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }

    private async Task SendNotificationCreateByAnnouncementAsync(Plan plan, SuUser? user, CancellationToken ct)
    {
        if (user is null)
        {
            return;
        }

        foreach (var targetUserId in await this.dbContext.GetNotificationTargetsForUserAsync(user.Id, ct))
        {
            await Notification
                  .Crate(
                      targetUserId,
                      NotificationConstant.InformAnnouncement.Title,
                      string.Format(NotificationConstant.InformAnnouncement.Message, ProgramConstant.Plan.Name, plan.PlanNumber),
                      NotificationProgram.Plan)
                  .SetReferenceId(plan.Id.Value)
                  .SetLinkUrl(string.Format(ProgramConstant.Plan.Url, plan.Id), "ดูรายละเอียด")
                  .PublishAsync(ct);
        }
    }
}