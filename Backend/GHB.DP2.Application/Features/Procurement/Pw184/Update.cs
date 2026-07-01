namespace GHB.DP2.Application.Features.Procurement.Pw184;

using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.Pw184.Abstract;
using GHB.DP2.Application.Validators;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdatePw184Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Pw184Status? Status,
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
    Pw184VendorDto[]? Vendors,
    Pw184GLAccountDto[]? GLAccounts,
    Pw184CommitteeDto[]? Committees,
    AcceptorRequest[]? Acceptors,
    AcceptorRequest[]? AcceptanceConfirmers,
    AttachmentsDtoWithId[] Attachments);

public class UpdatePw184RequestValidator : Validator<UpdatePw184Request>
{
    public UpdatePw184RequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class UpdatePw184Endpoint : Pw184EndpointBase<UpdatePw184Request, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdatePw184Endpoint(ILogger<UpdatePw184Endpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw184")
             .WithName("UpdatePw184")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<UpdatePw184Request>("application/json"));
        this.Put("pw184/{Id:guid}");
        this.AuditLog("รายการ ว 184", "แก้ไขรายการ ว 184");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(
        UpdatePw184Request req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.Pw184s
                               .Include(p => p.Vendors).ThenInclude(v => v.VendorParcels)
                               .Include(p => p.GLAccounts)
                               .Include(p => p.Committees)
                               .Include(p => p.Acceptors).ThenInclude(a => a.User).ThenInclude(u => u.Employee)
                               .Include(p => p.Attachments)
                               .SingleOrDefaultAsync(p => p.Id == Pw184Id.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการ ว 184");
        }

        entity.SetPw184Date(req.Pw184Date)
              .SetDocumentDate(req.Pw184Date)
              .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
              .SetBudgetYear(req.BudgetYear)
              .SetSupplyMethod(
                  ParameterCode.From(req.SupplyMethodCode),
                  req.SupplyMethodSpecialTypeCode != null ? ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null)
              .SetSubject(req.Subject)
              .SetSource(req.Source)
              .SetReason(req.Reason)
              .SetBudget(req.Budget)
              .SetIsAdvance(req.IsAdvance)
              .SetAdvanceName(req.AdvanceName)
              .SetAdvancePayment(
                  req.AdvancePaymentMethodCode != null ? ParameterCode.From(req.AdvancePaymentMethodCode) : null,
                  req.AdvancePaymentDate)
              .SetAdvanceBank(
                  req.AdvanceBankCode != null ? ParameterCode.From(req.AdvanceBankCode) : null,
                  req.AdvanceBankAccount,
                  req.AdvanceBankBranch,
                  req.AdvanceBankAccountName)
              .SetAdvanceDetail(req.AdvanceDetail);

        if (req.DisbursementDate.HasValue && req.DisbursementAmount.HasValue)
        {
            entity.SetDisbursement(req.DisbursementDate, req.DisbursementAmount, req.DisbursementDescription);
        }

        UpdateVendors(entity, req.Vendors);
        UpdateGLAccounts(entity, req.GLAccounts);
        await this.UpdateCommitteesAsync(entity, req.Committees, ct);

        var isTransitionToPaid = req.Status == Pw184Status.Paid
                                 && entity.Status == Pw184Status.WaitingDisbursementDate;
        var isDisbursementStage = entity.Status is Pw184Status.WaitingDisbursementDate or Pw184Status.Paid;
        var shouldIncludeConfirmers = (isDisbursementStage || isTransitionToPaid)
                                     && req.AcceptanceConfirmers is { Length: > 0 };

        var acceptorRequest = shouldIncludeConfirmers
            ? [.. req.Acceptors ?? [], .. req.AcceptanceConfirmers ?? []]
            : req.Acceptors;
        await this.UpdateAcceptorsAsync(entity, acceptorRequest, ct);
        await this.SyncInspectionCommitteeAcceptorsAsync(entity, ct);
        await this.UpsertAttachments(entity, req.Attachments);

        // ── Status transition via update ──────────────────────────────────────
        if (req.Status.HasValue && req.Status.Value != entity.Status)
        {
            this.ApplyStatusTransition(entity, req.Status.Value);
        }
        else
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                ActivityLogActionTypeConstant.Update,
                entity.Status.ToString()));
        }

        this.dbContext.Pw184s.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Status transition via update
    // ──────────────────────────────────────────────────────────────────────────
    private static readonly Dictionary<(Pw184Status From, Pw184Status To), bool> AllowedTransitions = new()
    {
        { (Pw184Status.Draft, Pw184Status.WaitingApproval), true },
        { (Pw184Status.Edit, Pw184Status.WaitingApproval), true },
        { (Pw184Status.Rejected, Pw184Status.WaitingApproval), true },
        { (Pw184Status.WaitingApproval, Pw184Status.Edit), true },
        { (Pw184Status.WaitingDisbursementDate, Pw184Status.Paid), true },
    };

    private void ApplyStatusTransition(Pw184 entity, Pw184Status newStatus)
    {
        var currentStatus = entity.Status;

        if (!AllowedTransitions.ContainsKey((currentStatus, newStatus)))
        {
            this.ThrowError(
                $"ไม่สามารถเปลี่ยนสถานะจาก {currentStatus} เป็น {newStatus} ได้",
                StatusCodes.Status400BadRequest);
        }

        switch ((currentStatus, newStatus))
        {
            case (Pw184Status.Draft or Pw184Status.Edit or Pw184Status.Rejected, Pw184Status.WaitingApproval):
                entity.Acceptors
                     .Where(a => a.Type == AcceptorType.Approver)
                     .Iter(a => a.Pending());

                var first = entity.Acceptors
                                  .Where(a => a.Type == AcceptorType.Approver && !a.IsDeleted)
                                  .OrderBy(a => a.Sequence)
                                  .FirstOrDefault();

                first?.SetIsCurrent(true);

                entity.SetStatus(Pw184Status.WaitingApproval);

                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.SendApprove,
                    ActivityLogActionTypeConstant.SendApprove,
                    nameof(Pw184Status.WaitingApproval)));
                break;

            case (Pw184Status.WaitingApproval, Pw184Status.Edit):
                if (entity.Acceptors.Where(a => a.Type == AcceptorType.Approver)
                         .Any(a => a.Status is AcceptorStatus.Approved or AcceptorStatus.Rejected))
                {
                    this.ThrowError(
                        "ไม่สามารถเรียกคืนได้ เนื่องจากผู้มีอำนาจได้ดำเนินการแล้ว",
                        StatusCodes.Status403Forbidden);
                }

                entity.Acceptors
                     .Where(a => a.Type == AcceptorType.Approver)
                     .Iter(a => a.Draft());

                entity.SetStatus(Pw184Status.Edit);

                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Recall,
                    ActivityLogActionTypeConstant.Recall,
                    nameof(Pw184Status.Edit)));
                break;

            case (Pw184Status.WaitingDisbursementDate, Pw184Status.Paid):
                entity.SetStatus(Pw184Status.Paid);

                entity.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.ConfirmDisbursement,
                    ActivityLogActionTypeConstant.ConfirmDisbursement,
                    nameof(Pw184Status.Paid)));
                break;
        }
    }

    private static void UpdateVendors(Pw184 entity, Pw184VendorDto[]? vendors)
    {
        if (vendors is null)
        {
            return;
        }

        entity.AddVendors(vendors.Map(vDto =>
        {
            var existing = vDto.Id.HasValue
                ? entity.Vendors.FirstOrDefault(v => v.Id == Pw184VendorId.From(vDto.Id.Value))
                : null;

            if (existing is not null)
            {
                existing.SetSequence(vDto.Sequence)
                        .SetVendorType(vDto.VendorType)
                        .SetVendor(
                            vDto.SuVendorId.HasValue ? SuVendorId.From(vDto.SuVendorId.Value) : null,
                            vDto.TaxNumber,
                            vDto.VendorName,
                            vDto.VendorBranchNumber)
                        .SetBill(
                            vDto.VatIncludeTypeCode != null ? ParameterCode.From(vDto.VatIncludeTypeCode) : null,
                            ParameterCode.From(vDto.BillTypeCode),
                            vDto.BillTypeOther,
                            vDto.BillBookNo,
                            vDto.BillDate,
                            vDto.BillDetail);

                if (vDto.VendorParcels != null)
                {
                    UpdateVendorParcels(existing, vDto.VendorParcels);
                }

                return existing;
            }

            var newVendor = Pw184Vendor.Create(entity.Id)
                                       .SetSequence(vDto.Sequence)
                                       .SetVendorType(vDto.VendorType)
                                       .SetVendor(
                                           vDto.SuVendorId.HasValue ? SuVendorId.From(vDto.SuVendorId.Value) : null,
                                           vDto.TaxNumber,
                                           vDto.VendorName,
                                           vDto.VendorBranchNumber)
                                       .SetBill(
                                           vDto.VatIncludeTypeCode != null ? ParameterCode.From(vDto.VatIncludeTypeCode) : null,
                                           ParameterCode.From(vDto.BillTypeCode),
                                           vDto.BillTypeOther,
                                           vDto.BillBookNo,
                                           vDto.BillDate,
                                           vDto.BillDetail);

            if (vDto.VendorParcels != null)
            {
                foreach (var vpDto in vDto.VendorParcels)
                {
                    var parcel = Pw184VendorParcel.Create(newVendor.Id)
                                                   .SetSequence(vpDto.Sequence)
                                                   .SetItem(vpDto.Item, vpDto.ItemDetail)
                                                   .SetPrice(vpDto.Quantity, ParameterCode.From(vpDto.UnitCode), vpDto.UnitPrice, vpDto.TotalPrice, vpDto.TotalPriceVat)
                                                   .SetVatIncludeType(vpDto.VatIncludeTypeCode != null ? ParameterCode.From(vpDto.VatIncludeTypeCode) : null);

                    newVendor.AddVendorParcels(parcel);
                }
            }

            return newVendor;
        }));
    }

    private static void UpdateVendorParcels(Pw184Vendor vendor, IEnumerable<Pw184VendorParcelDto> parcels)
    {
        vendor.UpdateVendorsParcels(parcels.Map(pDto =>
        {
            var existing = pDto.Id.HasValue
                ? vendor.VendorParcels.FirstOrDefault(p => p.Id == Pw184VendorParcelId.From(pDto.Id.Value))
                : null;

            if (existing is not null)
            {
                return existing.SetSequence(pDto.Sequence)
                               .SetItem(pDto.Item, pDto.ItemDetail)
                               .SetPrice(pDto.Quantity, ParameterCode.From(pDto.UnitCode), pDto.UnitPrice, pDto.TotalPrice, pDto.TotalPriceVat)
                               .SetVatIncludeType(pDto.VatIncludeTypeCode != null ? ParameterCode.From(pDto.VatIncludeTypeCode) : null);
            }

            return Pw184VendorParcel.Create(vendor.Id)
                                    .SetSequence(pDto.Sequence)
                                    .SetItem(pDto.Item, pDto.ItemDetail)
                                    .SetPrice(pDto.Quantity, ParameterCode.From(pDto.UnitCode), pDto.UnitPrice, pDto.TotalPrice, pDto.TotalPriceVat)
                                    .SetVatIncludeType(pDto.VatIncludeTypeCode != null ? ParameterCode.From(pDto.VatIncludeTypeCode) : null);
        }));
    }

    private static void UpdateGLAccounts(Pw184 entity, Pw184GLAccountDto[]? glAccounts)
    {
        if (glAccounts is null)
        {
            return;
        }

        entity.AddGLAccounts(glAccounts.Map(gDto =>
        {
            var existing = gDto.Id.HasValue
                ? entity.GLAccounts.FirstOrDefault(g => g.Id == Pw184GLAccountId.From(gDto.Id.Value))
                : null;

            if (existing is not null)
            {
                return existing.SetGLAccount(
                    gDto.Sequence,
                    gDto.SolId,
                    ParameterCode.From(gDto.BudgetTypeCode),
                    ParameterCode.From(gDto.GLAccountCode),
                    gDto.ProjectNumber,
                    gDto.Amount);
            }

            return Pw184GLAccount.Create(entity.Id)
                                  .SetGLAccount(
                                      gDto.Sequence,
                                      gDto.SolId,
                                      ParameterCode.From(gDto.BudgetTypeCode),
                                      ParameterCode.From(gDto.GLAccountCode),
                                      gDto.ProjectNumber,
                                      gDto.Amount);
        }));
    }

    private async Task UpdateCommitteesAsync(Pw184 entity, Pw184CommitteeDto[]? committees, CancellationToken ct)
    {
        if (committees is null)
        {
            return;
        }

        var newUserIds = committees
                         .Where(c => !c.Id.HasValue)
                         .Select(c => UserId.From(c.UserId))
                         .ToArray();

        var users = newUserIds.Length > 0
            ? await this.dbContext.SuUsers
                        .Include(u => u.Employee).ThenInclude(e => e.View)
                        .Where(u => newUserIds.Contains(u.Id))
                        .ToArrayAsync(ct)
            : [];

        entity.AddCommittees(committees.Map(cDto =>
        {
            var existing = cDto.Id.HasValue
                ? entity.Committees.FirstOrDefault(c => c.Id == Pw184CommitteeId.From(cDto.Id.Value))
                : null;

            if (existing is not null)
            {
                return existing.Update(
                    cDto.FullName,
                    cDto.FullPositionName,
                    ParameterCode.From(cDto.CommitteePositionsCode),
                    cDto.CommitteePositionsName ?? string.Empty,
                    cDto.GroupType,
                    cDto.Sequence);
            }

            return Pw184Committee.Create(
                entity.Id,
                cDto.GroupType,
                UserId.From(cDto.UserId),
                cDto.FullName,
                cDto.FullPositionName,
                ParameterCode.From(cDto.CommitteePositionsCode),
                cDto.CommitteePositionsName ?? string.Empty,
                cDto.Sequence);
        }));
    }

    private async Task SyncInspectionCommitteeAcceptorsAsync(Pw184 entity, CancellationToken ct)
    {
        entity.Acceptors
              .Where(a => a.Type == AcceptorType.InspectionCommittee)
              .ToList()
              .Iter(a => entity.RemoveAcceptor(a));

        var inspectionMembers = entity.Committees
                                       .Where(c => c.GroupType == Pw184CommitteeGroupType.InspectionCommittee)
                                       .OrderBy(c => c.Sequence)
                                       .ToArray();

        if (inspectionMembers.Length == 0)
        {
            return;
        }

        var userIds = inspectionMembers.Select(c => c.SuUserId).ToArray();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee).ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        foreach (var member in inspectionMembers)
        {
            var user = users.FirstOrDefault(u => u.Id == member.SuUserId);
            if (user is null)
            {
                continue;
            }

            entity.AddAcceptor(Pw184Acceptor.Create(AcceptorType.InspectionCommittee, user, member.Sequence));
        }
    }

    private async Task UpdateAcceptorsAsync(Pw184 entity, AcceptorRequest[]? acceptors, CancellationToken ct)
    {
        if (acceptors is null)
        {
            return;
        }

        var userIds = acceptors
                      .Where(a => !a.Id.HasValue)
                      .Select(a => UserId.From(a.UserId))
                      .ToArray();

        var users = userIds.Length > 0
            ? await this.dbContext.SuUsers
                        .Include(u => u.Employee).ThenInclude(e => e.View)
                        .Where(u => userIds.Contains(u.Id))
                        .ToArrayAsync(ct)
            : [];

        // Remove acceptors not in the incoming list
        _ = entity.Acceptors
                  .ExceptBy(
                      acceptors.Where(a => a.Id.HasValue).Select(a => a.Id!.Value),
                      a => a.Id.Value)
                  .Map(entity.RemoveAcceptor)
                  .ToHashSet();

        // Update existing
        _ = acceptors.Where(a => a.Id.HasValue)
                     .Join(
                         entity.Acceptors,
                         req => req.Id!.Value,
                         existing => existing.Id.Value,
                         (req, existing) => new { req, existing })
                     .Iter(x => x.existing.SetSequence(x.req.Sequence));

        // Add new
        _ = acceptors.Where(a => !a.Id.HasValue)
                     .Join(
                         users,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => Pw184Acceptor.Create(a.AcceptorType, u, a.Sequence))
                     .Iter(a => entity.AddAcceptor(a));
    }
}
