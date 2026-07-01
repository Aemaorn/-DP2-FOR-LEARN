namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetContractDraftRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid ProcurementId { get; init; }
}

public sealed record GetContractDraftResponse(
    [property: Description("รหัสร่างสัญญา")]
    Guid Id,
    [property: Description("รหัสการจัดซื้อ")]
    Guid ProcurementId,
    [property: Description("รายชื่อผู้ขาย")]
    ContractDraftVendorInfo[] Vendors,
    [property: Description("ผู้มีสิทธิ์แก้ไข")]
    bool HasEditPermission);

public record ContractDraftVendorInfo(
    [property: Description("รหัสผู้ขาย")] Guid Id,
    [property: Description("ชื่อผู้ขาย")] string Name,
    bool IsCompleted,
    string? ContractNumber = null,
    string? Status = null
);

public class GetContractDraftEndpoint : ContractDraftEndpointBase<GetContractDraftRequest, Ok<GetContractDraftResponse>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractDraftEndpoint(
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<GetVendorEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId}/contract-draft");
        this.Description(b => b
                              .WithTags(nameof(ContractDraft))
                              .WithName("GetContractDraft")
                              .Produces<Ok<GetContractDraftResponse>>()
                              .WithSummary("Get Contract Draft")
                              .WithDescription("Retrieve the contract draft for a specific procurement."));
    }

    protected override async ValueTask<Ok<GetContractDraftResponse>> HandleRequestAsync(GetContractDraftRequest req, CancellationToken ct)
    {
        var contractDraft =
            await this.dbContext
                      .CaContractDrafts
                      .Include(c => c.Vendors).ThenInclude(caContractDraftVendor => caContractDraftVendor.ContractInvitationVendors)
                      .ThenInclude(caContractInvitationVendors => caContractInvitationVendors.PurchaseOrderApprovalContract)
                      .ThenInclude(pPurchaseOrderApprovalContract => pPurchaseOrderApprovalContract.Entrepreneur)
                      .ThenInclude(pPurchaseOrderEntrepreneur => pPurchaseOrderEntrepreneur!.SuVendor)
                      .Include(caContractDraft => caContractDraft.Vendors)
                      .ThenInclude(caContractDraftVendor => caContractDraftVendor.ContractInvitationVendors)
                      .ThenInclude(caContractInvitationVendors => caContractInvitationVendors.PurchaseOrderApprovalContract)
                      .ThenInclude(pPurchaseOrderApprovalContract => pPurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs)
                      .ThenInclude(pPrincipleApprovalRentalEntrepreneurs => pPrincipleApprovalRentalEntrepreneurs!.Vendor)
                      .Include(p => p.Vendors)
                      .ThenInclude(v => v.ContractInvitationVendors)
                      .ThenInclude(ci => ci.PurchaseOrderApprovalContract)
                      .Include(caContractDraft => caContractDraft.Procurement)
                      .FirstOrDefaultAsync(
                          c => c.ProcurementId == ProcurementId.From(req.ProcurementId),
                          ct);

        if (contractDraft == null)
        {
            this.ThrowError("ไม่พบข้อมูลร่างสัญญา", StatusCodes.Status404NotFound);
        }

        var hasEditPermission = false;

        var poaData = await this.dbContext.PPurchaseOrderApprovals
                                .Where(w => w.ProcurementId == ProcurementId.From(req.ProcurementId) && w.Status == PurchaseOrderApprovalStatus.Assigned)
                                .SelectMany(s => s.Assignees)
                                .Where(a => a.Type == AssigneeType.Assignee)
                                .ToListAsync(ct);

        if (poaData != null)
        {
            hasEditPermission = poaData.Select(DelegatorExtensions.DelegatorToAssignee)
                                       .Any(a => a.Delegatee?.SuUserId == null
                                           ? a.UserId == req.UserId
                                           : a.Delegatee?.SuUserId == UserId.From(req.UserId) &&
                                             !a.IsDeleted);
        }

        var response = new GetContractDraftResponse(
            contractDraft.Id.Value,
            contractDraft.ProcurementId.Value,
            [
                .. contractDraft.Vendors
                                .Select(v =>
                                    new ContractDraftVendorInfo(
                                        v.Id.Value,
                                        ResolveBudgetDescription(contractDraft.Procurement.Type, v.ContractInvitationVendors.PurchaseOrderApprovalContract) ?? $"{v.ContractNumber} : {v.ContractInvitationVendors.PurchaseOrderApprovalContract.Entrepreneur?.SuVendor.EstablishmentName}{v.ContractInvitationVendors.PurchaseOrderApprovalContract.PrincipleApprovalRentalEntrepreneurs?.Vendor.EstablishmentName}",
                                        v.Status == ContractDraftVendorStatus.Approved && v.ContractSignedDate.HasValue,
                                        v.ContractNumber,
                                        v.Status.ToString()))
            ],
            hasEditPermission);

        return TypedResults.Ok(response);
    }

    protected static string? ResolveBudgetDescription(ProcurementType type, PPurchaseOrderApprovalContract contract)
    {
        if (type == ProcurementType.Rent && contract.PrincipleApprovalRentalBudget is not null)
        {
            return contract.PrincipleApprovalRentalBudget.Description;
        }

        if (type == ProcurementType.Procurement && contract.Budget is not null)
        {
            return contract.Budget.Description;
        }

        if (contract.PpPurchaseRequisitionBudget is not null)
        {
            return contract.PpPurchaseRequisitionBudget.Description;
        }

        if (contract.PPurchaseOrderApprovalBudget is not null)
        {
            return contract.PPurchaseOrderApprovalBudget.Description;
        }

        return null;
    }
}