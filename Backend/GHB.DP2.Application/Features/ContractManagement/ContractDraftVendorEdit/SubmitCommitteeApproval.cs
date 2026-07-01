namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record SubmitCommitteeApprovalContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    IEnumerable<AcceptorRequest>? Acceptors,
    DateTimeOffset? DocumentDate = null);

public class SubmitCommitteeApprovalContractDraftVendorEditEndpoint
    : ContractDraftVendorEditEndpoint<SubmitCommitteeApprovalContractDraftVendorEditRequest, Results<Ok, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public SubmitCommitteeApprovalContractDraftVendorEditEndpoint(
        ILogger<SubmitCommitteeApprovalContractDraftVendorEditEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("SubmitCommitteeApprovalContractDraftVendorEdit")
             .Accepts<SubmitCommitteeApprovalContractDraftVendorEditRequest>());

        this.Put("contract/contract-draft-vendor-edit/{Id:guid}/submit-committee-approval");
    }

    protected override async ValueTask<Results<Ok, BadRequest<string>>>
        HandleRequestAsync(SubmitCommitteeApprovalContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        // Validate at least 1 edited section
        var hasEditedSection = entity.Components.Any(c => c.IsEdited);

        if (!hasEditedSection)
        {
            return TypedResults.BadRequest("ต้องมี section ที่แก้ไขแล้วอย่างน้อย 1 section");
        }

        // Update acceptors if provided
        if (req.Acceptors != null)
        {
            await this.UpsertAcceptors(
                entity,
                [.. req.Acceptors],
                ct,
                UserId.From(req.UserId));
        }

        // Set committee acceptors to pending
        entity.Acceptors
              .Where(a => a is { Type: AcceptorType.AcceptanceCommittee, IsActive: true } && !a.IsUnableToPerformDuties)
              .Iter(a => a.Pending());

        entity.SetDocumentDate(req.DocumentDate);
        entity.SetWaitingCommitteeApproval();

        // Update documents — mark replaced when submitting to committee
        var supplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
        await this.UpdateDocumentAsync(
            entity,
            supplyMethodCode,
            new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
            ct,
            creatorUserId: UserId.From(req.UserId));

        // Notify committee members
        foreach (var member in entity.Acceptors
                                     .Where(a => a is { Type: AcceptorType.AcceptanceCommittee, IsActive: true, Status: AcceptorStatus.Pending }))
        {
            _ = SendNotificationAsync(
                entity,
                member.UserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(
                    NotificationConstant.WaitForLike.Message,
                    ProgramConstant.ContractDraftVendorEdit.Name,
                    entity.ContractNumber));
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(CaContractDraftVendorEdit entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(userId, title, message, NotificationProgram.ContractManagement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractDraftVendorEdit.Url, entity.Id.Value), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}