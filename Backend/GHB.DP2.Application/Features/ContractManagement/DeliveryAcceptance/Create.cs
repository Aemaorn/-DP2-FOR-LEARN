namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using GHB.DP2.Application.EventHandlers.ContractAgreement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateDeliveryAcceptanceRequest(
    Guid? RefId,
    SourceType SourceType,
    string? DepartmentId,
    string? SupplyMethodCode,
    string? SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    string? Name,
    decimal? Budget,
    bool? IsCommercialMaterial);

public class CreateDeliveryAcceptance : EndpointBase<CreateDeliveryAcceptanceRequest, Ok<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateDeliveryAcceptance(
        Dp2DbContext dbContext,
        ILogger<CreateDeliveryAcceptance> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .Produces<Ok>());
        this.Post("delivery-acceptance");
    }

    protected override async ValueTask<Ok<Guid>> HandleRequestAsync(CreateDeliveryAcceptanceRequest req, CancellationToken ct)
    {
        using var tx = await this.dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        if (req.SourceType != SourceType.Manual && !req.RefId.HasValue)
        {
            this.ThrowError("ต้องระบุเอกสารอ้างอิง", StatusCodes.Status400BadRequest);
        }

        CmDeliveryAcceptance newEntity;

        if (req.SourceType == SourceType.Manual)
        {
            newEntity = CmDeliveryAcceptance.CreateManual(
                !string.IsNullOrWhiteSpace(req.DepartmentId) ? Domain.Raws.BusinessUnitId.From(req.DepartmentId) : null,
                !string.IsNullOrWhiteSpace(req.SupplyMethodCode) ? Domain.SystemUtility.ParameterCode.From(req.SupplyMethodCode) : null,
                !string.IsNullOrWhiteSpace(req.SupplyMethodTypeCode) ? Domain.SystemUtility.ParameterCode.From(req.SupplyMethodTypeCode) : null,
                !string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode) ? Domain.SystemUtility.ParameterCode.From(req.SupplyMethodSpecialTypeCode) : null,
                req.Name,
                req.Budget,
                req.IsCommercialMaterial);

            var (numberPrefix, numberSeq) = await this.GetNextDocumentNumberSequenceAsync(ct);
            newEntity.SetNumber($"{numberPrefix}{numberSeq:D5}");

            this.dbContext.CmDeliveryAcceptances.Add(newEntity);
            await this.dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return TypedResults.Ok(newEntity.Id.Value);
        }

        newEntity = CmDeliveryAcceptance.Create(
            null,
            req.SourceType,
            req.RefId);

        if (newEntity.SourceType == SourceType.ContractDraftVendor)
        {
            var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                                                .Include(caContractDraftVendor => caContractDraftVendor.PaymentTerms)
                                                .Include(c => c.ContractDraft)
                                                .ThenInclude(caContractDraft => caContractDraft.Procurement)
                                                .FirstOrDefaultAsync(x => x.Id == ContractDraftVendorId.From(req.RefId!.Value), ct);

            if (contractDraftVendor == null)
            {
                this.ThrowError("ไม่พบข้อมูลผู้ขายในร่างสัญญา", StatusCodes.Status404NotFound);
            }

            var contractDraft = contractDraftVendor.ContractDraft;

            var intiAcceptanceCommittees =
                contractDraft.Procurement.Type == ProcurementType.Rent
                    ? await GetAcceptanceCommitteeFromRental()
                    : await GetAcceptanceCommitteeFromJp005();

            if (!intiAcceptanceCommittees.Any())
            {
                intiAcceptanceCommittees = await GetAcceptanceCommitteeFromContractDraftVendor();
            }

            var jp004Exists = await this.dbContext.PpPurchaseRequisitions.Include(ppPurchaseRequisition => ppPurchaseRequisition.Budgets)
                                        .ThenInclude(ppPurchaseRequisitionBudget => ppPurchaseRequisitionBudget.PpPurchaseRequisitionBudgetDetails)
                                        .FirstOrDefaultAsync(j => j.ProcurementId == contractDraft.ProcurementId, ct);

            var (acPrefix1, acNextSeq1) = await this.GetNextAcceptanceSequenceAsync(ct);
            foreach (var pt in contractDraftVendor.PaymentTerms.OrderBy(x => x.Sequence))
            {
                var newPeriod = CmDeliveryAcceptancePeriod.Create(
                    newEntity.Id,
                    CmDeliveryAcceptancePeriodStatus.Draft);

                intiAcceptanceCommittees?
                    .Iter(ac =>
                        newPeriod.AddAcceptor(
                            CmDeliveryAcceptancePeriodAcceptor
                                .Create(
                                    newPeriod.Id,
                                    AcceptorType.AcceptanceCommittee,
                                    ac.User,
                                    ac.Sequence,
                                    CmDeliveryAcceptancePeriodStatus.Draft)
                                .SetCommitteePositionsCode(ac.CommitteePositionsCode)));

                newPeriod.SetAcceptanceNumber($"{acPrefix1}{acNextSeq1++:D5}");

                newPeriod.AddPaymentTerm(
                    CmDeliveryAcceptancePeriodPaymentTerm.Create(
                        newPeriod.Id,
                        pt.Sequence,
                        pt.PaymentTermNo ?? 0,
                        pt.Description ?? string.Empty,
                        pt.Amount ?? 0));

                if (jp004Exists is not null)
                {
                    jp004Exists.Budgets.SelectMany(x => x.PpPurchaseRequisitionBudgetDetails)
                               .Iter(b => newPeriod.AddBudget(CmDeliveryAcceptancePeriodBudget.Create(
                                   newPeriod.Id,
                                   b.Sequence,
                                   b.Department,
                                   b.BudgetTypeCode,
                                   b.ProjectCode,
                                   b.AccountNoCode,
                                   b.Budget)));
                }

                newPeriod.SetContractBudget(contractDraftVendor.Budget);

                newEntity.AddPeriod(newPeriod);
            }

            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromJp005()
            {
                var committees =
                    await this.dbContext.PJp005S
                              .Include(f => f.Committees)
                              .ThenInclude(c => c.User)
                              .Where(w => w.ProcurementId == contractDraft.ProcurementId)
                              .SelectMany(s => s.Committees)
                              .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                              .OrderBy(o => o.Sequence)
                              .ToListAsync(ct);

                if (!committees.Any())
                {
                    return [];
                }

                return
                [
                    .. committees
                        .Select(a =>
                            new InitAcceptanceCommitteeDto(
                                a.Sequence,
                                a.User,
                                a.CommitteePositionsCode))
                ];
            }

            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromContractDraftVendor()
            {
                var committees =
                    await this.dbContext.PPurchaseOrderApprovals
                              .Include(f => f.Committees)
                              .ThenInclude(c => c.User)
                              .Where(w => w.ProcurementId == contractDraft.ProcurementId)
                              .SelectMany(s => s.Committees)
                              .OrderBy(o => o.Sequence)
                              .ToListAsync(ct);

                if (!committees.Any())
                {
                    return [];
                }

                return
                [
                    .. committees
                        .Select(a =>
                            new InitAcceptanceCommitteeDto(
                                a.Sequence,
                                a.User,
                                a.CommitteePositionsCode))
                ];
            }

            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromRental()
            {
                var committees =
                    await this.dbContext.PPrincipleApprovals
                              .Include(c => c.PrincipleApprovalCommittees)
                              .ThenInclude(c => c.User)
                              .Where(w => w.ProcurementId == contractDraft.ProcurementId)
                              .SelectMany(s => s.PrincipleApprovalCommittees)
                              .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                              .OrderBy(o => o.Sequence)
                              .ToArrayAsync(ct);

                if (!committees.Any())
                {
                    return [];
                }

                return
                [
                    .. committees
                        .Select(a =>
                            new InitAcceptanceCommitteeDto(
                                a.Sequence,
                                a.User,
                                a.CommitteePositionsCode))
                ];
            }
        }

        if (newEntity.SourceType == SourceType.ContractDraftVendorEdit)
        {
            var vendorEdit = await this.dbContext.CaContractDraftVendorEdits
                                                 .FirstOrDefaultAsync(x => x.Id == ContractDraftVendorEditId.From(req.RefId!.Value), ct);

            if (vendorEdit == null)
            {
                this.ThrowError("ไม่พบข้อมูลการแก้ไขร่างสัญญา", StatusCodes.Status404NotFound);
            }

            var contractDraftVendor = await this.dbContext.CaContractDraftVendors
                                                .Include(v => v.PaymentTerms)
                                                .Include(v => v.ContractDraft)
                                                .ThenInclude(d => d.Procurement)
                                                .FirstOrDefaultAsync(x => x.Id == vendorEdit.ContractDraftVendorId, ct);

            if (contractDraftVendor == null)
            {
                this.ThrowError("ไม่พบข้อมูลผู้ขายในร่างสัญญา", StatusCodes.Status404NotFound);
            }

            var contractDraft = contractDraftVendor.ContractDraft;

            var initAcceptanceCommittees =
                contractDraft.Procurement.Type == ProcurementType.Rent
                    ? await GetAcceptanceCommitteeFromRentalByProcId(contractDraft.ProcurementId)
                    : await GetAcceptanceCommitteeFromJp005ByProcId(contractDraft.ProcurementId);

            if (!initAcceptanceCommittees.Any())
            {
                initAcceptanceCommittees = await GetAcceptanceCommitteeFromPOAByProcId(contractDraft.ProcurementId);
            }

            var jp004Exists = await this.dbContext.PpPurchaseRequisitions
                                        .Include(pr => pr.Budgets)
                                        .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                                        .FirstOrDefaultAsync(j => j.ProcurementId == contractDraft.ProcurementId, ct);

            var (acPrefix2, acNextSeq2) = await this.GetNextAcceptanceSequenceAsync(ct);
            foreach (var pt in contractDraftVendor.PaymentTerms.OrderBy(x => x.Sequence))
            {
                var newPeriod = CmDeliveryAcceptancePeriod.Create(
                    newEntity.Id,
                    CmDeliveryAcceptancePeriodStatus.Draft);

                initAcceptanceCommittees?
                    .Iter(ac =>
                        newPeriod.AddAcceptor(
                            CmDeliveryAcceptancePeriodAcceptor
                                .Create(
                                    newPeriod.Id,
                                    AcceptorType.AcceptanceCommittee,
                                    ac.User,
                                    ac.Sequence,
                                    CmDeliveryAcceptancePeriodStatus.Draft)
                                .SetCommitteePositionsCode(ac.CommitteePositionsCode)));

                newPeriod.SetAcceptanceNumber($"{acPrefix2}{acNextSeq2++:D5}");

                newPeriod.AddPaymentTerm(
                    CmDeliveryAcceptancePeriodPaymentTerm.Create(
                        newPeriod.Id,
                        pt.Sequence,
                        pt.PaymentTermNo ?? 0,
                        pt.Description ?? string.Empty,
                        pt.Amount ?? 0));

                if (jp004Exists is not null)
                {
                    jp004Exists.Budgets.SelectMany(x => x.PpPurchaseRequisitionBudgetDetails)
                               .Iter(b => newPeriod.AddBudget(CmDeliveryAcceptancePeriodBudget.Create(
                                   newPeriod.Id,
                                   b.Sequence,
                                   b.Department,
                                   b.BudgetTypeCode,
                                   b.ProjectCode,
                                   b.AccountNoCode,
                                   b.Budget)));
                }

                newPeriod.SetContractBudget(contractDraftVendor.Budget);

                newEntity.AddPeriod(newPeriod);
            }

            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromJp005ByProcId(ProcurementId procId)
            {
                var committees = await this.dbContext.PJp005S
                                           .Include(f => f.Committees)
                                           .ThenInclude(c => c.User)
                                           .Where(w => w.ProcurementId == procId)
                                           .SelectMany(s => s.Committees)
                                           .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                                           .OrderBy(o => o.Sequence)
                                           .ToListAsync(ct);
                return committees.Any()
                    ? [.. committees.Select(a => new InitAcceptanceCommitteeDto(a.Sequence, a.User, a.CommitteePositionsCode))]
                    : [];
            }

            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromPOAByProcId(ProcurementId procId)
            {
                var committees = await this.dbContext.PPurchaseOrderApprovals
                                           .Include(f => f.Committees)
                                           .ThenInclude(c => c.User)
                                           .Where(w => w.ProcurementId == procId)
                                           .SelectMany(s => s.Committees)
                                           .OrderBy(o => o.Sequence)
                                           .ToListAsync(ct);
                return committees.Any()
                    ? [.. committees.Select(a => new InitAcceptanceCommitteeDto(a.Sequence, a.User, a.CommitteePositionsCode))]
                    : [];
            }

            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromRentalByProcId(ProcurementId procId)
            {
                var committees = await this.dbContext.PPrincipleApprovals
                                           .Include(c => c.PrincipleApprovalCommittees)
                                           .ThenInclude(c => c.User)
                                           .Where(w => w.ProcurementId == procId)
                                           .SelectMany(s => s.PrincipleApprovalCommittees)
                                           .Where(w => w.GroupType == CommitteeGroupType.AcceptanceCommittee)
                                           .OrderBy(o => o.Sequence)
                                           .ToArrayAsync(ct);
                return committees.Any()
                    ? [.. committees.Select(a => new InitAcceptanceCommitteeDto(a.Sequence, a.User, a.CommitteePositionsCode))]
                    : [];
            }
        }

        if (newEntity.SourceType == SourceType.Procurement)
        {
            var poa = await this.dbContext.PPurchaseOrderApprovals
                                        .Include(x => x.Procurement)
                                        .ThenInclude(p => p.Plan)
                                        .FirstOrDefaultAsync(x => x.Id == PurchaseOrderApprovalId.From(req.RefId!.Value), ct);

            if (poa == null)
            {
                this.ThrowError("ไม่พบข้อมูลใบสั่งซื้อ/จ้าง/เช่า", StatusCodes.Status404NotFound);
            }

            var procurement = poa.Procurement;

            var purchaseRequisition = await this.dbContext.PpPurchaseRequisitions
                                                .Include(pr => pr.PaymentTerms)
                                                .Include(pr => pr.Budgets)
                                                .ThenInclude(b => b.PpPurchaseRequisitionBudgetDetails)
                                                .FirstOrDefaultAsync(pr => pr.ProcurementId == procurement.Id, ct);

            if (purchaseRequisition == null)
            {
                this.dbContext.CmDeliveryAcceptances.Add(newEntity);
                await this.dbContext.SaveChangesAsync(ct);
                return TypedResults.Ok(newEntity.Id.Value);
            }

            var initAcceptanceCommittees = await GetAcceptanceCommitteeFromProcurementJp005();
            if (!initAcceptanceCommittees.Any())
            {
                initAcceptanceCommittees = await GetAcceptanceCommitteeFromProcurementPOA();
            }

            var paymentTerms = purchaseRequisition.PaymentTerms?.Where(pp => pp.IsMA != true).ToList();

            if (paymentTerms != null && paymentTerms.Any())
            {
                var termWithTotalPeriod = paymentTerms
                    .FirstOrDefault(pp => pp.TotalPeriod.HasValue && pp.TotalPeriod > 0);

                var (acPrefix3, acNextSeq3) = await this.GetNextAcceptanceSequenceAsync(ct);

                if (termWithTotalPeriod != null)
                {
                    var totalPeriod = termWithTotalPeriod.TotalPeriod!.Value;

                    for (int i = 1; i <= totalPeriod; i++)
                    {
                        var newPeriod = CmDeliveryAcceptancePeriod.Create(
                            newEntity.Id,
                            CmDeliveryAcceptancePeriodStatus.Draft);

                        initAcceptanceCommittees
                            .Iter(ac =>
                                newPeriod.AddAcceptor(
                                    CmDeliveryAcceptancePeriodAcceptor
                                        .Create(
                                            newPeriod.Id,
                                            AcceptorType.AcceptanceCommittee,
                                            ac.User,
                                            ac.Sequence,
                                            CmDeliveryAcceptancePeriodStatus.Draft)
                                        .SetCommitteePositionsCode(ac.CommitteePositionsCode)));

                        newPeriod.SetAcceptanceNumber($"{acPrefix3}{acNextSeq3++:D5}");

                        newPeriod.AddPaymentTerm(
                            CmDeliveryAcceptancePeriodPaymentTerm.Create(
                                newPeriod.Id,
                                i,
                                i,
                                termWithTotalPeriod.Description ?? string.Empty,
                                0));

                        purchaseRequisition.Budgets.SelectMany(x => x.PpPurchaseRequisitionBudgetDetails)
                                           .Iter(b => newPeriod.AddBudget(CmDeliveryAcceptancePeriodBudget.Create(
                                               newPeriod.Id,
                                               b.Sequence,
                                               b.Department,
                                               b.BudgetTypeCode,
                                               b.ProjectCode,
                                               b.AccountNoCode,
                                               b.Budget)));

                        newPeriod.SetContractBudget(procurement.Plan.Budget);

                        newEntity.AddPeriod(newPeriod);
                    }
                }
                else
                {
                    // กรณีมี PaymentTerms แบบปกติ → สร้าง Period ตามจำนวน PaymentTerms
                    foreach (var pt in paymentTerms.OrderBy(p => p.TermNumber))
                    {
                        var newPeriod = CmDeliveryAcceptancePeriod.Create(
                            newEntity.Id,
                            CmDeliveryAcceptancePeriodStatus.Draft);

                        initAcceptanceCommittees
                            .Iter(ac =>
                                newPeriod.AddAcceptor(
                                    CmDeliveryAcceptancePeriodAcceptor
                                        .Create(
                                            newPeriod.Id,
                                            AcceptorType.AcceptanceCommittee,
                                            ac.User,
                                            ac.Sequence,
                                            CmDeliveryAcceptancePeriodStatus.Draft)
                                        .SetCommitteePositionsCode(ac.CommitteePositionsCode)));

                        newPeriod.SetAcceptanceNumber($"{acPrefix3}{acNextSeq3++:D5}");

                        newPeriod.AddPaymentTerm(
                            CmDeliveryAcceptancePeriodPaymentTerm.Create(
                                newPeriod.Id,
                                (int)pt.TermNumber,
                                (int)pt.TermNumber,
                                pt.Description ?? string.Empty,
                                0m));

                        purchaseRequisition.Budgets.SelectMany(x => x.PpPurchaseRequisitionBudgetDetails)
                                           .Iter(b => newPeriod.AddBudget(CmDeliveryAcceptancePeriodBudget.Create(
                                               newPeriod.Id,
                                               b.Sequence,
                                               b.Department,
                                               b.BudgetTypeCode,
                                               b.ProjectCode,
                                               b.AccountNoCode,
                                               b.Budget)));

                        newPeriod.SetContractBudget(procurement.Plan.Budget);

                        newEntity.AddPeriod(newPeriod);
                    }
                }
            }

            // ถ้ามี จพ.004 แต่ไม่มี PaymentTerms → ไม่สร้าง Period (ให้ผู้ใช้สร้างเอง)
            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromProcurementJp005()
            {
                var committees =
                    await this.dbContext.PJp005S
                              .Include(f => f.Committees)
                              .ThenInclude(c => c.User)
                              .Where(w => w.ProcurementId == procurement.Id)
                              .SelectMany(s => s.Committees)
                              .Where(w => w.GroupType == PJp005CommitteeGroupType.InspectionCommittee)
                              .OrderBy(o => o.Sequence)
                              .ToListAsync(ct);

                if (!committees.Any())
                {
                    return [];
                }

                return
                [
                    .. committees
                        .Select(a =>
                            new InitAcceptanceCommitteeDto(
                                a.Sequence,
                                a.User,
                                a.CommitteePositionsCode))
                ];
            }

            async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromProcurementPOA()
            {
                var committees =
                    await this.dbContext.PPurchaseOrderApprovals
                              .Include(f => f.Committees)
                              .ThenInclude(c => c.User)
                              .Where(w => w.ProcurementId == procurement.Id)
                              .SelectMany(s => s.Committees)
                              .OrderBy(o => o.Sequence)
                              .ToListAsync(ct);

                if (!committees.Any())
                {
                    return [];
                }

                return
                [
                    .. committees
                        .Select(a =>
                            new InitAcceptanceCommitteeDto(
                                a.Sequence,
                                a.User,
                                a.CommitteePositionsCode))
                ];
            }
        }

        this.dbContext.CmDeliveryAcceptances.Add(newEntity);
        await this.dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return TypedResults.Ok(newEntity.Id.Value);
    }

    private async Task<(string Prefix, int NextSequence)> GetNextAcceptanceSequenceAsync(CancellationToken ct)
    {
        var yearSuffix = ((DateTimeOffset.UtcNow.Year + 543) % 100).ToString("D2");
        var prefix = $"RC{yearSuffix}";

        var lastAcceptanceNumber = await this.dbContext.CmDeliveryAcceptancePeriods
                                             .IgnoreQueryFilters()
                                             .Where(p => !string.IsNullOrWhiteSpace(p.AcceptanceNumber) && p.AcceptanceNumber.StartsWith(prefix))
                                             .OrderByDescending(p => p.AcceptanceNumber)
                                             .Select(p => p.AcceptanceNumber)
                                             .FirstOrDefaultAsync(ct);

        var nextSequence = lastAcceptanceNumber is null
            ? 1
            : int.Parse(lastAcceptanceNumber[prefix.Length..]) + 1;

        return (prefix, nextSequence);
    }

    private async Task<(string Prefix, int NextSequence)> GetNextDocumentNumberSequenceAsync(CancellationToken ct)
    {
        var yearSuffix = ((DateTimeOffset.UtcNow.Year + 543) % 100).ToString("D2");
        var prefix = $"DA{yearSuffix}";

        var lastNumber = await this.dbContext.CmDeliveryAcceptances
                                   .IgnoreQueryFilters()
                                   .Where(p => p.Number != null && p.Number.StartsWith(prefix))
                                   .OrderByDescending(p => p.Number)
                                   .Select(p => p.Number)
                                   .FirstOrDefaultAsync(ct);

        var nextSequence = lastNumber is null
            ? 1
            : int.Parse(lastNumber[prefix.Length..]) + 1;

        return (prefix, nextSequence);
    }
}