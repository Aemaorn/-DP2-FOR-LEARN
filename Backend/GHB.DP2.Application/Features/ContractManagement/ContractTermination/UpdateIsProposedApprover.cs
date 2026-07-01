namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record UpdateIsProposedApproverTerminationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    bool IsProposedApprover);

public class UpdateIsProposedApproverTerminationEndpoint : ContractTerminationEndpoint<
    UpdateIsProposedApproverTerminationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateIsProposedApproverTerminationEndpoint(
        ILogger<UpdateIsProposedApproverTerminationEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractTermination")
             .WithName("UpdateIsProposedApproverTermination"));
        this.Put("contract/{ContractDraftVendorId:guid}/contract-termination/{Id:guid}/proposed-approver");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        UpdateIsProposedApproverTerminationRequest req,
        CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var termination = entity.CmContractTerminations.FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        if (termination == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการยกเลิกสัญญา");
        }

        termination.SetIsProposedApprover(req.IsProposedApprover);

        if (termination.Assignees.Any(r => !string.IsNullOrWhiteSpace(r.Remark)) && !req.IsProposedApprover && termination.Status == CmContractTerminationStatus.WaitingComment)
        {
            entity.SetContractStatus(ContractStatus.Cancel);
            termination.SetStatus(CmContractTerminationStatus.Approved);

            var suVendor = this.MapSuVendorByType(entity.ContractInvitationVendors, entity.ContractDraft.Procurement.Type);

            if (suVendor is not null)
            {
                await this.ReplaceAcceptorsAsync(entity, termination, ct);
                await this.StampAcceptorDateAsync(termination, ct);
            }
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}