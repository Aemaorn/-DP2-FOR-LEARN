namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.WaiveOrReducePenalty;

using System.ComponentModel;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record WaiveOrReducePenaltyVendorReplaceDto
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

    public static WaiveOrReducePenaltyVendorReplaceDto FromEntity(CaContractDraftVendor vendor)
    {
        var response = new WaiveOrReducePenaltyVendorReplaceDto
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

public record WaiveOrReducePenaltyAcceptorReplaceDto(
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

public record WaiveOrReducePenaltyAssigneeReplaceDto(
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

public record WaiveOrReducePenaltyApproverReplaceDto(
    string? Action,
    string? FullName,
    string? FullPositionName);

public record WaiveOrReducePenaltyReplaceDto(
    Guid? Id,
    Guid CamContractAmendmentId,
    bool WaiveAll,
    WaiveOrReducePenaltyVendorReplaceDto ContractDraft,
    PenaltyInfoReplaceDto PenaltyOld,
    PenaltyInfoReplaceDto PenaltyNew,
    WaiveOrReducePenaltyApproverReplaceDto? Creator,
    IEnumerable<WaiveOrReducePenaltyAcceptorReplaceDto>? Acceptors,
    IEnumerable<WaiveOrReducePenaltyAssigneeReplaceDto>? Assignees,
    CamContractAmendmentWaiveOrReducePenaltyStatus Status)
{
    public static WaiveOrReducePenaltyReplaceDto? MapToResponse(
        CamContractAmendmentWaiveOrReducePenalty? entity,
        SuUser? user,
        bool hasCreator = false,
        bool hasAcceptor = false)
    {
        if (entity is null)
        {
            return null;
        }

        var contractPenalty =
            entity.CamContractAmendment
                  .ContractDraftVendor
                  .DraftTermsConditions
                  .Penalty;

        var penaltyOld = new PenaltyInfoReplaceDto(
            contractPenalty.TypeCode?.Value,
            contractPenalty.Rate,
            contractPenalty.Amount,
            contractPenalty.RateTypeCode?.Value);

        var penaltyNew = new PenaltyInfoReplaceDto(
            entity.PenaltyTypeCode?.Value,
            entity.Rate,
            entity.Amount,
            entity.RateTypeCode?.Value);

        var creator =
            hasCreator
                ? new WaiveOrReducePenaltyApproverReplaceDto("ผู้จัดทำ", user?.Employee.View?.FullName, user?.Employee.View?.FullPositionName)
                : null;

        var lastAcceptors =
            hasAcceptor
                ? entity.Acceptors
                        .Where(a => a.Type == AcceptorType.Approver)
                        .OrderBy(a => a.Sequence)
                        .LastOrDefault()
                : null;

        var acceptors =
            hasAcceptor
                ? [.. entity.Acceptors
                        .Where(a => a.Status == AcceptorStatus.Approved)
                        .Where(a => a is { Type: AcceptorType.Approver, IsUnableToPerformDuties: false })
                        .OrderBy(a => a.Sequence)
                        .Select(a => MapToAcceptorReplace(a, lastAcceptors))]
                : new List<WaiveOrReducePenaltyAcceptorReplaceDto>();

        var assignees = entity.Assignees
                              .OrderBy(o => o.Sequence)
                              .Select(a => new WaiveOrReducePenaltyAssigneeReplaceDto(
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
                                  a.ActionAt));

        return new WaiveOrReducePenaltyReplaceDto(
            entity.Id.Value,
            entity.CamContractAmendment.Id.Value,
            entity.WaiveAll,
            WaiveOrReducePenaltyVendorReplaceDto.FromEntity(entity.CamContractAmendment.ContractDraftVendor),
            penaltyOld,
            penaltyNew,
            creator,
            acceptors,
            assignees,
            entity.Status);

        WaiveOrReducePenaltyAcceptorReplaceDto MapToAcceptorReplace(CamContractAmendmentWaiveOrReducePenaltyAcceptor acceptor, CamContractAmendmentWaiveOrReducePenaltyAcceptor? lastedAcceptor)
        {
            var action =
                (acceptor.Status, lastedAcceptor == acceptor) switch
                {
                    (AcceptorStatus.Approved, false) => "เห็นชอบ",
                    (AcceptorStatus.Approved, true) => "อนุมัติ",
                    _ => "ไมเห็นชอบ",
                };

            return new WaiveOrReducePenaltyAcceptorReplaceDto(
                acceptor.Id.Value,
                acceptor.Type,
                acceptor.UserId.Value,
                acceptor.Sequence,
                action,
                acceptor.User.FullName,
                acceptor.PositionName,
                acceptor.BusinessUnitName,
                acceptor.Status,
                acceptor.Remark,
                acceptor.ActionAt,
                acceptor.CommitteePositionsCode.HasValue ? (string)acceptor.CommitteePositionsCode : string.Empty,
                acceptor.CommitteePosition?.Label ?? string.Empty,
                acceptor.IsUnableToPerformDuties,
                string.Empty,
                acceptor.DelegateeId?.Value,
                acceptor.IsCurrentApprover());
        }
    }
}

public record PenaltyInfoReplaceDto(
    string? PenaltyTypeCode,
    decimal? Rate,
    decimal? Amount,
    string? RateTypeCode)
{
    public static PenaltyInfo Default => new(
        null,
        null,
        null,
        null);
}

public class GetListMappingDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingDocumentEndpoint(ILogger<GetListMappingDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ContractAmendment/WaiveOrReducePenalty"));
        this.Get("contract-amendment/waive-or-reduce-penalty/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(WaiveOrReducePenaltyReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}