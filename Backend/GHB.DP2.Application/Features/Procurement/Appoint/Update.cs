namespace GHB.DP2.Application.Features.Procurement.Appoint;

using LanguageExt;
using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Appoint.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateAppointRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    UpdateAppointDto Appoint,
    decimal Budget,
    Guid? AppointDocumentId,
    bool? IsAppointDocumentIdReplaced,
    IEnumerable<UpdateAppointTorDraftCommitteeDto> TorDraftCommittees,
    IEnumerable<UpdateDutiesDto> TorDraftCommitteeDuties,
    IEnumerable<UpdateAppointMedianPriceCommitteeDto> MedianPriceCommittees,
    IEnumerable<UpdateDutiesDto> MedianPriceCommitteeDuties,
    IEnumerable<AcceptorAppointResponse> Acceptors);

public record UpdateAppointDto(
    DateTimeOffset MemorandumDate,
    string? MemorandumNumber,
    string? Telephone,
    string? Reason,
    AppointStatus Status,
    string? ChangeReason,
    string? CancelReason);

public record UpdateAppointTorDraftCommitteeDto(
    Guid? Id,
    Guid UserId,
    int Sequence,
    string CommitteePositionsCode);

public record UpdateAppointMedianPriceCommitteeDto(
    Guid? Id,
    Guid UserId,
    int Sequence,
    string CommitteePositionsCode);

public record UpdateDutiesDto(
    Guid? Id,
    int Sequence,
    string Description);

public class UpdateAppointRequestValidator : Validator<UpdateAppointRequest>
{
    public UpdateAppointRequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ไม่พบข้อมูลขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง");

        this.RuleFor(x => x.Appoint)
            .NotNull()
            .WithMessage("กรุณาระบุข้อมูล");

        this.RuleFor(x => x.Appoint.MemorandumDate)
            .NotEmpty()
            .WithMessage("กรุณาระบุวันที่เอกสารบันทึกข้อความแต่งตั้ง");

        this.RuleFor(x => x.Appoint.Status)
            .IsInEnum()
            .WithMessage("สถานะขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลางไม่ถูกต้อง");

        this.RuleFor(x => x.TorDraftCommittees)
            .NotEmpty()
            .WithMessage("จะต้องมีบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานอย่างน้อย 1 คน")
            .When(w => w.Appoint.Status is AppointStatus.WaitingApproval);

        this.RuleFor(x => x.TorDraftCommitteeDuties)
            .NotEmpty()
            .WithMessage("จะต้องมีอำนาจหน้าที่ของบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานอย่างน้อย 1 รายการ")
            .When(w => w.Appoint.Status is AppointStatus.WaitingApproval);

        this.RuleForEach(x => x.TorDraftCommittees).ChildRules(committee =>
        {
            committee.RuleFor(c => c.UserId)
                     .NotEmpty()
                     .WithMessage("กรุณาระบุผู้ใช้งาน");

            committee.RuleFor(c => c.CommitteePositionsCode)
                     .NotEmpty()
                     .WithMessage("กรุณาระบุตำแหน่งในคณะกรรมการ");

            committee.RuleFor(c => c.Sequence)
                     .GreaterThan(0)
                     .WithMessage("ลำดับต้องมากกว่าศูนย์");
        }).When(w => w.Appoint.Status is AppointStatus.WaitingApproval);

        this.RuleForEach(x => x.TorDraftCommitteeDuties).ChildRules(duty =>
        {
            duty.RuleFor(d => d.Description)
                .NotEmpty()
                .WithMessage("กรุณาระบุอำนาจหน้าที่ของบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน");

            duty.RuleFor(d => d.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่าศูนย์");
        }).When(w => w.Appoint.Status is AppointStatus.WaitingApproval);

        this.RuleFor(x => x.MedianPriceCommittees)
            .NotEmpty()
            .WithMessage("จะต้องมีบุคคล/คณะกรรมการกำหนดราคากลางอย่างน้อย 1 คน")
            .When(w => w.Appoint.Status is AppointStatus.WaitingApproval && w.Budget > 100000);

        this.RuleFor(x => x.MedianPriceCommitteeDuties)
            .NotEmpty()
            .WithMessage("จะต้องมีอำนาจหน้าที่ของบุคคล/คณะกรรมการกำหนดราคากลางอย่างน้อย 1 รายการ")
            .When(w => w.Appoint.Status is AppointStatus.WaitingApproval && w.Budget > 100000);

        this.RuleForEach(x => x.MedianPriceCommittees).ChildRules(committee =>
        {
            committee.RuleFor(c => c.UserId)
                     .NotEmpty()
                     .WithMessage("กรุณาระบุผู้ใช้งาน");

            committee.RuleFor(c => c.CommitteePositionsCode)
                     .NotEmpty()
                     .WithMessage("กรุณาระบุตำแหน่งในคณะกรรมการ");

            committee.RuleFor(c => c.Sequence)
                     .GreaterThan(0)
                     .WithMessage("ลำดับต้องมากกว่าศูนย์");
        }).When(w => w.Appoint.Status is AppointStatus.WaitingApproval && w.Budget > 100000);

        // Validate MedianPriceCommitteeDuties collection
        this.RuleForEach(x => x.MedianPriceCommitteeDuties).ChildRules(duty =>
        {
            duty.RuleFor(d => d.Description)
                .NotEmpty()
                .WithMessage("กรุณาระบุอำนาจหน้าที่ของบุคคล/คณะกรรมการกำหนดราคากลาง");

            duty.RuleFor(d => d.Sequence)
                .GreaterThan(0)
                .WithMessage("ลำดับต้องมากกว่าศูนย์");
        }).When(w => w.Appoint.Status is AppointStatus.WaitingApproval && w.Budget > 100000);

        this.RuleForEach(x => x.Acceptors).ChildRules(acceptor =>
        {
            acceptor.RuleFor(a => a.UserId)
                    .NotEmpty()
                    .WithMessage("กรุณาระบุผู้ใช้งาน");

            acceptor.RuleFor(a => a.EmployeeCode)
                    .NotEmpty()
                    .WithMessage("กรุณาระบุรหัสพนักงาน");

            acceptor.RuleFor(a => a.FullName)
                    .NotEmpty()
                    .WithMessage("กรุณาระบุชื่อ-นามสกุล");

            acceptor.RuleFor(a => a.AcceptorType)
                    .IsInEnum()
                    .WithMessage("ไม่พบประเภทของผู้มีอำนาจเห็นชอบ/อนุมัติ");

            acceptor.RuleFor(a => a.Sequence)
                    .GreaterThan(0)
                    .WithMessage("ลำดับต้องมากกว่าศูนย์");
        }).When(w => w.Appoint.Status is AppointStatus.WaitingApproval);
    }
}

public record UpdateAppointResponse(Guid? NewAppointDocumentFileId);

public class UpdateAppointEndpoint : AppointEndpointBase<UpdateAppointRequest, Results<Ok<UpdateAppointResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateAppointEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<UpdateAppointEndpoint> logger)
        : base(dbContext, logger, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Put("appointments/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdateAppointResponse>, NotFound<string>>> HandleRequestAsync(UpdateAppointRequest request, CancellationToken ct)
    {
        var appoint = await this.dbContext.PpAppoints
                                .Include(x => x.TorDraftCommittees)
                                .ThenInclude(ppAppointTorDraftCommittee => ppAppointTorDraftCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(c => c.TorDraftCommittees)
                                .ThenInclude(p => p.CommitteePositions)
                                .Include(x => x.TorDraftCommitteeDuties)
                                .Include(x => x.MedianPriceCommittees)
                                .ThenInclude(ppAppointMedianPriceCommittee => ppAppointMedianPriceCommittee.User)
                                .ThenInclude(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .Include(c => c.MedianPriceCommittees)
                                .ThenInclude(p => p.CommitteePositions)
                                .Include(x => x.MedianPriceCommitteeDuties)
                                .Include(x => x.Acceptors)
                                .Include(ppAppoint => ppAppoint.DocumentHistories)
                                .Include(auditableEntity => auditableEntity.AuditInfo)
                                .Include(ppAppoint => ppAppoint.Procurement)
                                .AsSplitQuery()
                                .FirstOrDefaultAsync(a => a.Id == PpAppointId.From(request.Id), ct);

        this.ValidateDocument(request, appoint);

        if (appoint == null)
        {
            return TypedResults.NotFound($"Appoint with ID {request.Id} not found.");
        }

        appoint.Update(
            request.Appoint.MemorandumDate,
            request.Appoint.MemorandumNumber,
            request.Appoint.Telephone,
            request.Appoint.Reason,
            request.Appoint.Status,
            request.Appoint.ChangeReason,
            request.Appoint.CancelReason);

        await this.UpdateTorAndMedian(
            appoint,
            request.TorDraftCommittees,
            request.TorDraftCommitteeDuties,
            request.MedianPriceCommittees,
            request.MedianPriceCommitteeDuties,
            ct);

        appoint.SetDocumentDate(request.Appoint.MemorandumDate);

        var acceptorUserIds = request.Acceptors.Select(a => UserId.From(a.UserId)).ToArray();

        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .ThenInclude(s => s.View)
                             .Where(u => acceptorUserIds.Contains(u.Id))
                             .ToArrayAsync(ct);

        UpdateAcceptors(appoint, request.Acceptors, request.Appoint.Status, user, UserId.From(request.UserId));

        var isReplaceRequest = request.IsAppointDocumentIdReplaced ?? false;

        var mustSaveDocument =
            request.AppointDocumentId.HasValue &&
            appoint.Status != AppointStatus.WaitingApproval &&
            isReplaceRequest;

        FileId? newAppointDocumentFileId = null;

        if (mustSaveDocument)
        {
            newAppointDocumentFileId = await this.UpdateDocumentHistoryAsync(
                appoint,
                FileId.From(request.AppointDocumentId!.Value),
                isReplaceRequest,
                ct);
        }

        if (appoint.DocumentHistories == null || !appoint.DocumentHistories.Any())
        {
            await this.SetDefaultDocumentTemplate(appoint, appoint.Procurement.SupplyMethodCode, appoint.Procurement.Budget, ct);
        }

        await this.dbContext.SaveChangesAsync(ct);

        await this.UpdateAndReplaceDocumentAsync(
            appoint,
            request,
            isReplaceRequest,
            ct);

        this.dbContext.PpAppoints.Update(appoint);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateAppointResponse(newAppointDocumentFileId?.Value));
    }

    private void ValidateDocument(UpdateAppointRequest request, PpAppoint? appoint)
    {
        if (request is { AppointDocumentId: not null, Appoint.Status: AppointStatus.WaitingApproval } &&
            (appoint != null && !appoint.IsMigration.GetValueOrDefault(false) && !appoint.DocumentHistories.Any()))
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private async ValueTask UpdateAndReplaceDocumentAsync(
        PpAppoint appoint,
        UpdateAppointRequest request,
        bool isReplace,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var operUser = await this.dbContext
                                 .SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(e => e.Id == UserId.From(request.UserId))
                                 .FirstOrDefaultAsync(ct);

        var lastedDraftDocument = appoint.LastedMaxDocument;

        if (lastedDraftDocument is not null)
        {
            var replaceDto =
                await this.MapToReplaceDto(appoint, ct, operUser);

            var templateFileId = isReplace
                ? await this.GetDocumentTemplateForReplace(appoint, ct)
                : lastedDraftDocument.FileId;

            var shouldCopy = isReplace || appoint.Status == AppointStatus.WaitingApproval;

            var newFileId = shouldCopy
                ? await documentService.CopyDocumentTemplateAsync(
                    templateFileId,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory: $"{DocumentTemplateGroups.Ap}/{appoint.AppointNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                    cancellationToken: ct)
                : templateFileId;

            if (newFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            appoint.AddDocumentHistory(newFileId.Value);
        }
    }

    private async Task<FileId> GetDocumentTemplateForReplace(
        PpAppoint appoint,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var isChange = appoint.IsChange;
        var isCancel = appoint.IsCancel;
        var budget = appoint.Procurement.Budget;

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.Ap &&
                dt.IsActive &&
                dt.SupplyMethodCode == appoint.Procurement.SupplyMethodCode &&
                (
                    (dt.IsChange == null && isChange == false) ||
                    dt.IsChange == isChange
                ) &&
                (
                    (dt.IsCancel == null && isCancel == false) ||
                    dt.IsCancel == isCancel
                ) &&
                dt.BudgetForDocument.Min <= budget &&
                (dt.BudgetForDocument.Max == null || budget <= dt.BudgetForDocument.Max),
            ct);

        if (templateFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.TemplateNotFoundForReset,
                StatusCodes.Status404NotFound);
        }

        return templateFileId.Value;
    }

    private async ValueTask UpdateTorAndMedian(
        PpAppoint appoint,
        IEnumerable<UpdateAppointTorDraftCommitteeDto> appointTorDraftCommittee,
        IEnumerable<UpdateDutiesDto> appointTorDraftCommitteeDuties,
        IEnumerable<UpdateAppointMedianPriceCommitteeDto> appointMedianPriceCommittee,
        IEnumerable<UpdateDutiesDto> appointMedianPriceCommitteeDuties,
        CancellationToken ct)
    {
        await this.DeleteRemovedTorDraftCommittees(
            appoint,
            appoint.Id,
            appointTorDraftCommittee,
            ct);

        await this.DeleteRemovedTorDraftCommitteeDuties(
            appoint.Id,
            appointTorDraftCommitteeDuties,
            ct);

        await this.DeleteRemovedMedianPriceCommittees(
            appoint,
            appoint.Id,
            appointMedianPriceCommittee,
            ct);

        await this.DeleteRemovedMedianPriceCommitteeDuties(
            appoint.Id,
            appointMedianPriceCommitteeDuties,
            ct);

        foreach (var dto in appointTorDraftCommittee)
        {
            await this.CreateOrUpdateAppointTorDraftCommittee(appoint, dto, ct);
        }

        foreach (var dto in appointTorDraftCommitteeDuties)
        {
            await this.CreateOrUpdateAppointTorDraftCommitteeDuties(appoint, dto, ct);
        }

        foreach (var dto in appointMedianPriceCommittee)
        {
            await this.CreateOrUpdateAppointMedianPriceCommittee(appoint, dto, ct);
        }

        foreach (var dto in appointMedianPriceCommitteeDuties)
        {
            await this.CreateOrUpdateAppointMedianPriceCommitteeDuties(appoint, dto, ct);
        }
    }

    private async ValueTask CreateOrUpdateAppointTorDraftCommittee(
        PpAppoint appoint,
        UpdateAppointTorDraftCommitteeDto appointTorDraftCommittee,
        CancellationToken ct)
    {
        if (appointTorDraftCommittee.Id != null)
        {
            var existingCommittee = await this.dbContext.PpAppointTorDraftCommittees
                                              .FirstOrDefaultAsync(
                                                  c =>
                                                      c.Id == PpAppointTorDraftCommitteeId.From(appointTorDraftCommittee.Id.Value),
                                                  ct);

            if (existingCommittee == null)
            {
                this.ThrowError($"ไม่พบข้อมูลบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน", StatusCodes.Status404NotFound);
            }

            existingCommittee
                .UpdateSequence(appointTorDraftCommittee.Sequence)
                .UpdatePositionCode(ParameterCode.From(appointTorDraftCommittee.CommitteePositionsCode));

            return;
        }

        if (appointTorDraftCommittee.Id == null)
        {
            var newCommittee = await this.CreateTorDraftCommittee(
                appoint.Id,
                appointTorDraftCommittee.UserId,
                appointTorDraftCommittee.CommitteePositionsCode,
                appointTorDraftCommittee.Sequence,
                ct);

            this.dbContext.PpAppointTorDraftCommittees.Add(newCommittee);
            appoint.AddPpAppointTorDraftCommittee(newCommittee);
        }
    }

    private async ValueTask CreateOrUpdateAppointTorDraftCommitteeDuties(
        PpAppoint appoint,
        UpdateDutiesDto appointTorDraftCommitteeDuties,
        CancellationToken ct)
    {
        if (appointTorDraftCommitteeDuties.Id != null)
        {
            var existingDuties = await this.dbContext.PpAppointTorDraftCommitteeDuties
                                           .FirstOrDefaultAsync(
                                               d =>
                                                   d.Id == PpAppointTorDraftCommitteeDutiesId.From(appointTorDraftCommitteeDuties.Id.Value),
                                               ct);

            if (existingDuties == null)
            {
                this.ThrowError($"ไม่พบข้อมูลอำนาจหน้าที่ของบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน", StatusCodes.Status404NotFound);
            }

            existingDuties.UpdateDescription(appointTorDraftCommitteeDuties.Description);
            existingDuties.UpdateSequence(appointTorDraftCommitteeDuties.Sequence);

            return;
        }

        if (appointTorDraftCommitteeDuties.Id == null)
        {
            var duties = CreateTorDraftCommitteeDuties(
                appoint.Id,
                appointTorDraftCommitteeDuties.Description,
                appointTorDraftCommitteeDuties.Sequence);

            this.dbContext.PpAppointTorDraftCommitteeDuties.Add(duties);
            appoint.AddPpAppointTorDraftCommitteeDuties(duties);
        }
    }

    private async ValueTask CreateOrUpdateAppointMedianPriceCommittee(
        PpAppoint appoint,
        UpdateAppointMedianPriceCommitteeDto appointMedianPriceCommittee,
        CancellationToken ct)
    {
        if (appointMedianPriceCommittee.Id != null)
        {
            var existingCommittee = await this.dbContext.PpAppointMedianPriceCommittees
                                              .FirstOrDefaultAsync(
                                                  c =>
                                                      c.Id == PpAppointMedianPriceCommitteeId.From(appointMedianPriceCommittee.Id.Value),
                                                  ct);

            if (existingCommittee == null)
            {
                this.ThrowError($"ไม่พบข้อมูลบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน", StatusCodes.Status404NotFound);
            }

            existingCommittee.UpdatePositionCode(ParameterCode.From(appointMedianPriceCommittee.CommitteePositionsCode));

            return;
        }

        if (appointMedianPriceCommittee.Id == null)
        {
            var newCommittee = await this.CreateMedianPriceCommittee(
                appoint.Id,
                appointMedianPriceCommittee.UserId,
                appointMedianPriceCommittee.CommitteePositionsCode,
                appointMedianPriceCommittee.Sequence,
                ct);

            this.dbContext.PpAppointMedianPriceCommittees.Add(newCommittee);
            appoint.AddPpAppointMedianPriceCommittee(newCommittee);
        }
    }

    private async ValueTask CreateOrUpdateAppointMedianPriceCommitteeDuties(
        PpAppoint appoint,
        UpdateDutiesDto appointMedianPriceCommitteeDuties,
        CancellationToken ct)
    {
        if (appointMedianPriceCommitteeDuties.Id != null)
        {
            var existingDuties = await this.dbContext.PpAppointMedianPriceCommitteeDuties
                                           .FirstOrDefaultAsync(
                                               d =>
                                                   d.Id == PpAppointMedianPriceCommitteeDutiesId.From(appointMedianPriceCommitteeDuties.Id.Value),
                                               ct);

            if (existingDuties == null)
            {
                this.ThrowError($"ไม่พบข้อมูลอำนาจหน้าที่ของบุคคล/คณะกรรมการกำหนดราคากลาง", StatusCodes.Status404NotFound);
            }

            existingDuties.UpdateDescription(appointMedianPriceCommitteeDuties.Description);
            existingDuties.UpdateSequence(appointMedianPriceCommitteeDuties.Sequence);

            return;
        }

        if (appointMedianPriceCommitteeDuties.Id == null)
        {
            var duties = CreateMedianPriceCommitteeDuties(
                appoint.Id,
                appointMedianPriceCommitteeDuties.Description,
                appointMedianPriceCommitteeDuties.Sequence);

            this.dbContext.PpAppointMedianPriceCommitteeDuties.Add(duties);
            appoint.AddPpAppointMedianPriceCommitteeDuties(duties);
        }
    }

    private async ValueTask DeleteRemovedTorDraftCommittees(
        PpAppoint appoint,
        PpAppointId appointId,
        IEnumerable<UpdateAppointTorDraftCommitteeDto> incomingCommittees,
        CancellationToken ct)
    {
        var incomingIds = incomingCommittees
                          .Where(dto => dto.Id.HasValue)
                          .Select(dto => PpAppointTorDraftCommitteeId.From(dto.Id.Value))
                          .ToList();

        var existingCommittees = await this.dbContext.PpAppointTorDraftCommittees
                                           .Where(c => c.PpAppointId == appointId)
                                           .ToListAsync(ct);

        var committeesToDeleteIds = existingCommittees
                                    .Where(existing => !incomingIds.Contains(existing.Id))
                                    .Select(s => s.Id)
                                    .ToList();

        committeesToDeleteIds.Iter(t => appoint.RemoveTorDraftCommittee(t));
    }

    private async ValueTask DeleteRemovedTorDraftCommitteeDuties(
        PpAppointId appointId,
        IEnumerable<UpdateDutiesDto> incomingDuties,
        CancellationToken ct)
    {
        var incomingIds = incomingDuties
                          .Where(dto => dto.Id.HasValue)
                          .Select(dto => PpAppointTorDraftCommitteeDutiesId.From(dto.Id.Value))
                          .ToList();

        var existingDuties = await this.dbContext.PpAppointTorDraftCommitteeDuties
                                       .Where(d => d.PpAppointId == appointId)
                                       .ToListAsync(ct);

        var dutiesToDelete = existingDuties
                             .Where(existing => !incomingIds.Contains(existing.Id))
                             .ToList();

        this.dbContext.PpAppointTorDraftCommitteeDuties.RemoveRange(dutiesToDelete);
    }

    private async ValueTask DeleteRemovedMedianPriceCommittees(
        PpAppoint appoint,
        PpAppointId appointId,
        IEnumerable<UpdateAppointMedianPriceCommitteeDto> incomingCommittees,
        CancellationToken ct)
    {
        var incomingIds = incomingCommittees
                          .Where(dto => dto.Id.HasValue)
                          .Select(dto => PpAppointMedianPriceCommitteeId.From(dto.Id.Value))
                          .ToList();

        var existingCommittees = await this.dbContext.PpAppointMedianPriceCommittees
                                           .Where(c => c.PpAppointId == appointId)
                                           .ToListAsync(ct);

        var committeesToDeleteIds = existingCommittees
                                    .Where(existing => !incomingIds.Contains(existing.Id))
                                    .Select(s => s.Id)
                                    .ToList();

        committeesToDeleteIds.Iter(t => appoint.RemoveMedianPriceCommittee(t));
    }

    private async ValueTask DeleteRemovedMedianPriceCommitteeDuties(
        PpAppointId appointId,
        IEnumerable<UpdateDutiesDto> incomingDuties,
        CancellationToken ct)
    {
        var incomingIds = incomingDuties
                          .Where(dto => dto.Id.HasValue)
                          .Select(dto => PpAppointMedianPriceCommitteeDutiesId.From(dto.Id.Value))
                          .ToList();

        var existingDuties = await this.dbContext.PpAppointMedianPriceCommitteeDuties
                                       .Where(d => d.PpAppointId == appointId)
                                       .ToListAsync(ct);

        var dutiesToDelete = existingDuties
                             .Where(existing => !incomingIds.Contains(existing.Id))
                             .ToList();

        this.dbContext.PpAppointMedianPriceCommitteeDuties.RemoveRange(dutiesToDelete);
    }

    private static void UpdateAcceptors(PpAppoint entity, IEnumerable<AcceptorAppointResponse>? acceptors, AppointStatus status, SuUser[] users, UserId userId)
    {
        if (acceptors == null)
        {
            return;
        }

        // Remove unmatched acceptors
        var toRemove = entity.Acceptors.Where(x => acceptors
                                                   .Where(w => w.Id.HasValue)
                                                   .All(r => x.Id != AcceptorId.From(r.Id.Value))).ToList();

        foreach (var item in toRemove)
        {
            entity.RemoveAcceptorById(item.Id);
        }

        // Add new acceptors.
        _ = acceptors.Where(w => !w.Id.HasValue)
                     .Join(
                         users,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => new { a, u })
                     .Iter(i =>
                     {
                         var newData = PpAppointAcceptors.Create(
                             entity.Id,
                             new AcceptorAppointInfoData(
                                 i.a.AcceptorType,
                                 i.u.Id,
                                 i.u.EmployeeCode,
                                 i.u.Employee.View?.FullName ?? string.Empty,
                                 i.u.Employee.ConvertPositionName(entity.Procurement.DepartmentId),
                                 i.u.Employee.View?.BusinessUnitName ?? string.Empty,
                                 i.a.Sequence),
                             status);

                         newData.SetSendToAcceptorId(userId);
                         entity.AddPpAppointAcceptor(newData);
                     });

        _ = entity.Acceptors
                  .Join(
                      acceptors.Where(w => w.Id.HasValue).ToList(),
                      db => db.Id,
                      payload => AcceptorId.From(payload.Id.Value),
                      (db, payload) => new { db, payload })
                  .Iter(p =>
                  {
                      var acceptorStatus =
                          (status == AppointStatus.WaitingApproval)
                              ? AcceptorStatus.Pending
                              : p.payload.Status;

                      var positionName =
                          !p.db.User.IsNull()
                              ? p.db.User.Employee.ConvertPositionName(entity.Procurement.DepartmentId)
                              : p.payload.PositionName;

                      p.db.Update(
                          new AcceptorAppointInfoData(
                              p.payload.AcceptorType,
                              p.db.UserId,
                              p.db.EmployeeCode,
                              p.db.FullName,
                              positionName,
                              p.db.BusinessUnitName ?? string.Empty,
                              p.payload.Sequence),
                          acceptorStatus);

                      p.db.SetSendToAcceptorId(userId);
                  });

        if (status == AppointStatus.WaitingApproval)
        {
            var acceptorsWithDelegator = entity.Acceptors
                                               .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                                               .Select(DelegatorExtensions.DelegatorToAcceptor)
                                               .OrderBy(a => a.Sequence)
                                               .ToList();

            var first = acceptorsWithDelegator.FirstOrDefault();

            if (first != null)
            {
                foreach (var a in entity.Acceptors)
                {
                    a.SetCurrent(a.Id == first.Id);
                }

                foreach (var targetUserId in first.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        entity,
                        targetUserId,
                        NotificationConstant.WaitForLike.Title,
                        string.Format(
                            NotificationConstant.WaitForLike.Message,
                            ProgramConstant.PreProcurementAppointment.Name,
                            entity.AppointNumber));
                }
            }
        }
    }

    private static async Task SendNotificationAsync(PpAppoint appoint, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(appoint.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Procurement.Url, appoint.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}