namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record CommentContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? Remark);

public class CommentContractDraftVendorEditEndpoint
    : ContractDraftVendorEditEndpoint<CommentContractDraftVendorEditRequest, Results<Ok, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CommentContractDraftVendorEditEndpoint(
        ILogger<CommentContractDraftVendorEditEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("CommentContractDraftVendorEdit")
             .Accepts<CommentContractDraftVendorEditRequest>("application/json"));

        this.Post("contract/contract-draft-vendor-edit/{Id:guid}/comment");
    }

    protected override async ValueTask<Results<Ok, BadRequest<string>>>
        HandleRequestAsync(CommentContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var entity = await this.GetEditByIdAsync(ContractDraftVendorEditId.From(req.Id), ct);

        if (entity.Status is not (ContractDraftVendorEditStatus.WaitingComment
            or ContractDraftVendorEditStatus.RejectedToAssignee))
        {
            return TypedResults.BadRequest("สถานะต้องเป็นรอ comment");
        }

        var mappedAssignee = entity.Assignees
                                   .Select(DelegatorExtensions.DelegatorToAssignee)
                                   .FirstOrDefault(a =>
                                       a.UserId == UserId.From(req.UserId) ||
                                       (a.Delegatee != null && a.Delegatee.SuUserId == UserId.From(req.UserId)));

        if (mappedAssignee is null)
        {
            return TypedResults.BadRequest("ไม่พบผู้รับผิดชอบ");
        }

        var assignee = entity.Assignees.FirstOrDefault(a => a.Id == mappedAssignee.Id);

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบผู้รับผิดชอบ");
        }

        assignee.Assigned();

        if (!string.IsNullOrWhiteSpace(req.Remark))
        {
            assignee.SetRemark(req.Remark);
        }

        entity.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            "ความเห็นผู้รับผิดชอบ",
            ContractDraftVendorEditStatus.WaitingComment.ToString(),
            req.Remark));

        var supplyMethodCode = await this.GetSupplyMethodCodeAsync(entity, ct);
        await this.UpdateDocumentAsync(
            entity,
            supplyMethodCode,
            new ContractDraftVendorEditDocumentOptions(false, false, IsMarkReplaced: false),
            ct,
            hasComment: true);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}