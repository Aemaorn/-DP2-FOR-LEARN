namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration;

using System.ComponentModel;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.AdjustContractDuration.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record AdjustContractDurationVendorReplaceDto
{
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string ContractName { get; init; }

    public string PoNumber { get; init; }

    public string ContractNumber { get; init; }

    public decimal Budget { get; init; }

    public string? BudgetFormat { get; init; }

    public string? BudgetText { get; init; }

    public DateTimeOffset? ContractSignedDate { get; init; }

    public string? ContractSignedDateFormat { get; init; }

    public string ContractType { get; init; }

    public string Template { get; init; }

    public string PeriodConditionType { get; init; }

    public bool IsWorkingDayOnly { get; init; }

    public Guid? ContractDraftDocumentId { get; init; }

    public bool? IsContractDraftDocumentIdReplace { get; init; }

    public Guid? ApprovalContractDraftDocumentId { get; init; }

    public bool? IsApprovalContractDraftDocumentIdReplace { get; init; }

    public Guid? ConfidentialContractDraftDocumentId { get; init; }

    public bool? IsConfidentialContractDraftDocumentIdReplace { get; init; }

    public ContractDraftVendorStatus Status { get; init; }

    public ContractDraftInfoReplaceDto ContractDraftInfoDetail { get; init; }

    public static AdjustContractDurationVendorReplaceDto FromEntity(CaContractDraftVendor vendor)
    {
        var response = new AdjustContractDurationVendorReplaceDto
        {
            Id = vendor.Id.Value,
            Email = vendor.Email,
            ContractName = vendor.ContractName,
            ContractNumber = vendor.ContractNumber,
            PoNumber = vendor.PoNumber,
            Budget = vendor.Budget,
            BudgetFormat = vendor.Budget.ToCurrencyStringWithComma(),
            BudgetText = vendor.Budget.ThaiBahtText(),
            ContractSignedDate = vendor.ContractSignedDate,
            ContractSignedDateFormat = vendor.ContractSignedDate.ToThaiDateString(),
            ContractType = vendor.ContractTypeCode?.Value ?? string.Empty,
            Template = vendor.TemplateCode?.Value ?? string.Empty,
            PeriodConditionType = vendor.PeriodConditionTypeCode?.Value ?? string.Empty,
            IsWorkingDayOnly = vendor.IsWorkingDayOnly,
            Status = vendor.Status,
            ContractDraftInfoDetail = ContractDraftInfoReplaceDto.FromEntity(vendor),
            ContractDraftDocumentId = vendor.ContractDraftDocument?.FileId.Value,
            ApprovalContractDraftDocumentId = vendor.ApprovedDocument?.FileId.Value,
            ConfidentialContractDraftDocumentId = vendor.ConfidentialDocument?.FileId.Value,
        };

        return response;
    }
}

public record AdjustContractDurationAcceptorReplaceDto(
    Guid? Id,
    AcceptorType AcceptorType,
    Guid UserId,
    int Sequence,
    string? Action,
    string FullName,
    string PositionName,
    string DepartmentName,
    AcceptorStatus Status,
    string? Remark = default,
    DateTimeOffset? ActionAt = default,
    string? CommitteePositionsCode = default,
    string? CommitteePositionName = default,
    bool? IsUnableToPerformDuties = default,
    string? DepartmentCode = default,
    Guid? DelegateId = default,
    bool IsCurrent = default);

public record AdjustContractDurationAssigneeReplaceDto(
    [param: Description("รหัสผู้รับมอบหมาย")]
    Guid? Id,
    [param: Description("กลุ่มผู้รับมอบหมาย")]
    AssigneeGroup AssigneeGroup,
    [param: Description("ประเภทผู้รับมอบหมาย")]
    AssigneeType AssigneeType,
    [param: Description("รหัสผู้ใช้")] Guid UserId,
    [param: Description("ลำดับ")] int Sequence,
    [param: Description("ชื่อ-สกุล")] string FullName,
    [param: Description("ตำแหน่ง")] string PositionName,
    [param: Description("หน่วยงาน")] string DepartmentName,
    [param: Description("สถานะการมอบหมาย")]
    AssigneeStatus Status,
    [param: Description("หมายเหตุ")] string? Remark = default,
    [param: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt = default);

public record AdjustContractDurationApproverReplaceDto(
    string? Action,
    string? FullName,
    string? FullPositionName);

public record GetAdjustContractDurationReplaceDto(
    Guid? Id,
    Guid ContractAmendmentId,
    AdjustContractDurationVendorReplaceDto ContractDraft,
    AdjustContractDurationInfo AdjustContractDurationOld,
    AdjustContractDurationInfo AdjustContractDurationNew,
    AdjustContractDurationApproverReplaceDto? Creator,
    IEnumerable<AdjustContractDurationAcceptorReplaceDto>? Acceptors,
    IEnumerable<AdjustContractDurationAssigneeReplaceDto>? Assignees,
    ContractAmendmentExtendChangeStatus Status);

public class GetListMappingDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingDocumentEndpoint(ILogger<GetListMappingDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractAmendment/AdjustContractDuration"));
        this.Get("contract-amendment/adjust-contract-duration/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(GetAdjustContractDurationReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}