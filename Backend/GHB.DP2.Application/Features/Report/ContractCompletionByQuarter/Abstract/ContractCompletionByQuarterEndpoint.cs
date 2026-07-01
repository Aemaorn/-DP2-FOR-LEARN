namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class ContractCompletionByQuarterEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    protected ContractCompletionByQuarterEndpoint(
        ILogger logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    protected void UpsertAcceptors(RpContractCompletionByQuarter entity, IEnumerable<AcceptorRequest> requests, RpContractCompletionByQuarterStatus status, UserId? sendToAcceptorId = null)
    {
        var acceptors = entity.Acceptors ?? [];
        var newEntities = requests.Select(dto =>
        {
            var existing = acceptors.FirstOrDefault(c => dto.Id == c.Id.Value);

            if (existing != null)
            {
                existing.SetType(existing.Type)
                        .SetUser(existing.UserId, existing.EmployeeCode, existing.FullName, existing.PositionName, existing.BusinessUnitName)
                        .SetSequence(dto.Sequence);

                existing.SetSendToAcceptorId(sendToAcceptorId);

                switch (entity.Status)
                {
                    case RpContractCompletionByQuarterStatus.Draft or RpContractCompletionByQuarterStatus.Rejected when
                        status == RpContractCompletionByQuarterStatus.WaitingApproval:
                        existing.SetStatus(AcceptorStatus.Pending);

                        break;
                }

                return existing;
            }

            var users = this.dbContext.SuUsers.SingleOrDefault(u => u.Id == UserId.From(dto.UserId));

            if (users == null)
            {
                this.ThrowError($"User with ID {dto.UserId} not found.", StatusCodes.Status404NotFound);
            }

            var newAcceptorPending = RpContractCompletionByQuarterAcceptor.Create(dto.AcceptorType, users, dto.Sequence);

            newAcceptorPending.SetSendToAcceptorId(sendToAcceptorId);

            if (status == RpContractCompletionByQuarterStatus.WaitingApproval)
            {
                newAcceptorPending.SetStatus(AcceptorStatus.Pending);
            }

            return newAcceptorPending;
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => acceptors.All(a => a.Id != e.Id)))
        {
            entity.AddAcceptor(toAdd);
        }

        // Remove obsolete
        var toRemoveList = acceptors.Where(a => !newEntities.Any(e => e.Id == a.Id)).ToList();

        foreach (var toRemove in toRemoveList)
        {
            entity.RemoveAcceptor(toRemove);
        }
    }

    protected void UpsertDetails(
        RpContractCompletionByQuarter entity,
        IEnumerable<RpContractCompletionByQuarterDetailDto> requests)
    {
        var details = entity.Details ?? new List<RpContractCompletionByQuarterDetail>();
        var newEntities = requests.Select(dto =>
        {
            var contractDraftVendor = this.dbContext.CaContractDraftVendors.SingleOrDefault(u => u.Id == ContractDraftVendorId.From(dto.CaContractDraftVendorId));

            if (contractDraftVendor == null)
            {
                this.ThrowError($"Contract Draft Vendor with ID {dto.CaContractDraftVendorId} not found.", StatusCodes.Status404NotFound);
            }

            var existing = details.FirstOrDefault(c => c.Id.Value == dto.Id);

            if (existing != null)
            {
                existing.SetValue(
                    dto.Sequence,
                    dto.Description,
                    contractDraftVendor);

                return existing;
            }

            return RpContractCompletionByQuarterDetail.Create()
                                                            .SetValue(
                                                                dto.Sequence,
                                                                dto.Description,
                                                                contractDraftVendor);
        }).ToList();

        // Add new
        foreach (var toAdd in newEntities.Where(e => details.All(a => !a.Id.Equals(e.Id))))
        {
            entity.AddDetail(toAdd);
        }

        // Remove obsolete
        foreach (var toRemove in details.Where(a => !newEntities.Any(e => e.Id.Equals(a.Id))).ToList())
        {
            entity.RemoveDetail(toRemove);
        }
    }

    protected async Task<FileId> GetDocumentTemplateByCriteria(int quarter, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var quarterString = quarter.ToString();

        var candidates =
            await this.dbContext.SuDocumentTemplates
                      .Where(dt =>
                          dt.Group == DocumentTemplateGroups.QuarterlyCompletion &&
                          dt.IsActive)
                      .ToListAsync(ct);

        var template = candidates.FirstOrDefault(dt => dt.Quarter == quarterString);

        if (template is null)
        {
            this.ThrowError(
                $"ไม่พบ template เอกสารสำหรับไตรมาส {quarter}",
                StatusCodes.Status404NotFound);
        }

        var templateId = template.Id;

        var fileId = await documentService.GetDocumentTemplateAsync(
            dt => dt.Id == templateId,
            ct);

        return (FileId)fileId;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        RpContractCompletionByQuarter entity,
        CancellationToken ct)
    {
        // TODO : new to confirm with business for document template
        var documentId =
            await this.GetDocumentTemplateByCriteria(entity.Quarter, ct);

        entity.AddDocumentHistory(
            RpContractCompletionByQuarterDocumentType.Completion,
            documentId);
    }

    protected async Task<RpContractCompletionByQuarter?> GetById(RpContractCompletionByQuarterId id, CancellationToken ct)
    {
        var entity =
            await this.dbContext.RpContractCompletionByQuarters
                      .Include(x => x.Details)
                      .ThenInclude(d => d.CaContractDraftVendor)
                      .ThenInclude(caContractDraftVendor => caContractDraftVendor.Vendor)
                      .ThenInclude(vendor => vendor.VendorInfo)
                      .Include(rpAuditAndRevenue => rpAuditAndRevenue.Acceptors)
                      .ThenInclude(x => x.User)
                      .ThenInclude(x => x.Employee)
                      .Include(rpAuditAndRevenue => rpAuditAndRevenue.Details)
                      .ThenInclude(rpAuditAndRevenueDetail => rpAuditAndRevenueDetail.CaContractDraftVendor).ThenInclude(caContractDraftVendor => caContractDraftVendor.ContractType)
                      .Include(ccq => ccq.DocumentHistories)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken: ct);

        return entity;
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

    protected async Task<ReportContractCompletionByQuarterReplaceDto> MapToReplaceDtoAsync(
        RpContractCompletionByQuarter entity,
        UserId userId,
        bool hasCreator,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var creatorReplace = hasCreator ? await this.GetCreatorReplaceAsync(entity, userId, ct) : null;
        var details = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.MapDetails(entity).ToArray();
        var acceptorsReplace = hasAcceptor ? this.GetAcceptorReplace(entity) : [];

        var rawDetails = await this.dbContext.RpContractCompletionByQuarterDetails
            .AsNoTracking()
            .Where(d =>
                d.RpContractCompletionByQuarter.Year == entity.Year &&
                d.RpContractCompletionByQuarter.Quarter <= entity.Quarter &&
                !d.RpContractCompletionByQuarter.IsDeleted)
            .Select(d => new
            {
                Quarter = d.RpContractCompletionByQuarter.Quarter,
                ContractTypeCode = d.CaContractDraftVendor.ContractTypeCode.Value.ToString(),
            })
            .ToListAsync(ct);

        var quarterData = rawDetails
            .Select(d => new
            {
                d.Quarter,
                Code = d.ContractTypeCode.Equals(ContractRentalTypeConstant.Rent, StringComparison.Ordinal)
                    ? ContractTypeConstant.Rent
                    : d.ContractTypeCode,
            })
            .ToList();

        var completionContractYear = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.BuildContractDetail(quarterData.Select(d => (d.Quarter, d.Code)), null);
        var q1 = entity.Quarter >= 1 ? ContractCompletionByQuarterEndpoint<TRequest, TResponse>.BuildContractDetail(quarterData.Select(d => (d.Quarter, d.Code)), 1) : null;
        var q2 = entity.Quarter >= 2 ? ContractCompletionByQuarterEndpoint<TRequest, TResponse>.BuildContractDetail(quarterData.Select(d => (d.Quarter, d.Code)), 2) : null;
        var q3 = entity.Quarter >= 3 ? ContractCompletionByQuarterEndpoint<TRequest, TResponse>.BuildContractDetail(quarterData.Select(d => (d.Quarter, d.Code)), 3) : null;
        var q4 = entity.Quarter >= 4 ? ContractCompletionByQuarterEndpoint<TRequest, TResponse>.BuildContractDetail(quarterData.Select(d => (d.Quarter, d.Code)), 4) : null;

        var contractTypeSummaryRows = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.BuildContractTypeSummaryRows(quarterData.Select(d => (d.Quarter, d.Code)));

        var acceptorDate =
             entity.Status == RpContractCompletionByQuarterStatus.WaitingApproval
                 ? entity.DocumentDate.ToThaiDateString() ?? DateTimeOffset.UtcNow.ToThaiDateString()
                 : null;

        var result = new ReportContractCompletionByQuarterReplaceDto(
            entity.Id.Value,
            entity.DocumentNumber,
            entity.DocumentDate.ToThaiDateString(includeBuddhistEra: false),
            acceptorDate,
            entity.Year,
            entity.Quarter,
            entity.SignStartDate.ToThaiDateString(includeBuddhistEra: false),
            entity.SignEndDate.ToThaiDateString(includeBuddhistEra: false),
            entity.Status,
            details,
            acceptorsReplace,
            creatorReplace,
            q1,
            q2,
            q3,
            q4,
            completionContractYear,
            contractTypeSummaryRows);

        return result;
    }

    private static ContractDetailReplaceDto BuildContractDetail(IEnumerable<(int Quarter, string Code)> data, int? filterQuarter)
    {
        var filtered = (filterQuarter.HasValue
            ? data.Where(d => d.Quarter == filterQuarter.Value)
            : data).ToList();

        var total = filtered.Count;

        ContractTypeDetailReplaceDto BuildType(string? code, string label)
        {
            var count = code is null ? total : filtered.Count(d => d.Code.Equals(code, StringComparison.Ordinal));
            var percent = total > 0 ? ((double)count / total * 100).ToString("F2") + "%" : "0%";
            return new ContractTypeDetailReplaceDto(label, count, percent);
        }

        var details = new[]
        {
            BuildType(ContractTypeConstant.Buy, "2.สัญญาซื้อขาย"),
            BuildType(ContractTypeConstant.Hire, "3.สัญญาจ้าง"),
            BuildType(ContractTypeConstant.Rent, "4.สัญญาเช่า"),
        }.Where(t => t.Count > 0).ToArray();

        return new ContractDetailReplaceDto(BuildType(null, "รวมสัญญาทั้งสิ้น"), details);
    }

    private static IEnumerable<ContractTypeSummaryRowReplaceDto> BuildContractTypeSummaryRows(IEnumerable<(int Quarter, string Code)> data)
    {
        var list = data.ToList();
        var yearTotal = list.Count;

        string Percent(int count, int total) =>
            total > 0 ? ((double)count / total * 100).ToString("F2") + "%" : "0%";

        int CountByQuarterAndCode(int q, string code) =>
            list.Count(d => d.Quarter == q && d.Code.Equals(code, StringComparison.Ordinal));

        int TotalByQuarter(int q) => list.Count(d => d.Quarter == q);

        var contractTypes = new[]
        {
            (Code: ContractTypeConstant.Buy,  Name: "สัญญาซื้อขาย"),
            (Code: ContractTypeConstant.Hire, Name: "สัญญาจ้าง"),
            (Code: ContractTypeConstant.Rent, Name: "สัญญาเช่า"),
        };

        var rows = new List<ContractTypeSummaryRowReplaceDto>();

        var q1Total = TotalByQuarter(1);
        var q2Total = TotalByQuarter(2);
        var q3Total = TotalByQuarter(3);
        var q4Total = TotalByQuarter(4);

        foreach (var ct in contractTypes)
        {
            var q1Count = CountByQuarterAndCode(1, ct.Code);
            var q2Count = CountByQuarterAndCode(2, ct.Code);
            var q3Count = CountByQuarterAndCode(3, ct.Code);
            var q4Count = CountByQuarterAndCode(4, ct.Code);
            var totalCount = q1Count + q2Count + q3Count + q4Count;

            if (totalCount == 0)
            {
                continue;
            }

            rows.Add(new ContractTypeSummaryRowReplaceDto(
                ct.Name,
                q1Count,
                Percent(q1Count, q1Total),
                q2Count,
                Percent(q2Count, q2Total),
                q3Count,
                Percent(q3Count, q3Total),
                q4Count,
                Percent(q4Count, q4Total),
                totalCount,
                Percent(totalCount, yearTotal)));
        }

        return rows;
    }

    private static IEnumerable<ReportContractCompletionByQuarterDetailReplaceDto> MapDetails(RpContractCompletionByQuarter entity)
    {
        var overDueDay = 30;

        return entity.Details.Select(d =>
        {
            return new ReportContractCompletionByQuarterDetailReplaceDto(
                d.Id.Value,
                d.CaContractDraftVendor.Id.Value,
                d.CaContractDraftVendor.ContractTypeCode.HasValue ? d.CaContractDraftVendor.ContractTypeCode.Value.ToString() : string.Empty,
                (d.CaContractDraftVendor.ContractType != null ? d.CaContractDraftVendor.ContractType?.Label : string.Empty)!,
                d.CaContractDraftVendor.ContractNumber,
                d.CaContractDraftVendor.ContractName,
                d.CaContractDraftVendor.ContractSignedDate.ToThaiDateString(),
                d.CaContractDraftVendor.Vendor.VendorInfo.EstablishmentName,
                d.CaContractDraftVendor.Budget.ToCurrencyStringWithComma(),
                d.CaContractDraftVendor.Budget.ThaiBahtText(),
                d.Description,
                GetOverdue(d.CaContractDraftVendor.ContractSignedDate));

            bool GetOverdue(DateTimeOffset? date) =>
                date.HasValue &&
                (DateTimeOffset.UtcNow - date.Value).TotalDays > overDueDay;
        });
    }

    private ContractDetailReplaceDto GetCompletionContractYear(RpContractCompletionByQuarter entity)
    {
        var allDetails = entity.Details
            .Where(d =>
                d.CaContractDraftVendor.ContractSignedDate.HasValue &&
                ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetQuarterFromDate(d.CaContractDraftVendor.ContractSignedDate.Value) <= entity.Quarter)
            .ToList();
        var total = allDetails.Count;
        var summary = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(allDetails, total, null, "รวมสัญญาทั้งสิ้น");
        var buyType = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(allDetails, total, ContractTypeConstant.Buy, "สัญญาซื้อขาย");
        var hireType = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(allDetails, total, ContractTypeConstant.Hire, "สัญญาจ้าง");
        var rentType = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(allDetails, total, ContractTypeConstant.Rent, "สัญญาเช่า");

        return new ContractDetailReplaceDto(summary, [buyType, hireType, rentType]);
    }

    private ContractDetailReplaceDto GetCompletionContractForQuarter(RpContractCompletionByQuarter entity, int quarter)
    {
        var quarterDetails = entity.Details
            .Where(d =>
                d.CaContractDraftVendor.ContractSignedDate.HasValue &&
                ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetQuarterFromDate(d.CaContractDraftVendor.ContractSignedDate.Value) == quarter)
            .ToList();

        var total = quarterDetails.Count;
        var summary = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(quarterDetails, total, null, "รวมสัญญาทั้งสิ้น");
        var buyType = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(quarterDetails, total, ContractTypeConstant.Buy, "สัญญาซื้อขาย");
        var hireType = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(quarterDetails, total, ContractTypeConstant.Hire, "สัญญาจ้าง");
        var rentType = ContractCompletionByQuarterEndpoint<TRequest, TResponse>.GetContractTypeDetailByDetails(quarterDetails, total, ContractTypeConstant.Rent, "สัญญาเช่า");

        return new ContractDetailReplaceDto(summary, [buyType, hireType, rentType]);
    }

    private static int GetQuarterFromDate(DateTimeOffset date) => ((date.Month - 1) / 3) + 1;

    private static string NormalizeContractTypeCode(RpContractCompletionByQuarterDetail detail)
    {
        var code = detail.CaContractDraftVendor.ContractTypeCode?.Value.ToString() ?? string.Empty;
        return code.Equals(ContractRentalTypeConstant.Rent, StringComparison.Ordinal)
            ? ContractTypeConstant.Rent
            : code;
    }

    private static ContractTypeDetailReplaceDto GetContractTypeDetailByDetails(
        List<RpContractCompletionByQuarterDetail> details,
        int total,
        string? contractTypeCode,
        string label)
    {
        var count = contractTypeCode is null
            ? details.Count
            : details.Count(d => NormalizeContractTypeCode(d).Equals(contractTypeCode, StringComparison.Ordinal));

        var percent = total > 0
            ? ((double)count / total * 100).ToString("F2") + "%"
            : "0%";

        return new ContractTypeDetailReplaceDto(label, count, percent);
    }

    private async Task<CreatorReplace?> GetCreatorReplaceAsync(
        RpContractCompletionByQuarter entity,
        UserId? creatorUserId,
        CancellationToken ct = default)
    {
        var sendToCommitteeApproveByUser =
            creatorUserId is not null
                ? await this.dbContext.SuUsers
                            .Include(suUser => suUser.Employee)
                            .ThenInclude(rawEmployee => rawEmployee.View)
                            .FirstOrDefaultAsync(u => u.Id == creatorUserId, ct)
                : await this.GetLastActivityCreatedByAsync(
                    entity.Id.ToString(),
                    ActivityLogActionTypeConstant.SendApprove,
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

    private AcceptorReplace[] GetAcceptorReplace(RpContractCompletionByQuarter entity)
    {
        AcceptorReplace[] acceptors =
            [.. entity.Acceptors
                  .Where(a => a.Type == AcceptorType.Approver)
                  .Select(DelegatorExtensions.DelegatorToAcceptor)
                  .Map(this.MapAcceptorReplace)
                  .OrderBy(a => a.Sequence)];

        if (acceptors.Any())
        {
            acceptors[^1] =
                acceptors.Last() with { Action = "อนุมัติ" };
        }

        return [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
    }

    private AcceptorReplace MapAcceptorReplace(RpContractCompletionByQuarterAcceptor acceptor)
    {
        return new AcceptorReplace(
            acceptor.UserId.Value,
            acceptor.Sequence,
            "เห็นชอบ",
            acceptor.User.FullName,
            acceptor.FullName,
            acceptor.User.Employee.View?.FullPositionName ?? string.Empty,
            string.Empty,
            string.Empty,
            acceptor.Status);
    }

    protected async Task UpsertAttachments(RpContractCompletionByQuarter entity, AttachmentsDtoWithId[] attachments)
    {
        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => new
                       {
                           f.Id,
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic,
                       }))
                       .ToArray();

        var incomingFileIds = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();
        var existingFileIds = entity.Attachments.Select(a => a.FileId).ToHashSet();

        var removedAttachments = entity.Attachments
                                      .Where(a => !incomingFileIds.Contains(a.FileId))
                                      .ToArray();

        foreach (var attachment in removedAttachments)
        {
            entity.RemoveAttachment(attachment);
            await this.fileServiceClient.DeleteAsync(attachment.FileId, CancellationToken.None);
        }

        if (removedAttachments.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(entity.Status),
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        var newFiles = fileList.Where(f => !existingFileIds.Contains(FileId.From(f.FileId))).ToArray();

        newFiles.Map(f => RpContractCompletionByQuarterAttachment.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        if (newFiles.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", newFiles.Select(f => f.FileName))));
        }

        foreach (var existing in entity.Attachments)
        {
            var match = fileList.FirstOrDefault(f => FileId.From(f.FileId) == existing.FileId);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }
}