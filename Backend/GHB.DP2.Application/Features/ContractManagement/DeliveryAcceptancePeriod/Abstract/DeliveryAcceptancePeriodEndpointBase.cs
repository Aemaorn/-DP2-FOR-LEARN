namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptancePeriod.Abstract;

using Codehard.Common.Extensions;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Linq;

public record DeliveryDto(
    [property: Description("รหัสการส่งมอบ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("วันที่ส่งมอบ")]
    DateTimeOffset? DeliveryDate,
    [property: Description("ผลการพิจารณา")]
    string ConsiderationResult,
    [property: Description("รายการส่งมอบ")]
    IEnumerable<DeliveryItemDto> DeliveryItems);

public record DeliveryItemDto(
    [property: Description("รหัสรายการส่งมอบ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียด")] string Description,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("ราคาต่อหน่วย")]
    decimal Price,
    [property: Description("รวมราคา")] decimal Total);

public record PeriodAcceptanceInfoDto(
    [property: Description("วันที่ตรวจรับ")]
    DateTimeOffset? AcceptanceDate,
    [property: Description("เลขที่ตรวจรับ")]
    string? AcceptanceNumber,
    [property: Description("รายละเอียด")] string? Description,
    [property: Description("จำนวนเงินที่รับ")]
    decimal? AcceptedAmount,
    [property: Description("มีการหักเงิน")]
    bool HasDeduction);

public record AcceptanceDeductionItemDto(
    [property: Description("รหัสรายการหักเงิน")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียดการหักเงิน")]
    string Description,
    [property: Description("จำนวนเงิน")] decimal Amount);

public class DeliveryDtoValidator : Validator<DeliveryDto>
{
    public DeliveryDtoValidator()
    {
        this.RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage("ลำดับการส่งมอบต้องมากกว่า 0");

        this.RuleFor(x => x.DeliveryDate)
            .NotEmpty()
            .WithMessage("วันที่ส่งมอบต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.ConsiderationResult)
            .NotEmpty()
            .WithMessage("ผลการพิจารณาต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.DeliveryItems)
            .NotNull()
            .WithMessage("รายการส่งมอบต้องไม่เป็นค่าว่าง")
            .Must(items => items.Any())
            .WithMessage("ต้องมีอย่างน้อย 1 รายการส่งมอบ");

        this.RuleForEach(x => x.DeliveryItems)
            .SetValidator(new DeliveryItemDtoValidator());
    }
}

public class DeliveryItemDtoValidator : Validator<DeliveryItemDto>
{
    public DeliveryItemDtoValidator()
    {
        this.RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage("ลำดับในรายการส่งมอบพัสดุต้องมากกว่า 0");

        this.RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("รายละเอียดพัสดุต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("จำนวนต้องมากกว่า 0");

        this.RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("ราคาต่อหน่วยต้องมากกว่า 0");

        this.RuleFor(x => x.Total)
            .GreaterThan(0)
            .WithMessage("รวมราคาต้องมากกว่า 0");
    }
}

public class PeriodAcceptanceInfoDtoValidator : Validator<PeriodAcceptanceInfoDto>
{
    public PeriodAcceptanceInfoDtoValidator()
    {
        this.RuleFor(x => x.AcceptedAmount)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("จำนวนที่ตรวจรับต้องมากกว่า 0");
    }
}

public class AcceptanceDeductionItemValidator : Validator<AcceptanceDeductionItemDto>
{
    public AcceptanceDeductionItemValidator()
    {
        this.RuleFor(x => x.Sequence)
            .GreaterThan(0)
            .WithMessage("ลำดับต้องมากกว่า 0");

        this.RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("รายละเอียดการหักเงินต้องไม่เป็นค่าว่าง");

        this.RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("จำนวนเงินต้องมากกว่า 0");
    }
}

public abstract class DeliveryAcceptancePeriodEndpointBase<TRequest, TResponse>
    : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;

    protected DeliveryAcceptancePeriodEndpointBase(
        Dp2DbContext dbContext,
        ILogger logger,
        IOperationService operationService,
        ICommandTextService commandTextService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.commandTextService = commandTextService;
    }

    protected static AcceptorNoIdResponse[] MapAcceptorsForFindCase(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorType acceptorType)
    {
        var acceptorsApprover = periodExisting.Acceptors
                                              .Where(x => x.Type != AcceptorType.AcceptanceCommittee)
                                              .Select(DelegatorExtensions.DelegatorToAcceptor)
                                              .ToList();

        var committee = periodExisting.Acceptors
                                      .Where(x => x.Type == AcceptorType.AcceptanceCommittee)
                                      .ToList();

        var acceptors =
            acceptorsApprover
                .Union(committee)
                .ToArray();

        if (!acceptors.Any())
        {
            return new AcceptorNoIdResponse[0];
        }

        return
        [
            .. acceptors
               .Where(a => a.Type == acceptorType && a is { IsDeleted: false, IsActive: true })
               .OrderBy(a => a.Sequence)
               .Select(a => new AcceptorNoIdResponse(
                   a.Id.Value,
                   a.Type,
                   a.UserId.Value,
                   a.Sequence,
                   a.FullName,
                   a.PositionName,
                   a.BusinessUnitName,
                   a.Status,
                   a.Remark,
                   a.ActionAt,
                   (a.CommitteePositionsCode != null && a.CommitteePositionsCode.HasValue) ? (string)a.CommitteePositionsCode : string.Empty,
                   a.CommitteePosition?.Label,
                   DelegateId: a.DelegateeId?.Value,
                   IsUnableToPerformDuties: a.IsUnableToPerformDuties,
                   IsCurrent: a?.IsCurrentApprover() ?? false,
                   DelegateeUserId: a?.Delegatee?.SuUserId != null ? a.Delegatee?.SuUserId.Value : null,
                   DepartmentCode: a?.User.Employee.View?.BusinessUnitId.Value))
        ];
    }

    protected async Task<bool> HasJorPorAssign(
        CmDeliveryAcceptancePeriod periodExisting,
        Guid currentUserId,
        CancellationToken ct)
    {
        ParameterCode supplyMethodCode;
        ParameterCode? supplyMethodSpecialTypeCode;
        string? supplyMethodSpecialName = null;
        var isCommercialMaterial = false;
        string? contractDescription = null;
        string? contractName = null;

        if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Plan)
        {
            var planData = await this.dbContext.Plans
                                     .Include(p => p.SupplyMethodSpecialType)
                                     .FirstOrDefaultAsync(p => p.Id == PlanId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (planData is null)
            {
                this.ThrowError("ไม่พบข้อมูลแผน", StatusCodes.Status404NotFound);
            }

            supplyMethodCode = planData.SupplyMethodCode;
            supplyMethodSpecialTypeCode = planData.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = planData.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = planData.IsCommercialMaterial ?? false;
            contractName = planData.Name;
            contractDescription = "ใบสั่งซื้อ/จ้าง/เช่า เลขที่ .................................................. ลงวันที่ ..................................................";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
        {
            var contractDraftVendorData = await this.dbContext.CaContractDraftVendors
                                                    .Include(x => x.ContractDraft)
                                                    .ThenInclude(x => x.Procurement)
                                                    .ThenInclude(x => x.SupplyMethodSpecialType)
                                                    .FirstOrDefaultAsync(x => x.Id == ContractDraftVendorId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (contractDraftVendorData is null)
            {
                this.ThrowError("ไม่พบร่างสัญญาของผู้ขาย", StatusCodes.Status404NotFound);
            }

            var procurement = contractDraftVendorData.ContractDraft.Procurement;
            supplyMethodCode = procurement.SupplyMethodCode;
            supplyMethodSpecialTypeCode = procurement.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = procurement.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = procurement.IsCommercialMaterial;
            contractName = contractDraftVendorData.ContractName;
            contractDescription =
                $"สัญญาเลขที่จพ.(สบส.) {contractDraftVendorData.ContractNumber} ลงวันที่ {contractDraftVendorData.ContractSignedDate?.ToThaiDateString(includeBuddhistEra: false)} สัญญา {contractDraftVendorData.ContractName}";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
        {
            var poaData = await this.dbContext.PPurchaseOrderApprovals
                                    .Include(poa => poa.Procurement)
                                    .ThenInclude(p => p.SupplyMethodSpecialType)
                                    .Include(poa => poa.Contracts)
                                    .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (poaData is null)
            {
                this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า", StatusCodes.Status404NotFound);
            }

            var procurement = poaData.Procurement;
            supplyMethodCode = procurement.SupplyMethodCode;
            supplyMethodSpecialTypeCode = procurement.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = procurement.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = procurement.IsCommercialMaterial;
            var poContract = poaData.Contracts.FirstOrDefault();
            contractName = procurement.Name;
            contractDescription = $"ใบสั่งซื้อ/จ้าง/เช่า เลขที่ {poContract?.PoNumber} ลงวันที่ ................................................";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEditBase = await this.dbContext.CaContractDraftVendorEdits
                .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (vendorEditBase is null)
            {
                this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา", StatusCodes.Status404NotFound);
            }

            var cdvBase = await this.dbContext.CaContractDraftVendors
                .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                .FirstOrDefaultAsync(v => v.Id == vendorEditBase.ContractDraftVendorId, ct);

            if (cdvBase is null)
            {
                this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
            }

            var procurementBase = cdvBase.ContractDraft.Procurement;
            supplyMethodCode = procurementBase.SupplyMethodCode;
            supplyMethodSpecialTypeCode = procurementBase.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = procurementBase.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = procurementBase.IsCommercialMaterial;
            contractName = vendorEditBase.ContractName;
            contractDescription =
                $"สัญญาเลขที่จพ.(สบส.) {vendorEditBase.ContractNumber} ลงวันที่ {vendorEditBase.ContractSignedDate?.ToThaiDateString(includeBuddhistEra: false)} สัญญา {vendorEditBase.ContractName}";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Manual)
        {
            var delivery = await this.dbContext.CmDeliveryAcceptances
                .Include(d => d.SupplyMethodSpecialType)
                .FirstOrDefaultAsync(d => d.Id == periodExisting.CmDeliveryAcceptanceId, ct);

            if (delivery is null)
            {
                this.ThrowError("ไม่พบข้อมูลการตรวจรับพัสดุ", StatusCodes.Status404NotFound);
            }

            if (delivery.SupplyMethodCode is null)
            {
                this.ThrowError("ไม่พบข้อมูลวิธีการจัดซื้อจัดจ้าง", StatusCodes.Status404NotFound);
            }

            supplyMethodCode = (ParameterCode)delivery.SupplyMethodCode!;
            supplyMethodSpecialTypeCode = delivery.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = delivery.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = delivery.IsCommercialMaterial ?? false;
            contractName = delivery.Name;
            contractDescription = "...............................................................................................";
        }
        else
        {
            throw new InvalidOperationException("SourceType ไม่ถูกต้อง");
        }

        var processType = SectionProcessType.DeliveryAcceptancePeriod;

        if (periodExisting.HasDeduction)
        {
            processType = SectionProcessType.DeliveryAcceptancePeriodPenalty;
        }
        else if (supplyMethodCode.Value == SupplyMethodConstant.Eighty && isCommercialMaterial)
        {
            processType = SectionProcessType.DeliveryAcceptancePeriodCommercialParcel;
        }

        var defaultAcceptors = await this.operationService.GetDefaultAcceptorAsync(
            processType,
            currentUserId,
            periodExisting.ContractBudgetAmount ?? 0,
            supplyMethodCode.Value,
            supplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : supplyMethodSpecialTypeCode?.Value,
            ct);

        return defaultAcceptors.Any(a =>
            a.OrganizationLevel == 100 || a.OrganizationLevel == 200 || a.OrganizationLevel == 300);
    }

    protected async Task<string?> GetSourceDepartmentOrganizationLevelAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        CancellationToken ct)
    {
        if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Plan)
        {
            var planData = await this.dbContext.Plans
                .Include(p => p.Department)
                .FirstOrDefaultAsync(p => p.Id == PlanId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            return planData?.Department.OrganizationLevel;
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
        {
            var contractDraftVendorData = await this.dbContext.CaContractDraftVendors
                .Include(x => x.ContractDraft).ThenInclude(x => x.Procurement).ThenInclude(x => x.Department)
                .FirstOrDefaultAsync(x => x.Id == ContractDraftVendorId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            return contractDraftVendorData?.ContractDraft.Procurement.Department.OrganizationLevel;
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
        {
            var poaData = await this.dbContext.PPurchaseOrderApprovals
                .Include(poa => poa.Procurement).ThenInclude(p => p.Department)
                .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            return poaData?.Procurement.Department.OrganizationLevel;
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEditBase = await this.dbContext.CaContractDraftVendorEdits
                .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (vendorEditBase is null)
            {
                return null;
            }

            var cdvBase = await this.dbContext.CaContractDraftVendors
                .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Department)
                .FirstOrDefaultAsync(v => v.Id == vendorEditBase.ContractDraftVendorId, ct);

            return cdvBase?.ContractDraft.Procurement.Department.OrganizationLevel;
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Manual)
        {
            var delivery = await this.dbContext.CmDeliveryAcceptances
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.Id == periodExisting.CmDeliveryAcceptanceId, ct);

            return delivery?.Department?.OrganizationLevel;
        }

        return null;
    }

    protected async Task<CmDeliveryAcceptancePeriod?> GetById(
        CmDeliveryAcceptanceId deliveryAcceptanceId,
        CmDeliveryAcceptancePeriodId deliveryAcceptancePeriodId,
        CancellationToken ct)
    {
        return
            await this.dbContext.CmDeliveryAcceptancePeriods
                      .Include(da => da.Acceptors)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .ThenInclude(e => e.Positions)
                      .ThenInclude(p => p.Position)
                      .Include(c => c.Acceptors)
                      .ThenInclude(c => c.CommitteePosition)
                      .Include(da => da.Acceptors)
                      .ThenInclude(a => a.Delegatee)
                      .Include(da => da.Assignees)
                      .ThenInclude(a => a.Delegatee)
                      .Include(da => da.Assignees)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .ThenInclude(e => e.View)
                      .Include(da => da.DocumentHistories)
                      .Include(x => x.PaymentTerms)
                      .Include(x => x.Budgets)
                      .Include(x => x.Attachments)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(
                          da =>
                              da.CmDeliveryAcceptanceId == deliveryAcceptanceId &&
                              da.Id == deliveryAcceptancePeriodId,
                          ct);
    }

    protected bool IsHasJorPorSection(CmDeliveryAcceptancePeriod periodExisting)
    {
        if (periodExisting.Assignees.Any(a => a.Type == AssigneeType.Assignee))
        {
            return true;
        }

        if (periodExisting.HasDeduction)
        {
            return true;
        }

        return periodExisting.Acceptors
                             .Any(a =>
                                 a.Type == AcceptorType.Approver &&
                                 a.User.Employee.Positions.Any(rep =>
                                     rep.Position.InRefCode.IsAssistantManagingDirector()));
    }

    protected async Task<AssigneeNoIdResponse> GetDefaultAssigneeAsync(CancellationToken ct)
    {
        var user =
            await this.dbContext
                      .RawEmployeePositions
                      .Include(p => p.Employee)
                      .ThenInclude(e => e.View)
                      .Where(p =>
                          p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.BusinessUnitId) &&
                          p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                      .SelectMany(p => p.Employee.Users)
                      .FirstOrDefaultAsync(ct);

        if (user == null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูล ผู้ช่วย ผอ. จพ. ส่วนงานบริหารสัญญา",
                StatusCodes.Status404NotFound);
        }

        if (user.Employee.View == null)
        {
            this.ThrowError(
                $"ไม่พบข้อมูล ผู้ช่วย ผอ. จพ. ส่วนงานบริหารสัญญา",
                StatusCodes.Status404NotFound);
        }

        return
            new AssigneeNoIdResponse(
                null,
                AssigneeGroup.Contract,
                AssigneeType.Director,
                user.Id.Value,
                1,
                user.FullName,
                user.Employee.View.FullPositionName.Trim(),
                user.Employee.View.BusinessUnitName.Trim(),
                AssigneeStatus.Draft);
    }

    protected void ValidateUsers(
        SuUser[] users,
        UserId[] userIds)
    {
        var foundUserIds =
            users.Select(u => u.Id)
                 .ToArray();

        var missingUserIds =
            userIds.Except(foundUserIds)
                   .ToArray();

        if (missingUserIds.Any())
        {
            this.ThrowError(
                $"ไม่พบข้อมูลผู้ใช้งาน {string.Join(", ", missingUserIds)}.",
                StatusCodes.Status404NotFound);
        }
    }

    protected async Task UpsertAssigneeAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        AssigneeRequest[] assigneesRequest,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        DeliveryAcceptancePeriodEndpointBase<TRequest, TResponse>.RemoveObsoleteAssignees(periodExisting, assigneesRequest);

        var userIdsIncoming = assigneesRequest.Map(s => UserId.From(s.UserId)).ToArray();
        var usersIncoming = await this.GetUsersFromDatabaseAsync(userIdsIncoming, ct);

        this.ValidateUsersExist(userIdsIncoming, usersIncoming);

        var lastAssigneeUserId = assigneesRequest
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)UserId.From(a.UserId))
            .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        DeliveryAcceptancePeriodEndpointBase<TRequest, TResponse>.AddNewAssignees(periodExisting, assigneesRequest, usersIncoming, resolvedSendToAcceptorId);
        DeliveryAcceptancePeriodEndpointBase<TRequest, TResponse>.UpdateExistingAssigneeSequences(periodExisting, assigneesRequest, resolvedSendToAcceptorId);
    }

    protected async Task SetDefaultDocumentTemplate(
        CmDeliveryAcceptancePeriod deliveryAcceptancePeriod,
        ParameterCode supplyMethodCode,
        Guid currentUserId,
        bool isJorPorSection,
        CancellationToken ct = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var query =
            this.dbContext.SuDocumentTemplates
                .Where(c =>
                    c.Group == DocumentTemplateGroups.CMR &&
                    c.SupplyMethodCode == supplyMethodCode)
                .WhereIfTrue(
                    isJorPorSection,
                    c => c.AdditionalInfo!.RootElement
                          .GetProperty(nameof(SuDocumentTemplate.IsFine))
                          .GetBoolean() == true);

        var deliveryAcceptanceTemplateDocId =
            await documentService.GetDocumentTemplateAsync(
                query,
                $"{DocumentTemplateGroups.CMR}/{deliveryAcceptancePeriod.AcceptanceNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                ct);

        if (deliveryAcceptanceTemplateDocId is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารแม่แบบสำหรับการส่งมอบพัสดุ",
                StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.MapToReplaceDtoAsync(
            deliveryAcceptancePeriod,
            ct,
            currentUserId,
            creatorUserId: (Domain.SystemUtility.UserId?)currentUserId);

        var parentDirectory =
            $"{DocumentTemplateGroups.CMR}/{deliveryAcceptancePeriod.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var newFileId = await documentService.CopyDocumentTemplateAsync(
            deliveryAcceptanceTemplateDocId.Value,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: parentDirectory,
            cancellationToken: ct);

        if (newFileId is null)
        {
            this.ThrowError(
                $"เกิดข้อผิดพลาดในการคัดลอกเอกสาร",
                StatusCodes.Status404NotFound);
        }

        deliveryAcceptancePeriod.AddDocumentHistory(
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            newFileId.Value);
    }

    protected async Task UpdateDocumentHistory(
        CmDeliveryAcceptancePeriod deliveryAcceptancePeriod,
        ParameterCode supplyMethodCode,
        Guid currentUserId,
        bool isJorPorSection,
        bool isCurrentDoc = false,
        CancellationToken ct = default)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var document = deliveryAcceptancePeriod.Document;

        if (deliveryAcceptancePeriod.Status is CmDeliveryAcceptancePeriodStatus.Edit or CmDeliveryAcceptancePeriodStatus.Rejected)
        {
            document = deliveryAcceptancePeriod.LastedNotReplacedDocument;
        }

        if (document is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารสำหรับการส่งมอบพัสดุ",
                StatusCodes.Status404NotFound);
        }

        var query =
            this.dbContext.SuDocumentTemplates
                .Where(c =>
                    c.Group == DocumentTemplateGroups.CMR &&
                    c.SupplyMethodCode == supplyMethodCode)
                .WhereIfTrue(
                    isJorPorSection,
                    c => c.AdditionalInfo!.RootElement
                          .GetProperty(nameof(SuDocumentTemplate.IsFine))
                          .GetBoolean() == true)
                .WhereIfTrue(
                    !isJorPorSection,
                    c => c.AdditionalInfo == null);

        var deliveryAcceptanceTemplateDocId =
            await documentService.GetDocumentTemplateAsync(
                query,
                $"{DocumentTemplateGroups.CMR}/{deliveryAcceptancePeriod.AcceptanceNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                ct);

        if (deliveryAcceptanceTemplateDocId is null)
        {
            this.ThrowError(
                $"ไม่พบเอกสารแม่แบบสำหรับการส่งมอบพัสดุ",
                StatusCodes.Status404NotFound);
        }

        var replaceDto = await this.MapToReplaceDtoAsync(
            deliveryAcceptancePeriod,
            ct,
            currentUserId,
            creatorUserId: (Domain.SystemUtility.UserId?)currentUserId);

        var parentDirectory =
            $"{DocumentTemplateGroups.CMR}/{deliveryAcceptancePeriod.Id}_ResetFromTemplate_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

        var copyDocumentIdAsync = isCurrentDoc switch
        {
            true => documentService
                .CopyDocumentTemplateAsync(
                    deliveryAcceptanceTemplateDocId.Value,
                    contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                    parentDirectory,
                    ct),
            _ => Task.FromResult<FileId?>(document.FileId),
        };

        var copyDocumentId = await copyDocumentIdAsync;

        if (copyDocumentId is null)
        {
            this.ThrowError(
                $"ไม่สามารถคัดลอกเอกสารแม่แบบสำหรับการส่งมอบพัสดุได้",
                StatusCodes.Status500InternalServerError);
        }

        deliveryAcceptancePeriod.AddDocumentHistory(
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            copyDocumentId.Value);
    }

    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        CmDeliveryAcceptancePeriod entity,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = entity.DocumentHistories
                                  .OrderVersions()
                                  .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            entity.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();

        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.CMR}/{entity.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        entity.AddDocumentHistory(
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            copiedFileId.Value,
            isReplace ?? false);

        var newHistory = entity.DocumentHistories
                               .OrderVersions()
                               .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    private async Task<SuUser?> GetLastActivityCreatedByAsync(
        string key,
        string type,
        CancellationToken ct)
    {
        var lastActivity =
            await this.dbContext.SuActivityLogs
                      .Where(l =>
                          l.Key == key &&
                          l.ActivityInfo.Type == type)
                      .OrderByDescending(l => l.AuditInfo.CreatedAt)
                      .FirstOrDefaultAsync(cancellationToken: ct);

        if (lastActivity is null)
        {
            return null;
        }

        var createByUser =
            await this.dbContext.SuUsers
                      .Include(u => u.Employee)
                      .ThenInclude(e => e.View)
                      .FirstOrDefaultAsync(
                          u => u.Id == UserId.From(lastActivity.AuditInfo.CreatedBy),
                          ct);

        return createByUser;
    }

    protected async Task<DeliveryAcceptancePeriodReplaceDto> MapToReplaceDtoAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        CancellationToken ct,
        Guid currentUserId,
        UserId? creatorUserId)
    {
        var periodAcceptance = DeliveryAcceptancePeriodEndpointBase<TRequest, TResponse>.MapPeriodAcceptanceInfo(periodExisting);
        var acceptanceCommittees = DeliveryAcceptancePeriodEndpointBase<TRequest, TResponse>.MapAcceptors(periodExisting, AcceptorType.AcceptanceCommittee);
        var acceptors = this.GetAcceptorReplace(periodExisting);
        var committees = this.GetCommitteeReplace(periodExisting);
        var assignees = await this.MapToAssigneeResponseAsync(periodExisting, ct);

        var jorPorCommentReplace = periodExisting.Status
            is CmDeliveryAcceptancePeriodStatus.WaitingComment
            or CmDeliveryAcceptancePeriodStatus.WaitingAcceptance
            ? periodExisting.Assignees
                .Select(DelegatorExtensions.DelegatorToAssignee)
                .FirstOrDefault(a => a.Type == AssigneeType.Assignee
                    && a.UserId == UserId.From(currentUserId))
                is { Remark: not null and not "" } commentAssignee
                ? new JorPorCommentReplace(
                    commentAssignee.UserId.Value,
                    commentAssignee.FullName,
                    commentAssignee.FullName,
                    commentAssignee.PositionName,
                    commentAssignee.Remark,
                    "ผู้จัดทำ")
                : null
            : null;

        var hasJorPorAssign = this.HasJorPorAssign(periodExisting, acceptors);

        var committeesGroupType = periodExisting.Acceptors
                                                .Where(a => a.Type == AcceptorType.AcceptanceCommittee)
                                                .Any(a => a.CommitteePositionsCode == ParameterCode.From("PosBoardInsp001"))
            ? "ผู้ตรวจรับพัสดุ"
            : "คณะกรรมการตรวจรับพัสดุ";

        var hasEditPermission = true;

        var lastedDocumentHistory = periodExisting.GetDocumentForStatus(periodExisting.Status);

        ParameterCode supplyMethodCode;
        ParameterCode? supplyMethodSpecialTypeCode;
        string? supplyMethodSpecialName = null;
        var isCommercialMaterial = false;
        string? contractDescription = null;
        string? contractName = null;

        if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Plan)
        {
            var planData = await this.dbContext.Plans
                                     .Include(p => p.SupplyMethodSpecialType)
                                     .FirstOrDefaultAsync(p => p.Id == PlanId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (planData is null)
            {
                this.ThrowError("ไม่พบข้อมูลแผน", StatusCodes.Status404NotFound);
            }

            supplyMethodCode = planData.SupplyMethodCode;
            supplyMethodSpecialTypeCode = planData.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = planData.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = planData.IsCommercialMaterial ?? false;
            contractName = planData.Name;
            contractDescription = "ใบสั่งซื้อ/จ้าง/เช่า เลขที่ .................................................. ลงวันที่ ..................................................";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendor)
        {
            var contractDraftVendorData = await this.dbContext.CaContractDraftVendors
                                                    .Include(x => x.ContractDraft)
                                                    .ThenInclude(x => x.Procurement)
                                                    .ThenInclude(x => x.SupplyMethodSpecialType)
                                                    .FirstOrDefaultAsync(x => x.Id == ContractDraftVendorId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (contractDraftVendorData is null)
            {
                this.ThrowError("ไม่พบร่างสัญญาของผู้ขาย", StatusCodes.Status404NotFound);
            }

            var procurement = contractDraftVendorData.ContractDraft.Procurement;
            supplyMethodCode = procurement.SupplyMethodCode;
            supplyMethodSpecialTypeCode = procurement.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = procurement.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = procurement.IsCommercialMaterial;
            contractName = contractDraftVendorData.ContractName;
            contractDescription =
                $"สัญญาเลขที่จพ.(สบส.) {contractDraftVendorData.ContractNumber} ลงวันที่ {contractDraftVendorData.ContractSignedDate?.ToThaiDateString(includeBuddhistEra: false)} สัญญา {contractDraftVendorData.ContractName}";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Procurement)
        {
            var poaData = await this.dbContext.PPurchaseOrderApprovals
                                    .Include(poa => poa.Procurement)
                                    .ThenInclude(p => p.SupplyMethodSpecialType)
                                    .Include(poa => poa.Contracts)
                                    .FirstOrDefaultAsync(poa => poa.Id == PurchaseOrderApprovalId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (poaData is null)
            {
                this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า", StatusCodes.Status404NotFound);
            }

            var procurement = poaData.Procurement;
            supplyMethodCode = procurement.SupplyMethodCode;
            supplyMethodSpecialTypeCode = procurement.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = procurement.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = procurement.IsCommercialMaterial;
            var poContract = poaData.Contracts.FirstOrDefault();
            contractName = procurement.Name;
            contractDescription = $"ใบสั่งซื้อ/จ้าง/เช่า เลขที่ {poContract?.PoNumber} ลงวันที่ ................................................";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEditBase = await this.dbContext.CaContractDraftVendorEdits
                .FirstOrDefaultAsync(ve => ve.Id == ContractDraftVendorEditId.From((Guid)periodExisting.CmDeliveryAcceptance.RefId), ct);

            if (vendorEditBase is null)
            {
                this.ThrowError("ไม่พบข้อมูลบันทึกต่อท้ายสัญญา", StatusCodes.Status404NotFound);
            }

            var cdvBase = await this.dbContext.CaContractDraftVendors
                .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethodSpecialType)
                .FirstOrDefaultAsync(v => v.Id == vendorEditBase.ContractDraftVendorId, ct);

            if (cdvBase is null)
            {
                this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
            }

            var procurementBase = cdvBase.ContractDraft.Procurement;
            supplyMethodCode = procurementBase.SupplyMethodCode;
            supplyMethodSpecialTypeCode = procurementBase.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = procurementBase.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = procurementBase.IsCommercialMaterial;
            contractName = vendorEditBase.ContractName;
            contractDescription =
                $"สัญญาเลขที่จพ.(สบส.) {vendorEditBase.ContractNumber} ลงวันที่ {vendorEditBase.ContractSignedDate?.ToThaiDateString(includeBuddhistEra: false)} สัญญา {vendorEditBase.ContractName}";
        }
        else if (periodExisting.CmDeliveryAcceptance.SourceType == SourceType.Manual)
        {
            var delivery = await this.dbContext.CmDeliveryAcceptances
                .Include(d => d.SupplyMethodSpecialType)
                .FirstOrDefaultAsync(d => d.Id == periodExisting.CmDeliveryAcceptanceId, ct);

            if (delivery is null)
            {
                this.ThrowError("ไม่พบข้อมูลการตรวจรับพัสดุ", StatusCodes.Status404NotFound);
            }

            if (delivery.SupplyMethodCode is null)
            {
                this.ThrowError("ไม่พบข้อมูลวิธีการจัดซื้อจัดจ้าง", StatusCodes.Status404NotFound);
            }

            supplyMethodCode = (ParameterCode)delivery.SupplyMethodCode!;
            supplyMethodSpecialTypeCode = delivery.SupplyMethodSpecialTypeCode;
            supplyMethodSpecialName = delivery.SupplyMethodSpecialType?.Label;
            isCommercialMaterial = delivery.IsCommercialMaterial ?? false;
            contractName = delivery.Name;
            contractDescription = "...............................................................................................";
        }
        else
        {
            throw new InvalidOperationException("SourceType ไม่ถูกต้อง");
        }

        var creatorReplace = await this.GetCreatorReplaceAsync(periodExisting, creatorUserId, ct);

        var processType = SectionProcessType.DeliveryAcceptancePeriod;

        if (periodExisting.HasDeduction)
        {
            processType = SectionProcessType.DeliveryAcceptancePeriodPenalty;
        }
        else if (supplyMethodCode.Value == SupplyMethodConstant.Eighty && isCommercialMaterial)
        {
            processType = SectionProcessType.DeliveryAcceptancePeriodCommercialParcel;
        }

        var defaultAcceptors = await this.operationService.GetDefaultAcceptorAsync(
            processType,
            currentUserId,
            periodExisting.ContractBudgetAmount ?? 0,
            supplyMethodCode.Value,
            supplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : supplyMethodSpecialTypeCode?.Value,
            ct);

        var hasHighLevelApprover = defaultAcceptors.Any(a =>
            a.OrganizationLevel == 100 || a.OrganizationLevel == 200 || a.OrganizationLevel == 300);

        var skipCurrentUserId = true;

        if (hasHighLevelApprover)
        {
            var jorPorDirector = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

            if (jorPorDirector is not null)
            {
                currentUserId = jorPorDirector.UserId.Value;
                skipCurrentUserId = false;
            }
        }

        var managers =
            await this.operationService.GetDefaultAcceptorPositionIgnorePrefixAsync(
                processType,
                currentUserId,
                periodExisting.ContractBudgetAmount ?? 0,
                supplyMethodCode.Value,
                supplyMethodCode.Value is SupplyMethodConstant.Eighty ? default : supplyMethodSpecialTypeCode?.Value,
                ct,
                skipCurrentUserId);

        var positionNamePrefix = hasHighLevelApprover
            ? this.operationService.AddPositionNamePrefixPassThroughJorPorDirector(managers)
            : this.operationService.AddPositionNamePrefix(managers);

        var sectionApproveName = positionNamePrefix.Select(m => new SectionApprove(m.PositionName))
                                                   .DefaultIfEmpty(new SectionApprove(string.Empty));

        var commandText =
            this.commandTextService.GetCommandText(
                CommandTextProgram.DeliveryAcceptance,
                managers,
                supplyMethodCode,
                periodExisting.AcceptedAmount ?? 0,
                supplyMethodSpecialType: supplyMethodSpecialTypeCode,
                supplyMethodSpecialName: supplyMethodSpecialName);

        var acceptorDate =
            periodExisting.Status is not
                (CmDeliveryAcceptancePeriodStatus.Draft
                    or CmDeliveryAcceptancePeriodStatus.Edit
                    or CmDeliveryAcceptancePeriodStatus.Rejected)
                ? periodExisting.DocumentDate?.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
                : null;

        var result =
            new DeliveryAcceptancePeriodReplaceDto(
                periodExisting.Id,
                periodExisting.Status,
                null,
                acceptorDate,
                sectionApproveName,
                periodExisting.AcceptanceNumber,
                contractName,
                null,
                null,
                null,
                periodExisting.Description,
                null,
                null,
                null,
                hasJorPorAssign,
                hasEditPermission,
                periodAcceptance,
                acceptanceCommittees,
                assignees,
                acceptors,
                committees,
                lastedDocumentHistory?.FileId.Value,
                lastedDocumentHistory?.IsReplaced,
                creatorReplace,
                commandText,
                periodExisting.PhoneNumber,
                periodExisting.ObjectiveDescription,
                periodExisting.PaymentTerms.Select(x => new PaymentTermReplaceDto(
                    x.PaymentTerm,
                    x.Amount.ToCurrencyStringWithComma(),
                    x.Amount.ThaiBahtText(),
                    x.Description)),
                committeesGroupType,
                supplyMethodSpecialName,
                contractDescription,
                jorPorCommentReplace);

        return result;
    }

    protected async ValueTask ReplaceDocumentsForStatusAsync(
        CmDeliveryAcceptancePeriod entity,
        Guid currentUserId,
        CmDeliveryAcceptancePeriodStatus statusContext,
        CancellationToken ct)
    {
        var replaceTemplate = entity.GetDocumentForStatus(statusContext);

        if (replaceTemplate is null)
        {
            return;
        }

        var documentService = this.Resolve<IDocumentService>();
        var replaceDto = await this.MapToReplaceDtoAsync(entity, ct, currentUserId, null);
        var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        var finalFileId = await documentService.CopyDocumentTemplateAsync(
            replaceTemplate.FileId,
            contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
            parentDirectory: $"{DocumentTemplateGroups.DeliveryAcceptancePeriod}/{entity.Id}_{CmDeliveryAcceptanceDocumentType.DeliveryAcceptance}_{timeStamp}.odt",
            cancellationToken: ct);

        if (finalFileId is null)
        {
            this.ThrowError("ไม่สามารถคัดลอกเอกสาร");
        }

        entity.AddDocumentHistory(
            CmDeliveryAcceptanceDocumentType.DeliveryAcceptance,
            finalFileId.Value,
            false);
    }

    private async Task<CreatorReplace?> GetCreatorReplaceAsync(
        CmDeliveryAcceptancePeriod periodExisting,
        UserId? creatorUserId,
        CancellationToken ct)
    {
        var sendToCommitteeApproveByUser =
            creatorUserId is not null
                ? await this.dbContext.SuUsers
                            .Include(suUser => suUser.Employee)
                            .ThenInclude(rawEmployee => rawEmployee.View)
                            .FirstOrDefaultAsync(u => u.Id == creatorUserId, ct)
                : await this.GetLastActivityCreatedByAsync(
                    periodExisting.Id.ToString(),
                    ActivityLogActionTypeConstant.SendCommitteeApprove,
                    ct);

        if (sendToCommitteeApproveByUser == null)
        {
            return null;
        }

        return new CreatorReplace(
            sendToCommitteeApproveByUser.Id.Value,
            "ผู้จัดทำ",
            sendToCommitteeApproveByUser.FullName,
            sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
            string.Empty);
    }

    private bool HasJorPorAssign(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorReplace[] acceptors)
    {
        if (periodExisting.HasDeduction)
        {
            return true;
        }

        var acceptorUserIds =
            acceptors
                .Select(a => a.UserId)
                .ToHashSet();

        var acceptorEmployeeCodes =
            this.dbContext.SuUsers
                .Where(u => acceptorUserIds.Contains((Guid)u.Id))
                .Select(u => u.EmployeeCode)
                .ToHashSet();

        return
            this.dbContext.RawEmployees
                .Any(e =>
                    acceptorEmployeeCodes.Contains(e.Id) &&
                    e.Positions.Any(p => p.Position.InRefCode == EmployeeConstant.InReferenceCode.ManagingDirector));
    }

    private static PeriodAcceptanceInfoDto MapPeriodAcceptanceInfo(CmDeliveryAcceptancePeriod periodExisting)
    {
        return
            new PeriodAcceptanceInfoDto(
                periodExisting.AcceptanceDate,
                periodExisting.AcceptanceNumber,
                periodExisting.Description,
                periodExisting.AcceptedAmount,
                periodExisting.HasDeduction);
    }

    private static AcceptorNoIdResponse[] MapAcceptors(
        CmDeliveryAcceptancePeriod periodExisting,
        AcceptorType acceptorType)
    {
        return
        [
            .. periodExisting.Acceptors
                             .Where(a =>
                                 a.Type == acceptorType &&
                                 a is
                                 {
                                     IsDeleted: false,
                                     IsActive: true,
                                 })
                             .OrderBy(a => a.Sequence)
                             .Select(a =>
                                 new AcceptorNoIdResponse(
                                     a.Id.Value,
                                     a.Type,
                                     a.UserId.Value,
                                     a.Sequence,
                                     a.User.FullName,
                                     a.PositionName,
                                     a.BusinessUnitName,
                                     a.Status,
                                     a.Remark,
                                     a.ActionAt,
                                     a.CommitteePositionsCode.HasValue ? (string)a.CommitteePositionsCode : string.Empty,
                                     a.CommitteePosition?.Label,
                                     DelegateId: a.DelegateeId?.Value,
                                     IsUnableToPerformDuties: a.IsUnableToPerformDuties,
                                     IsCurrent: a.IsCurrentApprover()))
        ];
    }

    private async Task<AssigneeNoIdResponse[]> MapToAssigneeResponseAsync(CmDeliveryAcceptancePeriod periodExisting, CancellationToken ct = default)
    {
        if (!periodExisting.Assignees.Any())
        {
            var defaultAssignee =
                await this.GetDefaultAssigneeAsync(ct);

            return [defaultAssignee];
        }

        return
        [
            .. periodExisting.Assignees
                             .Where(a => !a.IsDeleted)
                             .Select(DelegatorExtensions.DelegatorToAssignee)
                             .OrderBy(a => a.Sequence)
                             .Select(a =>
                                 new AssigneeNoIdResponse(
                                     a.Id.Value,
                                     a.Group,
                                     a.Type,
                                     a.UserId.Value,
                                     a.Sequence,
                                     a.User.FullName,
                                     a.PositionName,
                                     a.BusinessUnitName,
                                     a.Status,
                                     a.Remark,
                                     a.ActionAt))
        ];
    }

    private AcceptorReplace[] GetAcceptorReplace(CmDeliveryAcceptancePeriod periodExisting)
    {
        if (periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingAcceptance && periodExisting.Status != CmDeliveryAcceptancePeriodStatus.Approved)
        {
            return [];
        }

        AcceptorReplace[] acceptors =
        [
            .. periodExisting.Acceptors
                             .Where(a => a.Type == AcceptorType.Approver)
                             .Select(DelegatorExtensions.DelegatorToAcceptor)
                             .Map(this.MapAcceptorReplace)
                             .OrderBy(a => a.Sequence)
        ];

        if (acceptors.Any())
        {
            acceptors[^1] =
                acceptors.Last() with { Action = "รับทราบผลการตรวจรับ" };
        }

        return [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
    }

    private AcceptorReplace[] GetCommitteeReplace(CmDeliveryAcceptancePeriod periodExisting)
    {
        if (periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingCommitteeApproval &&
            periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingAcceptance &&
            periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingAssign &&
            periodExisting.Status != CmDeliveryAcceptancePeriodStatus.WaitingComment &&
            periodExisting.Status != CmDeliveryAcceptancePeriodStatus.Approved)
        {
            return [];
        }

        return
        [
            .. periodExisting.Acceptors
                             .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a is { IsActive: true })
                             .Map(this.MapAcceptorReplace)
                             .OrderBy(a => a.Sequence)
        ];
    }

    private AcceptorReplace MapAcceptorReplace(CmDeliveryAcceptancePeriodAcceptor acceptor)
    {
        var actionLabel =
            acceptor.Type switch
            {
                AcceptorType.Approver => "เห็นชอบ",
                AcceptorType.AcceptanceCommittee =>
                    acceptor.Status switch
                    {
                        AcceptorStatus.Approved => "เห็นชอบ",
                        AcceptorStatus.Rejected => "ไม่เห็นชอบ",
                        AcceptorStatus.UnableToPerformDuties => acceptor.Remark,
                        _ => string.Empty,
                    },
                _ => string.Empty,
            };

        return new AcceptorReplace(
            acceptor.UserId.Value,
            acceptor.Sequence,
            actionLabel ?? string.Empty,
            acceptor.User.FullName,
            acceptor.FullName,
            acceptor.User.Employee.View?.FullPositionName ?? string.Empty,
            string.Empty,
            string.Empty,
            acceptor.Status,
            acceptor.PositionName,
            acceptor.CommitteePosition?.Label ?? string.Empty);
    }

    private static void RemoveObsoleteAssignees(
        CmDeliveryAcceptancePeriod periodExisting,
        AssigneeRequest[] assigneesRequest)
    {
        var requestIds = assigneesRequest.Select(s => s.Id).ToHashSet();

        periodExisting.Assignees
                      .Where(w => !requestIds.Contains(w.Id.Value))
                      .ToList()
                      .ForEach(s => periodExisting.RemoveAssignee(s));
    }

    private async Task<SuUser[]> GetUsersFromDatabaseAsync(
        UserId[] userIds,
        CancellationToken ct)
    {
        return await this.dbContext.SuUsers
                         .Include(r => r.Employee)
                         .ThenInclude(r => r.View)
                         .Where(w => userIds.Contains(w.Id))
                         .ToArrayAsync(ct);
    }

    private void ValidateUsersExist(
        UserId[] expectedUserIds,
        SuUser[] actualUsers)
    {
        var userNotExistsInDb = expectedUserIds.Except(actualUsers.Map(u => u.Id)).ToArray();

        if (userNotExistsInDb.Any())
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userNotExistsInDb)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private static void AddNewAssignees(
        CmDeliveryAcceptancePeriod periodExisting,
        AssigneeRequest[] assigneesRequest,
        SuUser[] usersIncoming,
        UserId? sendToAcceptorId = null)
    {
        var newAssignees = assigneesRequest
                           .Where(w => !w.Id.HasValue)
                           .Join(
                               usersIncoming,
                               req => UserId.From(req.UserId),
                               usr => usr.Id,
                               (req, usr) => CmDeliveryAcceptancePeriodAssignee.Create(
                                   periodExisting.Id,
                                   req.AssigneeGroup,
                                   req.AssigneeType,
                                   usr,
                                   req.Sequence));

        foreach (var assignee in newAssignees)
        {
            assignee.SetSendToAcceptorId(sendToAcceptorId);
            periodExisting.AddAssignee(assignee);
        }
    }

    private static void UpdateExistingAssigneeSequences(
        CmDeliveryAcceptancePeriod periodExisting,
        AssigneeRequest[] assigneesRequest,
        UserId? sendToAcceptorId = null)
    {
        var requestLookup = assigneesRequest.ToDictionary(req => req.UserId, req => req.Sequence);

        foreach (var existing in periodExisting.Assignees)
        {
            if (requestLookup.TryGetValue(existing.UserId.Value, out var sequence))
            {
                existing.SetSequence(sequence);
                existing.SetSendToAcceptorId(sendToAcceptorId);
            }
        }
    }
}