namespace GHB.DP2.Application.Features.Procurement.Jp005.Abstract;

using System.ComponentModel;
using System.Linq;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record EvaluationDto(
    [property: Description("ระยะเวลาในการพิจารณาผลการเสนอราคา")]
    int EvaluationDueDate,
    [property: Description("รหัสประเภทระยะเวลา")]
    string EvaluationPeriodTypeCode,
    [property: Description("รหัสเงื่อนไขระยะเวลา")]
    string EvaluationPeriodConditionCode,
    [property: Description("เลขโครงการ eGP")]
    string? EgpProjectNumber);

public record CommitteeSectionDto(
    [property: Description("คณะกรรมการ")] IEnumerable<CommitteeDto> Committees,
    [property: Description("อำนาจหน้าที่")]
    IEnumerable<DutyDto> Duties,
    [property: Description("เป็นคณะกรรมการ")]
    bool IsCommittee = true);

public record ProcurementSuppliesDivisionDto(
    Guid? Id,
    Guid UserId,
    string FullName,
    string FullPositionName,
    int Sequence);

public record CommitteeDto(
    [property: Description("รหัสคณะกรรมการ")]
    Guid? Id,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ชื่อตำแหน่งเต็ม")]
    string FullPositionName,
    [property: Description("รหัสตำแหน่งคณะกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ลำดับ")] int Sequence);

public record DutyDto(
    [property: Description("รหัสอำนาจหน้าที่")]
    Guid? Id,
    [property: Description("รายละเอียดอำนาจหน้าที่")]
    string Description,
    [property: Description("ลำดับ")] int Sequence);

public record AcceptorDto(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid? Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType Type,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string BusinessUnitName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัสผู้รับมอบอำนาจ")]
    Guid? DelegateId,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt,
    [property: Description("หมายเหตุ")] string? Remark
);

public class EvaluationValidator : Validator<EvaluationDto>
{
    public EvaluationValidator()
    {
        this.RuleFor(x => x.EvaluationDueDate)
            .GreaterThan(0)
            .WithMessage("ระยะเวลาในการพิจารณาผลการเสนอราคาให้แล้วเสร็จภายใน ต้องมากกว่า 0");

        this.RuleFor(x => x.EvaluationPeriodTypeCode)
            .NotEmpty()
            .WithMessage("จำเป็นต้องระบุ หน่วยของระยะเวลาในการพิจารณาผลการเสนอราคาให้แล้วเสร็จภายใน");

        this.RuleFor(x => x.EvaluationPeriodConditionCode)
            .NotEmpty()
            .WithMessage("จำเป็นต้องระบุ เงื่อนไขของระยะเวลาในการพิจารณาผลการเสนอราคาให้แล้วเสร็จภายใน");
    }
}

public class CommitteeSectionDtoValidator : Validator<CommitteeSectionDto>
{
    public CommitteeSectionDtoValidator(string roleLabel)
    {
        this.RuleFor(x => x.Committees)
            .NotNull()
            .WithMessage($"คณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage($"คณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.Duties)
            .NotNull()
            .WithMessage($"อำนาจหน้าที่ของคณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง")
            .NotEmpty()
            .WithMessage($"อำนาจหน้าที่ของคณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง");

        this.RuleForEach(x => x.Committees)
            .SetValidator(new CommitteeDtoValidator(roleLabel));

        this.RuleForEach(x => x.Duties)
            .SetValidator(new DutyDtoValidator(roleLabel));
    }
}

public class AcceptorValidator : Validator<AcceptorDto>
{
    public AcceptorValidator()
    {
        this.RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("ประเภทผู้มีอำนาจต้องเป็นค่าที่ถูกต้อง")
            .Must(x => x == AcceptorType.Approver)
            .WithMessage("ประเภทผู้มีอำนาจต้องเป็น 'ผู้มีอำนาจเห็นชอบ' เท่านั้น");

        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId ของผูู้มีอำนาจต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("ชื่อ-นามสกุลของผูู้มีอำนาจต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.PositionName)
            .NotEmpty()
            .WithMessage("ตำแหน่งของผูู้มีอำนาจต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.BusinessUnitName)
            .NotEmpty()
            .WithMessage("หน่วยงานของผู้มีอำนาจต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage("ลำดับของผู้มีอำนาจต้องมากกว่า 0");
    }
}

public class CommitteeDtoValidator : Validator<CommitteeDto>
{
    public CommitteeDtoValidator(string roleLabel)
    {
        this.RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage($"UserId ของคณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage($"ชื่อ-นามสกุลของคณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.FullPositionName)
            .NotEmpty()
            .WithMessage($"ตำแหน่งของคณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.CommitteePositionsCode)
            .NotEmpty()
            .WithMessage($"รหัสตำแหน่งของคณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง");
    }
}

public class DutyDtoValidator : Validator<DutyDto>
{
    public DutyDtoValidator(string roleLabel)
    {
        this.RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage($"รายละเอียดอำนาจหน้าที่ของคณะกรรมการ{roleLabel}ต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage($"ลำดับของอำนาจหน้าที่คณะกรรมการ{roleLabel}ต้องมากกว่า 0");
    }
}

public abstract partial class Jp005EndpointBase<TRequest, TResponse>
    : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;

    protected Jp005EndpointBase(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger logger)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.commandTextService = commandTextService;
    }

    protected GetById.Jp005Response GetJp005Response(PJp005 jp005)
    {
        var procurementCommittees = MapCommitteeByType(
            [.. jp005.Committees],
            [.. jp005.CommitteeDuties],
            PJp005CommitteeGroupType.ProcurementCommittee);

        var inspectionCommittees = MapCommitteeByType(
            [.. jp005.Committees],
            [.. jp005.CommitteeDuties],
            PJp005CommitteeGroupType.InspectionCommittee);

        var maintenanceInspectionCommittees = MapCommitteeByType(
            [.. jp005.Committees],
            [.. jp005.CommitteeDuties],
            PJp005CommitteeGroupType.MaintenanceInspectionCommittee);

        var constructionSupervisors = MapCommitteeByType(
            [.. jp005.Committees],
            [.. jp005.CommitteeDuties],
            PJp005CommitteeGroupType.ConstructionSupervisor);

        var acceptors =
            MapAcceptors([.. jp005.Acceptors]);

        var lastedApprovalHistory = jp005.DocumentHistories
                                         .Where(d => d.DocumentType == PJp005DocumentType.Approval)
                                         .OrderVersions()
                                         .FirstOrDefault();

        var lastedCommandHistory = jp005.DocumentHistories
                                        .Where(d => d.DocumentType == PJp005DocumentType.Command)
                                        .OrderVersions()
                                        .FirstOrDefault();

        var procurementSuppliesDivision =
            jp005.ProcurementSuppliesDivisions.Select(s =>
                new ProcurementSuppliesDivisionDto(
                    s.Id.Value,
                    s.SuUserId.Value,
                    s.FullName,
                    s.FullPositionName,
                    s.Sequence));

        var approvalDocumentVersions =
            jp005.DocumentHistories
                 .Where(d => d.DocumentType == PJp005DocumentType.Approval)
                 .OrderVersions()
                 .Select((d, index) => new Jp005DocumentVersionResponse(
                     d.FileId.Value,
                     d.Version,
                     d.CreatedAt,
                     d.CreatedByName ?? string.Empty,
                     index == 0))
                 .ToArray();

        var commandDocumentVersions =
            jp005.DocumentHistories
                 .Where(d => d.DocumentType == PJp005DocumentType.Command)
                 .OrderVersions()
                 .Select((d, index) => new Jp005DocumentVersionResponse(
                     d.FileId.Value,
                     d.Version,
                     d.CreatedAt,
                     d.CreatedByName ?? string.Empty,
                     index == 0))
                 .ToArray();

        return new GetById.Jp005Response(
            jp005.EvaluationDueDate,
            jp005.EvaluationPeriodTypeCode,
            jp005.EvaluationPeriodConditionCode,
            jp005.EgpProjectNumber,
            lastedApprovalHistory?.FileId.Value,
            false,
            lastedCommandHistory?.FileId.Value,
            false,
            procurementCommittees,
            inspectionCommittees,
            maintenanceInspectionCommittees.Committees.Any(),
            maintenanceInspectionCommittees,
            constructionSupervisors.Committees.Any(),
            constructionSupervisors,
            acceptors,
            [.. procurementSuppliesDivision],
            approvalDocumentVersions,
            commandDocumentVersions);
    }

    protected GetById.Jp004Response GetPurchaseRequisitionResponse(PpPurchaseRequisition jp004)
    {
        var requisition =
            new GetById.GetPurchaseRequisition(
                (string?)jp004.PurchaseRequisitionNumber,
                jp004.EgpNumber,
                jp004.PrNumber,
                jp004.Description,
                jp004.PriceReasonablenessInfo,
                jp004.MedianPriceAmount,
                jp004.EvaluationCriteriaCode.ToString(),
                jp004.DeliveryPeriod,
                jp004.DeliveryPeriodTypeCode.ToString(),
                jp004.DeliveryConditionCode.ToString(),
                jp004.HasFineRate,
                jp004.HasWarranty,
                jp004.WarrantyPeriod,
                jp004.WarrantyPeriodCode.ToString(),
                jp004.WarrantyConditionCode.ToString(),
                jp004.HasContractGuarantee,
                jp004.HasInspectionCommittee,
                jp004.HasConstructionSupervisor,
                jp004.Telephone);

        var budgets =
            jp004.Budgets.Select(b =>
                new GetById.GetPurchaseRequisitionBudget(
                    b.Id.Value,
                    b.Description,
                    b.BudgetAmount,
                    b.PpPurchaseRequisitionBudgetDetails
                     .OrderBy(s => s.Sequence)
                     .Select(d =>
                         new GetById.GetPurchaseRequisitionBudgetDetail(
                             d.Id.Value,
                             d.Sequence,
                             d.Department,
                             d.BudgetTypeCode.Value,
                             d.ProjectCode?.ToString(),
                             d.AccountNoCode.ToString(),
                             d.Budget)),
                    b.Sequence));

        var warranties =
            jp004.Warranties.Select(w =>
                new GetById.GetPurchaseRequisitionWarranty(
                    w.Id.Value,
                    w.HasWarranty,
                    w.Period,
                    w.PeriodTypeCode?.ToString(),
                    w.ConditionOther));

        var committees =
            jp004.Committees
                 .Where(x => x.GroupType == GroupType.ProcurementCommittee || x.GroupType == GroupType.InspectionCommittee || x.GroupType == GroupType.MaintenanceInspectionCommittee ||
                             x.GroupType == GroupType.ConstructionSupervisor)
                 .Select(c =>
                     new GetById.GetPurchaseRequisitionCommittee(
                         c.Id.Value,
                         c.GroupType,
                         c.SuUserId.Value,
                         c.FullName,
                         c.User.Employee.View?.FullPositionName ?? string.Empty,
                         c.CommitteePositionsCode.Value,
                         c.CommitteePositionsName,
                         c.Sequence))
                 .OrderBy(x => x.Sequence)
                 .ToList();

        var operators =
            committees.Where(c => c.GroupType == GroupType.ProcurementCommittee)
                      .Select(c => new GetById.Jp004Operator(
                          UserId.From(c.SuUserId),
                          GetById.Jp004OperatorType.ProcurementCommittee,
                          c.Sequence))
                      .ToList();

        var scopeOfWork =
            jp004.TechnicalSpecifications
                 .Select(s => new GetById.GetScopeOfWork(
                     s.Id.Value,
                     s.Sequence,
                     s.Name,
                     s.Description,
                     s.Quantity,
                     s.UnitCode.HasValue ? (string)s.UnitCode : string.Empty))
                 .OrderBy(s => s.Sequence);

        if (jp004.LastedAssignee is not null)
        {
            operators.AddRange(jp004.Assignees.Where(a => a.Type == AssigneeType.Assignee)
                                    .OrderBy(x => x.Sequence)
                                    .Select(c => new GetById.Jp004Operator(
                                        UserId.From((Guid)c.UserId),
                                        GetById.Jp004OperatorType.Assignee,
                                        c.Sequence)));
        }

        return
            new GetById.Jp004Response(
                jp004.Id,
                requisition,
                budgets,
                warranties,
                committees,
                scopeOfWork,
                operators,
                jp004.Committees
                     .Where(w => w.GroupType is GroupType.ProcurementCommittee)
                     .All(a => a.IsCommittee()),
                jp004.Committees
                     .Where(w => w.GroupType is GroupType.InspectionCommittee)
                     .All(a => a.IsCommittee()));
    }

    private static IEnumerable<AcceptorResponse> MapAcceptors(PJp005Acceptors[] acceptors)
    {
        return
            acceptors
                .Select(DelegatorExtensions.DelegatorToAcceptor)
                .OrderBy(o => o.Sequence)
                .Select(acceptor =>
                    new AcceptorResponse(
                        acceptor.Id.Value,
                        acceptor.Type,
                        acceptor.UserId.Value,
                        acceptor.Sequence,
                        acceptor.FullName,
                        acceptor.PositionName,
                        acceptor.BusinessUnitName,
                        acceptor.Status,
                        acceptor.Remark,
                        acceptor.ActionAt,
                        DelegateId: acceptor.DelegateeId?.Value,
                        IsCurrent: acceptor.IsCurrentApprover(),
                        DelegateeUserId: acceptor.Delegatee?.SuUserId.Value));
    }

    private static CommitteeSectionDto MapCommitteeByType(
        PJp005Committee[] committees,
        PJp005CommitteeDuties[] duties,
        PJp005CommitteeGroupType groupType)
    {
        return groupType switch
        {
            PJp005CommitteeGroupType.ProcurementCommittee =>
                new CommitteeSectionDto(
                    committees.Where(c =>
                                  c.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                              .OrderBy(r => r.Sequence)
                              .Select(c => new CommitteeDto(
                                  c.Id.Value,
                                  c.SuUserId.Value,
                                  c.FullName,
                                  c.FullPositionName,
                                  (string)c.CommitteePositionsCode,
                                  c.Sequence))
                              .ToArray(),
                    duties.Where(c =>
                              c.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                          .OrderBy(o => o.Sequence)
                          .Select(c => new DutyDto(
                              c.Id.Value,
                              c.Description,
                              c.Sequence))
                          .ToArray(),
                    committees.Where(c => c.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                              .All(s => s.IsCommittee())),
            PJp005CommitteeGroupType.InspectionCommittee =>
                new CommitteeSectionDto(
                    committees.Where(c => c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                              .OrderBy(r => r.Sequence)
                              .Select(c => new CommitteeDto(
                                  c.Id.Value,
                                  c.SuUserId.Value,
                                  c.FullName,
                                  c.FullPositionName,
                                  (string)c.CommitteePositionsCode,
                                  c.Sequence))
                              .ToArray(),
                    duties.Where(c => c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                          .OrderBy(o => o.Sequence)
                          .Select(c => new DutyDto(
                              c.Id.Value,
                              c.Description,
                              c.Sequence))
                          .ToArray(),
                    committees.Where(c => c.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                              .All(s => s.IsCommittee())),
            PJp005CommitteeGroupType.MaintenanceInspectionCommittee =>
                new CommitteeSectionDto(
                    committees.Where(c => c.GroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee)
                              .OrderBy(r => r.Sequence)
                              .Select(c => new CommitteeDto(
                                  c.Id.Value,
                                  c.SuUserId.Value,
                                  c.FullName,
                                  c.FullPositionName,
                                  (string)c.CommitteePositionsCode,
                                  c.Sequence))
                              .ToArray(),
                    duties.Where(c => c.GroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee)
                          .OrderBy(o => o.Sequence)
                          .Select(c => new DutyDto(
                              c.Id.Value,
                              c.Description,
                              c.Sequence))
                          .ToArray(),
                    committees.Where(c => c.GroupType == PJp005CommitteeGroupType.MaintenanceInspectionCommittee)
                              .All(s => s.IsCommittee())),
            PJp005CommitteeGroupType.ConstructionSupervisor =>
                new CommitteeSectionDto(
                    committees.Where(c => c.GroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
                              .OrderBy(r => r.Sequence)
                              .Select(c => new CommitteeDto(
                                  c.Id.Value,
                                  c.SuUserId.Value,
                                  c.FullName,
                                  c.FullPositionName,
                                  (string)c.CommitteePositionsCode,
                                  c.Sequence))
                              .ToArray(),
                    duties.Where(c => c.GroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
                          .OrderBy(o => o.Sequence)
                          .Select(c => new DutyDto(
                              c.Id.Value,
                              c.Description,
                              c.Sequence))
                          .ToArray(),
                    committees.Where(c => c.GroupType == PJp005CommitteeGroupType.ConstructionSupervisor)
                              .All(s => s.IsCommittee())),
            _ => throw new NotImplementedException("ไม่พบประเภทคณะกรรมการ"),
        };
    }

    private async Task<FileId> GetDocumentTemplateByCriteria(
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Jp05 &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    dt.AdditionalInfo == null &&
                    dt.IsActive,
                ct);

        if (fileId is null)
        {
            this.ThrowError(
                "ไม่พบเทมเพลตเอกสารแผนที่ตรงกับเงื่อนไข",
                StatusCodes.Status404NotFound);
        }

        return fileId.Value;
    }

    private async Task<FileId> GetAppointmentOrderedDocumentTemplateByCriteria(
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Jp05 &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    dt.AdditionalInfo != null &&
                    dt.AdditionalInfo.RootElement
                      .GetProperty(nameof(SuDocumentTemplate.IsAppointmentOrdered))
                      .GetBoolean() &&
                    dt.IsActive,
                ct);

        if (fileId is null)
        {
            this.ThrowError(
                "ไม่พบเทมเพลตเอกสารคำสั่งแต่งตั้งที่ตรงกับเงื่อนไข",
                StatusCodes.Status404NotFound);
        }

        return fileId.Value;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        PJp005 jp005Data,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var approvalDocId =
            await this.GetDocumentTemplateByCriteria(
                supplyMethodCode,
                ct);

        var createDocumentHistory =
            PJp005DocumentHistory.Create(
                PJp005DocumentType.Approval,
                jp005Data.Status,
                "1.0",
                approvalDocId);

        jp005Data.AddDocumentHistory(createDocumentHistory);

        if (supplyMethodCode == SupplyMethodConstant.Eighty)
        {
            var commandDocId =
                await this.GetAppointmentOrderedDocumentTemplateByCriteria(
                    supplyMethodCode,
                    ct);

            var createCommandDocumentHistory =
                PJp005DocumentHistory.Create(
                    PJp005DocumentType.Command,
                    jp005Data.DocumentHistories.Any() ? jp005Data.Status : PJp005Status.Draft,
                    "1.0",
                    commandDocId);

            jp005Data.AddDocumentHistory(createCommandDocumentHistory);
        }
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PJp005 jp005Data,
        PJp005DocumentType documentType,
        FileId fileId,
        bool? isReplaced = false,
        CancellationToken ct = default)
    {
        var latestHistory = jp005Data.DocumentHistories
                                     .Where(d => d.DocumentType == documentType)
                                     .OrderVersions()
                                     .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            jp005Data.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.Jp05}/{jp005Data.Id}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        var addNewHistory = PJp005DocumentHistory.Create(
            documentType,
            jp005Data.Status,
            newVersion,
            copiedFileId.Value,
            isReplaced ?? false);

        jp005Data.AddDocumentHistory(addNewHistory);

        return copiedFileId.Value;
    }

    protected async Task<FileId> GetDocumentTemplateForResetAsync(
        PJp005 jp005Data,
        PJp005DocumentType documentType,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        FileId? fileId;

        if (documentType == PJp005DocumentType.Approval)
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Jp05 &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    dt.AdditionalInfo == null &&
                    dt.IsActive,
                parentDirectory: $"{DocumentTemplateGroups.Jp05}/{jp005Data.Id}_{documentType}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);
        }
        else
        {
            fileId = await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.Jp05 &&
                    dt.SupplyMethodCode == supplyMethodCode &&
                    dt.AdditionalInfo != null &&
                    dt.AdditionalInfo.RootElement
                      .GetProperty(nameof(SuDocumentTemplate.IsAppointmentOrdered))
                      .GetBoolean() &&
                    dt.IsActive,
                parentDirectory: $"{DocumentTemplateGroups.Jp05}/{jp005Data.Id}_{documentType}_Reset_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);
        }

        if (fileId is null)
        {
            this.ThrowError(
                "ไม่พบเทมเพลตเอกสารที่ตรงกับเงื่อนไข",
                StatusCodes.Status404NotFound);
        }

        return fileId.Value;
    }

    protected async Task<Procurement> GetProcurementById(
        ProcurementId procurementId,
        CancellationToken ct)
    {
        var procurement =
            await this.dbContext.Procurements
                      .Include(p => p.Jp005)
                      .ThenInclude(jp005 => jp005.ProcurementSuppliesDivisions)
                      .Include(p => p.Jp005)
                      .ThenInclude(p => p.Acceptors)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .ThenInclude(e => e.View)
                      .FirstOrDefaultAsync(p => p.Id == procurementId, ct);

        if (procurement is null)
        {
            this.ThrowError(
                $"ไม่พบ Procurements ในระบบ",
                StatusCodes.Status404NotFound);
        }

        return procurement;
    }

    protected PJp005 GetJp005ById(
        IReadOnlyCollection<PJp005> jp005,
        PJp005Id id,
        ProcurementId procurementId)
    {
        var jp005Existing = jp005.FirstOrDefault(jp => jp.Id == id && jp.ProcurementId == procurementId);

        if (jp005Existing is null)
        {
            this.ThrowError(
                r =>
                    id,
                $"ไม่พบ จพ.005 ในระบบ",
                StatusCodes.Status404NotFound);
        }

        return jp005Existing;
    }

    protected void ValidateUsers(SuUser[] users, UserId[] userIds)
    {
        var foundUserIds =
            users.Select(u => u.Id)
                 .ToArray();

        var missingUserIds =
            userIds.Except(foundUserIds)
                   .ToArray();

        if (missingUserIds.Length > 0)
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้ใช้งาน {string.Join(", ", missingUserIds)}.",
                StatusCodes.Status404NotFound);
        }
    }

    protected string FindCommitteeNameByCode(SuParameter[] listOfValue, string code)
    {
        var position = listOfValue.FirstOrDefault(p => p.Code == ParameterCode.From(code));

        if (position is null)
        {
            this.ThrowError($"Position with code {code} not found.", StatusCodes.Status404NotFound);
        }

        return position.Label;
    }

    protected async Task<GetById.GetJp005ByIdResponse> GetJp005ByIdResponseAsync(
        PJp005 jp005Existing,
        Guid procurementId,
        Guid userId,
        CancellationToken ct)
    {
        var jp004Existing =
            await this.dbContext.PpPurchaseRequisitions
                      .Include(r => r.Budgets)
                      .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                      .Include(r => r.Warranties)
                      .Include(r => r.PaymentTerms)
                      .Include(r => r.FineRates)
                      .Include(r => r.Committees)
                      .ThenInclude(ppPurchaseRequisitionCommittee => ppPurchaseRequisitionCommittee.User)
                      .ThenInclude(suUser => suUser.Employee)
                      .ThenInclude(rawEmployee => rawEmployee.View)
                      .Include(r => r.Acceptors)
                      .Include(r => r.Assignees)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.TechnicalSpecifications)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.Department)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethod)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethodType)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                      .Include(ppPurchaseRequisition => ppPurchaseRequisition.Procurement)
                      .ThenInclude(procurement => procurement.Plan)
                      .AsNoTracking()
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(r => r.ProcurementId == ProcurementId.From(procurementId), ct);

        if (jp004Existing == null)
        {
            this.ThrowError(
                "ไม่พบข้อมูล จพ.004",
                StatusCodes.Status404NotFound);
        }

        var purchaseRequisition = this.GetPurchaseRequisitionResponse(jp004Existing);

        var hasEditPermission = purchaseRequisition.Operators
                                                   .Any(s => (Guid)s.UserId == userId);

        var torData = await this.dbContext.PpTorDrafts.Include(ppTorDraft => ppTorDraft.DocumentTemplate)
                                .FirstOrDefaultAsync(t => t.ProcurementId == jp005Existing.ProcurementId, ct);

        return new GetById.GetJp005ByIdResponse(
            ProcurementId.From(procurementId),
            new ProcurementDto(
                jp005Existing.Procurement.PlanId.HasValue ? (Guid)jp005Existing.Procurement.PlanId : null,
                jp005Existing.Procurement.ProcurementNumber,
                jp005Existing.Procurement.Type,
                jp005Existing.Procurement.Step,
                jp005Existing.Procurement.Department.Name,
                jp005Existing.Procurement.DepartmentId,
                jp005Existing.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                jp005Existing.Procurement.Name,
                jp005Existing.Procurement.Budget,
                jp005Existing.Procurement.Budget.ThaiBahtText(),
                jp005Existing.Procurement.BudgetYear,
                jp005Existing.Procurement.SupplyMethod.Label,
                jp005Existing.Procurement.SupplyMethodCode,
                jp005Existing.Procurement.SupplyMethodType?.Label ?? string.Empty,
                jp005Existing.Procurement.SupplyMethodTypeCode,
                jp005Existing.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                jp005Existing.Procurement.SupplyMethodSpecialTypeCode,
                jp005Existing.Procurement.Status,
                jp005Existing.Procurement.ExpectingProcurementAt,
                jp005Existing.Procurement.IsStock,
                jp005Existing.Procurement.IsCommercialMaterial,
                jp005Existing.Procurement.Plan?.Type,
                jp005Existing.Procurement.ProcessType),
            jp005Existing.Id,
            jp005Existing.PJp005Number,
            purchaseRequisition,
            this.GetJp005Response(jp005Existing),
            jp005Existing.Status,
            torData?.DocumentTemplate?.Code,
            jp005Existing.JorPorNumber,
            hasEditPermission);
    }
}