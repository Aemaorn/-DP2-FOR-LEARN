namespace GHB.DP2.Application.Features.Procurement.Pw184.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class Pw184EndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;

    protected Pw184EndpointBase(ILogger logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<Pw184> GetPw184ById(Pw184Id id, CancellationToken ct)
    {
        var data = await this.dbContext.Pw184s
                             .Include(p => p.Vendors)
                             .ThenInclude(v => v.VendorParcels)
                             .Include(p => p.GLAccounts)
                             .ThenInclude(gl => gl.GLAccount)
                             .Include(p => p.GLAccounts)
                             .ThenInclude(gl => gl.BudgetType)
                             .Include(p => p.Committees)
                             .ThenInclude(c => c.User)
                             .ThenInclude(u => u.Employee)
                             .Include(p => p.Acceptors)
                             .ThenInclude(a => a.User)
                             .ThenInclude(u => u.Employee)
                             .Include(p => p.Attachments)
                             .Include(p => p.Department)
                             .Include(p => p.SupplyMethod)
                             .Include(p => p.SupplyMethodSpecialType)
                             .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (data is null)
        {
            this.ThrowError($"Pw184 with ID {id} not found.", StatusCodes.Status404NotFound);
        }

        return data;
    }

    protected GetPw184Response MapToResponse(Pw184 data)
    {
        return new GetPw184Response(
            data.Id.Value,
            data.Pw184Number.Value,
            data.Status,
            data.Pw184Date,
            data.DepartmentId.Value,
            data.BudgetYear,
            data.SupplyMethodCode.Value,
            data.SupplyMethodSpecialTypeCode?.Value,
            data.Subject,
            data.Source,
            data.Reason,
            data.Budget,
            data.IsAdvance,
            data.AdvanceName,
            data.AdvancePaymentMethodCode?.Value,
            data.AdvancePaymentDate,
            data.AdvanceBankCode?.Value,
            data.AdvanceBankAccount,
            data.AdvanceBankBranch,
            data.AdvanceBankAccountName,
            data.AdvanceDetail,
            data.DisbursementDate,
            data.DisbursementAmount,
            data.DisbursementDescription,
            data.CurrentCommitteeSequence,
            data.AuditInfo.CreatedBy,
            data.Vendors
                .OrderBy(v => v.Sequence)
                .Select(v => new Pw184VendorDto(
                    v.Id.Value,
                    v.VendorType,
                    v.SuVendorId?.Value,
                    v.VendorName,
                    v.Sequence,
                    v.TaxNumber,
                    v.VendorBranchNumber,
                    v.VatIncludeTypeCode?.Value,
                    v.BillTypeCode.Value,
                    v.BillTypeOther,
                    v.BillBookNo,
                    v.BillDate,
                    v.BillDetail,
                    v.VendorParcels
                      .OrderBy(vp => vp.Sequence)
                      .Select(vp => new Pw184VendorParcelDto(
                          vp.Id.Value,
                          vp.Sequence,
                          vp.Item,
                          vp.ItemDetail,
                          vp.Quantity,
                          vp.UnitCode.Value,
                          vp.UnitPrice,
                          vp.TotalPrice,
                          vp.TotalPriceVat,
                          vp.VatIncludeTypeCode?.Value)))),
            data.GLAccounts
                .OrderBy(gl => gl.Sequence)
                .Select(gl => new Pw184GLAccountDto(
                    gl.Id.Value,
                    gl.Sequence,
                    gl.SoId,
                    gl.BudgetTypeCode.Value,
                    gl.GLAccountCode.Value,
                    gl.ProjectNumber,
                    gl.Amount)),
            data.Committees
                .OrderBy(c => c.GroupType)
                .ThenBy(c => c.Sequence)
                .Select(c => new Pw184CommitteeDto(
                    c.Id.Value,
                    c.GroupType,
                    c.SuUserId.Value,
                    c.FullName,
                    c.FullPositionName,
                    c.CommitteePositionsCode.Value,
                    c.CommitteePositionsName,
                    c.Sequence)),
            [
                .. data.Acceptors
                       .Where(a => !a.IsDeleted)
                       .OrderBy(a => a.Sequence)
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
                           IsCurrent: a.IsCurrent,
                           DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ],
            [
                .. data.Acceptors
                       .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingConfirmer)
                       .OrderBy(a => a.Sequence)
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
                           IsCurrent: a.IsCurrent,
                           DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ],
            [
                .. data.Attachments
                       .GroupBy(
                           a => a.DocumentTypeCode,
                           (key, g) => new AttachmentsDto(
                               key.Value,
                               [.. g.Select(s => new FileAttachments(s.Id.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))
            ]);
    }

    protected async Task UpsertAttachments(Pw184 entity, AttachmentsDtoWithId[] attachments)
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
        var existingFileIds = entity.Attachments.Select(a => a.Id).ToHashSet();

        var removedAttachments = entity.Attachments
                                       .Where(a => !incomingFileIds.Contains(a.Id))
                                       .ToArray();

        foreach (var attachment in removedAttachments)
        {
            entity.RemoveAttachment(attachment);
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

        newFiles.Select(f => Pw184Attachments.Create(
                    ParameterCode.From(f.DocumentTypeCode),
                    FileId.From(f.FileId),
                    f.FileName,
                    f.Sequence,
                    f.IsPublic))
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
            var match = fileList.FirstOrDefault(f => FileId.From(f.FileId) == existing.Id);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }

        await Task.CompletedTask;
    }
}

// ─── DTOs ────────────────────────────────────────────────────────────────────
public record Pw184VendorDto(
    Guid? Id,
    string VendorType,
    Guid? SuVendorId,
    string VendorName,
    int Sequence,
    string? TaxNumber,
    string? VendorBranchNumber,
    string? VatIncludeTypeCode,
    string BillTypeCode,
    string? BillTypeOther,
    string? BillBookNo,
    DateTimeOffset? BillDate,
    string? BillDetail,
    IEnumerable<Pw184VendorParcelDto> VendorParcels);

public record Pw184VendorParcelDto(
    Guid? Id,
    int Sequence,
    string Item,
    string? ItemDetail,
    int Quantity,
    string UnitCode,
    decimal UnitPrice,
    decimal TotalPrice,
    decimal TotalPriceVat,
    string? VatIncludeTypeCode);

public record Pw184GLAccountDto(
    Guid? Id,
    int Sequence,
    string SolId,
    string BudgetTypeCode,
    string GLAccountCode,
    string? ProjectNumber,
    decimal Amount);

public record Pw184CommitteeDto(
    Guid? Id,
    Pw184CommitteeGroupType GroupType,
    Guid UserId,
    string FullName,
    string FullPositionName,
    string CommitteePositionsCode,
    string CommitteePositionsName,
    int Sequence);

public record GetPw184Response(
    Guid Id,
    string Pw184Number,
    Pw184Status Status,
    DateTimeOffset Pw184Date,
    string DepartmentCode,
    int BudgetYear,
    string SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    string Subject,
    string Source,
    string? Reason,
    decimal Budget,
    bool IsAdvance,
    string? AdvanceName,
    string? AdvancePaymentMethodCode,
    DateTimeOffset? AdvancePaymentDate,
    string? AdvanceBankCode,
    string? AdvanceBankAccount,
    string? AdvanceBankBranch,
    string? AdvanceBankAccountName,
    string? AdvanceDetail,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementDescription,
    int CurrentCommitteeSequence,
    Guid CreatedBy,
    IEnumerable<Pw184VendorDto> Vendors,
    IEnumerable<Pw184GLAccountDto> GLAccounts,
    IEnumerable<Pw184CommitteeDto> Committees,
    AcceptorResponse[] Acceptors,
    AcceptorResponse[] AcceptanceConfirmers,
    AttachmentsDto[] Attachments);
