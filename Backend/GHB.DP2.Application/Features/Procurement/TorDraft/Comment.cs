namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AssigneeCommentRequest
{
    public Guid ProcurementId { get; init; }

    public Guid TorDraftId { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class AssigneeCommentTorDraftEndpoint : TorDraftEndpointBase<AssigneeCommentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AssigneeCommentTorDraftEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<AssigneeCommentTorDraftEndpoint> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/tordraft/{TorDraftId:guid}/assignee-comment");
        this.Options(b =>
            b.WithTags("Procurement/TorDraft")
             .WithName("AssigneeCommentTorDraft")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(AssigneeCommentRequest req, CancellationToken ct)
    {
        // Fetch TorDraft data
        var torDraft = await this.FetchTorDraftAsync(req, ct);

        var appoint = await this.GetAppointById(ProcurementId.From(req.ProcurementId), ct);

        if (torDraft is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล TOR แบบร่าง");
        }

        // Validate that the assignee status is not Assigned
        if (torDraft.Status != TorDraftStatus.WaitingComment)
        {
            return TypedResults.BadRequest("ไม่สามารถแสดงความคิดเห็นได้ เนื่องจากผู้มอบหมายได้มอบหมายให้กับผู้รับผิดชอบแล้ว");
        }

        var assignee =
            torDraft.Assignees
                    .Map(DelegatorExtensions.DelegatorToAssignee)
                    .FirstOrDefault(a =>
                        a.Delegatee?.SuUserId == null
                                    ? a.UserId == UserId.From(req.UserId)
                                    : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ไม่พบผู้รับผิดชอบที่ตรงกับผู้ใช้ปัจจุบัน");
        }

        if (string.IsNullOrWhiteSpace(req.Remark))
        {
            return TypedResults.BadRequest("ความคิดเห็นไม่สามารถเป็นค่าว่าง");
        }

        var currentUser =
            torDraft.Assignees
                    .FirstOrDefault(a => a.Id == assignee.Id);

        if (currentUser is null)
        {
            return TypedResults.BadRequest("ไม่พบผู้รับผิดชอบที่ตรงกับผู้ใช้ปัจจุบัน");
        }

        torDraft.AddActivity(new ActivityInfo(
            "เจ้าหน้าที่พัสดุให้ความเห็น",
            $"เจ้าหน้าที่พัสดุให้ความเห็น",
            torDraft.Status.ToString(),
            req.Remark));

        // Add comment to TorDraft
        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(req.Remark);

        await this.ReplaceDocumentsAsync(torDraft, appoint, ct);

        // Update the TorDraft in the database
        this.dbContext.PpTorDrafts.Update(torDraft);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async ValueTask<PpTorDraft?> FetchTorDraftAsync(AssigneeCommentRequest req, CancellationToken ct)
    {
        return await this.dbContext.PpTorDrafts
                         .FirstOrDefaultAsync(
                             t =>
                                 t.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                 t.Id == PpTorDraftId.From(req.TorDraftId),
                             ct);
    }
}