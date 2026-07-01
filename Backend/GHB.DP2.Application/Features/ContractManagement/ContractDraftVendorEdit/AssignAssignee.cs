namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record AssignAssigneeContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    IEnumerable<AssigneeRequest>? Assignees);

public class AssignAssigneeContractDraftVendorEditEndpoint
    : ContractDraftVendorEditEndpoint<AssignAssigneeContractDraftVendorEditRequest, Results<Ok, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AssignAssigneeContractDraftVendorEditEndpoint(
        ILogger<AssignAssigneeContractDraftVendorEditEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("AssignAssigneeContractDraftVendorEdit")
             .Accepts<AssignAssigneeContractDraftVendorEditRequest>("application/json"));

        this.Put("contract/contract-draft-vendor-edit/{Id:guid}/assign");
    }

    protected override async ValueTask<Results<Ok, BadRequest<string>>>
        HandleRequestAsync(AssignAssigneeContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        if (entity.Status != ContractDraftVendorEditStatus.WaitingAssignment)
        {
            return TypedResults.BadRequest("สถานะต้องเป็นรอมอบหมายผู้รับผิดชอบ");
        }

        if (req.Assignees != null)
        {
            await this.UpsertAssignee(entity, [.. req.Assignees], ct);
        }

        // Mark assignees as Pending
        entity.Assignees
              .Where(a => a.Status == AssigneeStatus.Draft)
              .Iter(a => a.Pending());

        entity.SetWaitingComment();

        // Update documents
        var supplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
        await this.UpdateDocumentAsync(
            entity,
            supplyMethodCode,
            new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: true),
            ct);

        // Notify assignees
        foreach (var targetUserId in entity.Assignees.Where(a => a.Status == AssigneeStatus.Pending).SelectMany(a => a.GetAssigneeNotificationTargets()))
        {
            _ = SendNotificationAsync(
                entity,
                targetUserId,
                NotificationConstant.Assignment.Title,
                string.Format(
                    NotificationConstant.Assignment.Message,
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
