namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetReportContractCompletionByQuarterByIdRequest(Guid Id);

public record ContractCompletionByQuarterDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetReportContractCompletionByQuarterByIdResponse(
    Guid Id,
    string DocumentNumber,
    DateTimeOffset DocumentDate,
    int Year,
    int Quarter,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    RpContractCompletionByQuarterStatus Status,
    Guid? DocumentId,
    bool? IsDocumentReplace,
    IEnumerable<ReportContractCompletionByQuarterDetailDto> Detail,
    IEnumerable<AcceptorResponse> Acceptors,
    AttachmentsDtoWithId[] Attachments,
    ContractCompletionByQuarterDocumentVersionResponse[]? DocumentVersions = null);

public record ReportContractCompletionByQuarterDetailDto(
    Guid Id,
    Guid CaContractDraftVendorId,
    Guid ProcurementId,
    string ContractTypeCode,
    string ContractTypeName,
    string ContractNumber,
    string ContractName,
    DateTimeOffset? ContractSignedDate,
    string EntrepreneurName,
    decimal Budget,
    string? Description,
    bool Overdue);

public class GetReportContractCompletionByQuarterByIdEndpoint : ContractCompletionByQuarterEndpoint<GetReportContractCompletionByQuarterByIdRequest,
    Results<Ok<GetReportContractCompletionByQuarterByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetReportContractCompletionByQuarterByIdEndpoint(ILogger<GetReportContractCompletionByQuarterByIdEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("report/contract-completion-by-quarter/{id:guid}");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("GetReportContractCompletionByQuarterById")
                              .AllowAnonymous()
                              .Produces<GetReportContractCompletionByQuarterByIdResponse>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<GetReportContractCompletionByQuarterByIdResponse>, NotFound<string>>> HandleRequestAsync(GetReportContractCompletionByQuarterByIdRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpContractCompletionByQuarters
            .AsNoTracking()
            .Where(x => x.Id == RpContractCompletionByQuarterId.From(req.Id))
            .Select(x => new
            {
                x.Id,
                x.DocumentNumber,
                x.DocumentDate,
                x.Year,
                x.Quarter,
                x.SignStartDate,
                x.SignEndDate,
                x.Status,
                Details = x.Details.OrderByDescending(d => d.CaContractDraftVendor.ContractSignedDate).Select(d => new
                {
                    d.Id,
                    d.Description,
                    CaContractDraftVendorId = d.CaContractDraftVendor.Id,
                    ProcurementId = d.CaContractDraftVendor.ContractDraft.Procurement.Id,
                    ContractTypeCode = d.CaContractDraftVendor.ContractTypeCode.Value.ToString(),
                    ContractTypeName = d.CaContractDraftVendor.ContractType != null ? d.CaContractDraftVendor.ContractType.Label : string.Empty,
                    d.CaContractDraftVendor.ContractNumber,
                    d.CaContractDraftVendor.ContractName,
                    d.CaContractDraftVendor.ContractSignedDate,
                    d.CaContractDraftVendor.Budget,
                    EntrepreneurName = d.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                }).ToList(),
                Acceptors = x.Acceptors.OrderBy(a => a.Sequence).ToList(),
                DocumentHistories = x.DocumentHistories
                    .Where(d => d.DocumentType == RpContractCompletionByQuarterDocumentType.Completion)
                    .ToList(),
                Attachments = x.Attachments.OrderBy(a => a.Sequence).ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        var details = entity.Details.Select(d =>
        {
            var contractTypeCode = d.ContractTypeCode;
            if (contractTypeCode.Equals(ContractRentalTypeConstant.Rent, StringComparison.Ordinal))
            {
                contractTypeCode = ContractTypeConstant.Rent;
            }

            return new ReportContractCompletionByQuarterDetailDto(
                d.Id.Value,
                d.CaContractDraftVendorId.Value,
                d.ProcurementId.Value,
                contractTypeCode,
                d.ContractTypeName,
                d.ContractNumber,
                d.ContractName,
                d.ContractSignedDate,
                d.EntrepreneurName,
                d.Budget,
                d.Description,
                d.ContractSignedDate.HasValue && entity.SignStartDate <= d.ContractSignedDate && d.ContractSignedDate <= entity.SignEndDate);
        });

        var orderedHistories = entity.DocumentHistories.OrderVersions().ToList();
        var lastedApprovalRequestDocument = orderedHistories.FirstOrDefault();

        var documentVersions = orderedHistories
            .Select((d, index) => new ContractCompletionByQuarterDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                index == 0))
            .ToArray();

        var response = new GetReportContractCompletionByQuarterByIdResponse(
            entity.Id.Value,
            entity.DocumentNumber,
            entity.DocumentDate,
            entity.Year,
            entity.Quarter,
            entity.SignStartDate,
            entity.SignEndDate,
            entity.Status,
            lastedApprovalRequestDocument?.FileId.Value,
            lastedApprovalRequestDocument?.IsReplaced ?? false,
            details,
            entity.Acceptors
                  .OrderBy(x => x.Sequence)
                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                  .Select(a => new AcceptorResponse(
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
                      IsCurrent: CurrentAcceptor(entity.Acceptors, a.Id.Value, entity.Status),
                      DelegateeUserId: a.Delegatee?.SuUserId.Value)),
            [.. entity.Attachments
                  .OrderBy(o => o.Sequence)
                  .GroupBy(
                      a => a.DocumentTypeCode,
                      (key, g) => new AttachmentsDtoWithId(
                          key.Value,
                          [.. g.Select(s => new FileAttachmentsWithId(s.Id.Value, s.FileId.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))],
            documentVersions);

        return TypedResults.Ok(response);
    }

    private static bool CurrentAcceptor(IEnumerable<RpContractCompletionByQuarterAcceptor> acceptors, Guid acceptorId, RpContractCompletionByQuarterStatus status)
    {
        if (status is
            RpContractCompletionByQuarterStatus.Draft or
            RpContractCompletionByQuarterStatus.Rejected)
        {
            return false;
        }

        var current = acceptors.FirstOrDefault(a => a.Id.Value == acceptorId);

        if (current == null)
        {
            return false;
        }

        var prev = acceptors
                   .Where(a => a.Sequence < current.Sequence)
                   .OrderByDescending(a => a.Sequence)
                   .FirstOrDefault();

        if (prev == null)
        {
            return current.Status != AcceptorStatus.Approved;
        }

        return current.Status != AcceptorStatus.Approved && prev.Status == AcceptorStatus.Approved;
    }
}