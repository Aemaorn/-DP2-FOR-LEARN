namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetAuditAndRevenueByIdRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public record AuditAndRevenueDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetAuditAndRevenueByIdResponse(
    Guid Id,
    string DocumentNumber,
    Guid? AuditReportDocumentId,
    bool? IsAuditReportDocumentIdReplaced,
    Guid? AuditGeneralReportDocumentId,
    bool? IsAuditGeneralReportDocumentIdReplaced,
    Guid? RevenueReportDocumentId,
    bool? IsRevenueReportDocumentIdReplaced,
    DateTimeOffset DocumentDate,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate,
    DateTimeOffset DeliveryDate,
    RpAuditAndRevenueStatus Status,
    IEnumerable<AuditAndRevenueDetailResponseDto> Details,
    IEnumerable<AcceptorResponse> ApprovalAcceptors,
    AttachmentsDtoWithId[] Attachments,
    bool HasPermission,
    AuditAndRevenueDocumentVersionResponse[]? AuditReportDocumentVersions = null,
    AuditAndRevenueDocumentVersionResponse[]? AuditGeneralReportDocumentVersions = null,
    AuditAndRevenueDocumentVersionResponse[]? RevenueReportDocumentVersions = null);

public record AuditAndRevenueDetailResponseDto(
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
    bool Overdue,
    int Sequence);

public class GetAuditAndRevenueByIdEndpoint : AuditAndRevenueEndpoint<GetAuditAndRevenueByIdRequest, Results<Ok<GetAuditAndRevenueByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetAuditAndRevenueByIdEndpoint(ILogger<GetAuditAndRevenueByIdEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient, IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("report/audit-revenue/{id:guid}");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("GetAuditAndRevenueById")
                              .AllowAnonymous()
                              .Produces<GetAuditAndRevenueByIdResponse>(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<GetAuditAndRevenueByIdResponse>, NotFound<string>>> HandleRequestAsync(GetAuditAndRevenueByIdRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpAuditAndRevenues
                               .Include(x => x.Details)
                               .ThenInclude(d => d.CaContractDraftVendor)
                               .ThenInclude(caContractDraftVendor => caContractDraftVendor.Vendor)
                               .ThenInclude(vendor => vendor.VendorInfo)
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.Acceptors)
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.Details)
                               .ThenInclude(rpAuditAndRevenueDetail => rpAuditAndRevenueDetail.CaContractDraftVendor).ThenInclude(caContractDraftVendor => caContractDraftVendor.ContractType)
                               .Include(auditableEntity => auditableEntity.AuditInfo)
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.DocumentHistories)
                               .Include(rpAuditAndRevenue => rpAuditAndRevenue.Attachments)
                               .FirstOrDefaultAsync(x => x.Id == RpAuditAndRevenueId.From(req.Id), cancellationToken: ct)!;

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        var hasEditPermission = entity.AuditInfo.CreatedBy == req.UserId;

        var details = entity.Details?.Select(d =>
        {
            var isOverdueContract =
                d.CaContractDraftVendor.ContractSignedDate.HasValue &&
                (entity.DeliveryDate - d.CaContractDraftVendor.ContractSignedDate.Value).TotalDays > 30;

            return new AuditAndRevenueDetailResponseDto(
                d.Id.Value,
                d.CaContractDraftVendor.Id.Value,
                d.CaContractDraftVendor.ContractDraft.Procurement.Id.Value,
                d.CaContractDraftVendor.ContractTypeCode.Value.ToString(),
                (d.CaContractDraftVendor.ContractType != null ? d.CaContractDraftVendor.ContractType?.Label : string.Empty)!,
                d.CaContractDraftVendor.ContractNumber,
                d.CaContractDraftVendor.ContractName,
                d.CaContractDraftVendor.ContractSignedDate,
                d.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                d.CaContractDraftVendor.Budget,
                d.Description,
                isOverdueContract,
                d.Sequence);
        }) ?? [];

        var auditReportDocumentVersions = entity.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditReport)
            .OrderVersions()
            .Select((d, index) => new AuditAndRevenueDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                index == 0))
            .ToArray();

        var auditGeneralReportDocumentVersions = entity.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditGeneralReport)
            .OrderVersions()
            .Select((d, index) => new AuditAndRevenueDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                index == 0))
            .ToArray();

        var revenueReportDocumentVersions = entity.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.RevenueReport)
            .OrderVersions()
            .Select((d, index) => new AuditAndRevenueDocumentVersionResponse(
                d.FileId.Value,
                d.Version,
                d.CreatedAt,
                d.CreatedByName ?? string.Empty,
                index == 0))
            .ToArray();

        var response = new GetAuditAndRevenueByIdResponse(
            entity.Id.Value,
            entity.DocumentNumber,
            entity.LastAuditReportDocumentHistory?.FileId.Value,
            entity.LastAuditReportDocumentHistory?.IsReplaced,
            entity.LastAuditGeneralReportDocumentHistory?.FileId.Value,
            entity.LastAuditGeneralReportDocumentHistory?.IsReplaced,
            entity.LastRevenueReportDocumentHistory?.FileId.Value,
            entity.LastRevenueReportDocumentHistory?.IsReplaced,
            entity.DocumentDate,
            entity.SignStartDate,
            entity.SignEndDate,
            entity.DeliveryDate,
            entity.Status,
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
            hasEditPermission,
            auditReportDocumentVersions,
            auditGeneralReportDocumentVersions,
            revenueReportDocumentVersions);

        return TypedResults.Ok(response);
    }

    private static bool CurrentAcceptor(IEnumerable<RpAuditRevenueAcceptor> acceptors, Guid acceptorId, RpAuditAndRevenueStatus status)
    {
        if (status is
            RpAuditAndRevenueStatus.Draft or
            RpAuditAndRevenueStatus.Rejected)
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