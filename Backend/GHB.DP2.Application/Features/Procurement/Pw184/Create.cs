namespace GHB.DP2.Application.Features.Procurement.Pw184;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
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

public record CreatePw184Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
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
    Pw184VendorDto[]? Vendors,
    Pw184GLAccountDto[]? GLAccounts,
    Pw184CommitteeDto[]? Committees,
    AcceptorRequest[]? Acceptors,
    AttachmentsDto[] Attachments);

public class CreatePw184RequestValidator : Validator<CreatePw184Request>
{
    public CreatePw184RequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class CreatePw184Endpoint : Pw184EndpointBase<CreatePw184Request, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePw184Endpoint(ILogger<CreatePw184Endpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw184")
             .WithName("CreatePw184")
             .Produces<Created<Guid>>()
             .Accepts<CreatePw184Request>("application/json"));
        this.Post("Pw184");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreatePw184Request req, CancellationToken ct)
    {
        var pw184Number = await this.GeneratePw184NumberAsync(req.BudgetYear, ct);
        var pw184 = BuildFromRequest(req, pw184Number);

        if (req.Acceptors != null && req.Acceptors.Length > 0)
        {
            await this.AddAcceptorsAsync(pw184, req.Acceptors, ct);
        }

        if (req.Committees != null && req.Committees.Length > 0)
        {
            await this.AddCommitteesAsync(pw184, req.Committees, ct);
            await this.SyncInspectionCommitteeAcceptorsAsync(pw184, ct);
        }

        if (req.Attachments != null && req.Attachments.Length > 0)
        {
            AddAttachments(pw184, req.Attachments);
        }

        this.dbContext.Pw184s.Add(pw184);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, pw184.Id.Value);
    }

    private async Task<Pw184Number> GeneratePw184NumberAsync(int budgetYear, CancellationToken ct)
    {
        var last = await this.dbContext.Pw184s
                             .IgnoreQueryFilters()
                             .Where(p => p.BudgetYear == budgetYear && !p.IsDeleted)
                             .OrderByDescending(p => p.Pw184Number)
                             .FirstOrDefaultAsync(ct);

        return last is null ? Pw184Number.New(budgetYear) : last.Pw184Number.Next();
    }

    private static Pw184 BuildFromRequest(CreatePw184Request req, Pw184Number number)
    {
        var pw184 = Pw184.Create(number)
                         .SetPw184Date(req.Pw184Date)
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

        if (req.Vendors != null)
        {
            foreach (var v in req.Vendors)
            {
                AddVendor(pw184, v);
            }
        }

        if (req.GLAccounts != null)
        {
            foreach (var gl in req.GLAccounts)
            {
                AddGLAccount(pw184, gl);
            }
        }

        return pw184;
    }

    private async Task AddAcceptorsAsync(Pw184 pw184, AcceptorRequest[] acceptors, CancellationToken ct)
    {
        var userIds = acceptors.Select(a => UserId.From(a.UserId)).ToArray();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee).ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        acceptors
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => Pw184Acceptor.Create(a.AcceptorType, u, a.Sequence))
            .Iter(a => pw184.AddAcceptor(a));
    }

    private async Task SyncInspectionCommitteeAcceptorsAsync(Pw184 pw184, CancellationToken ct)
    {
        var inspectionMembers = pw184.Committees
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

            pw184.AddAcceptor(Pw184Acceptor.Create(AcceptorType.InspectionCommittee, user, member.Sequence));
        }
    }

    private async Task AddCommitteesAsync(Pw184 pw184, Pw184CommitteeDto[] committees, CancellationToken ct)
    {
        var userIds = committees.Select(c => UserId.From(c.UserId)).ToArray();
        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        foreach (var c in committees)
        {
            var user = users.FirstOrDefault(u => u.Id == UserId.From(c.UserId));

            if (user == null)
            {
                continue;
            }

            var committee = Pw184Committee.Create(
                pw184.Id,
                c.GroupType,
                UserId.From(c.UserId),
                c.FullName,
                user.Employee.View?.FullPositionName ?? string.Empty,
                ParameterCode.From(c.CommitteePositionsCode),
                c.CommitteePositionsName ?? string.Empty,
                c.Sequence);

            pw184.AddCommittee(committee);
        }
    }

    private static void AddAttachments(Pw184 pw184, AttachmentsDto[] attachments)
    {
        var fileList = attachments
            .SelectMany(r => r.FileAttachments.Select(f => (
                r.DocumentTypeCode,
                f.FileId,
                f.FileName,
                f.Sequence,
                f.IsPublic)));

        foreach (var (docType, fileId, fileName, seq, isPublic) in fileList)
        {
            pw184.AddAttachment(Pw184Attachments.Create(
                ParameterCode.From(docType),
                FileId.From(fileId),
                fileName,
                seq,
                isPublic));
        }
    }

    private static void AddVendor(Pw184 pw184, Pw184VendorDto v)
    {
        var vendor = Pw184Vendor.Create(pw184.Id)
                                .SetSequence(v.Sequence)
                                .SetVendorType(v.VendorType)
                                .SetVendor(
                                    v.SuVendorId.HasValue ? SuVendorId.From(v.SuVendorId.Value) : null,
                                    v.TaxNumber,
                                    v.VendorName,
                                    v.VendorBranchNumber)
                                .SetBill(
                                    v.VatIncludeTypeCode != null ? ParameterCode.From(v.VatIncludeTypeCode) : null,
                                    ParameterCode.From(v.BillTypeCode),
                                    v.BillTypeOther,
                                    v.BillBookNo,
                                    v.BillDate,
                                    v.BillDetail);

        pw184.AddVendor(vendor);

        if (v.VendorParcels != null)
        {
            foreach (var vp in v.VendorParcels)
            {
                var parcel = Pw184VendorParcel.Create(vendor.Id)
                                              .SetSequence(vp.Sequence)
                                              .SetItem(vp.Item, vp.ItemDetail)
                                              .SetPrice(vp.Quantity, ParameterCode.From(vp.UnitCode), vp.UnitPrice, vp.TotalPrice, vp.TotalPriceVat)
                                              .SetVatIncludeType(vp.VatIncludeTypeCode != null ? ParameterCode.From(vp.VatIncludeTypeCode) : null);

                vendor.AddVendorParcels(parcel);
            }
        }
    }

    private static void AddGLAccount(Pw184 pw184, Pw184GLAccountDto gl)
    {
        var glAcc = Pw184GLAccount.Create(pw184.Id)
                                  .SetGLAccount(gl.Sequence, gl.SolId, ParameterCode.From(gl.BudgetTypeCode), ParameterCode.From(gl.GLAccountCode), gl.ProjectNumber, gl.Amount);

        pw184.AddGLAccount(glAcc);
    }
}