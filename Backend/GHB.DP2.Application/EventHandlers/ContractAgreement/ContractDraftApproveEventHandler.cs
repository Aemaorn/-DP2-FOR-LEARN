namespace GHB.DP2.Application.EventHandlers.ContractAgreement;

using GHB.DP2.Domain.ContractAgreement.Event;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public record InitAcceptanceCommitteeDto(
    int Sequence,
    SuUser User,
    ParameterCode? CommitteePositionsCode);

public class ContractDraftApproveEventHandler : IEventHandler<ContractDraftApproveEvent>
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ContractDraftApproveEventHandler> logger;

    public ContractDraftApproveEventHandler(
        ILogger<ContractDraftApproveEventHandler> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task HandleAsync(ContractDraftApproveEvent eventModel, CancellationToken ct)
    {
        this.logger.LogInformation(
            "Handling ContractInvitationApproveEvent for ContractInvitationId: {ContractInvitationId}",
            eventModel.Id);

        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        var contractDraft = await
            dbContext
                .CaContractDrafts
                .Include(caContractDraft => caContractDraft.Vendors)
                .ThenInclude(caContractDraftVendors => caContractDraftVendors.PaymentTerms)
                .Include(caContractDraft => caContractDraft.Procurement)
                .FirstOrDefaultAsync(
                    c => c.Id == eventModel.Id,
                    ct);

        if (contractDraft == null)
        {
            this.logger.LogWarning(
                "ContractDraft with Id {ContractDraftId} not found.",
                eventModel.Id);

            throw new InvalidOperationException("Contract draft not found.");
        }

        var intiAcceptanceCommittees =
            contractDraft.Procurement.Type == ProcurementType.Rent
                ? await GetAcceptanceCommitteeFromRental()
                : await GetAcceptanceCommitteeFromJp005();

        var jp006Exists = await dbContext.PJp006S.Include(pPurchaseOrder => pPurchaseOrder.Entrepreneurs).ThenInclude(pPurchaseOrderEntrepreneur => pPurchaseOrderEntrepreneur.PJp006PriceDetails)
                                         .FirstOrDefaultAsync(j => j.ProcurementId == contractDraft.ProcurementId, ct);

        await dbContext.SaveChangesAsync(ct);
        this.logger.LogWarning("ContractDraftApproveEventHandler is not implemented yet for ContractDraftId: {ContractDraftId}", eventModel.Id);

        async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromJp005()
        {
            var committees =
                await dbContext.PJp005S
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

        async Task<InitAcceptanceCommitteeDto[]> GetAcceptanceCommitteeFromRental()
        {
            var committees =
                await dbContext.PPrincipleApprovals
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
}