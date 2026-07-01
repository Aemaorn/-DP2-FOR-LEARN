namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record CommentContractTerminationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    string? Remark);

public class CommentContractTerminationEndpoint : ContractTerminationEndpoint<CommentContractTerminationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CommentContractTerminationEndpoint(
        Dp2DbContext dbContext,
        ILogger<CommentContractTerminationRequest> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractTermination")
             .WithName("CommentContractTermination")
             .Accepts<ApproveContractTerminationEndpoint>("application/json"));
        this.Post("contract/{ContractDraftVendorId:guid}/contract-termination/{Id:guid}/comment");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(CommentContractTerminationRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var termination = entity.CmContractTerminations.FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        var delivery = entity.Delivery;

        var suVendor = this.MapSuVendorByType(entity.ContractInvitationVendors, entity.ContractDraft.Procurement.Type);

        if (suVendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ");
        }

        if (termination == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการยกเลิกสัญญา");
        }

        var assignee =
            termination.Assignees
                       .Select(DelegatorExtensions.DelegatorToAssignee)
                       .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                           ? a.UserId == UserId.From(req.UserId)
                           : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบข้อมูลผเจ้าหน้าที่พัสดุให้ความเห็นในรายการนี้");
        }

        termination.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Comment,
                string.Empty,
                termination.Status.ToString(),
                req.Remark));

        var currentUser = termination.Assignees.FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(remark: req.Remark);

        if (!termination.IsProposedApprover)
        {
            entity.SetContractStatus(ContractStatus.Cancel);
            termination.SetStatus(CmContractTerminationStatus.Approved);

            await this.ReplaceAcceptorsAsync(entity, termination, ct);
            await this.StampAcceptorDateAsync(termination, ct);
        }
        else
        {
            await this.UpdateDocumentAsync(
                entity,
                termination,
                delivery,
                suVendor,
                req.UserId,
                new TerminationDocumentOptions(IsReplace: true, HasCreator: false, HasAcceptor: false, MarkAsReplaced: false),
                ct,
                hasComment: true);
        }

        this.dbContext.CmContractTerminations.Update(termination);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}