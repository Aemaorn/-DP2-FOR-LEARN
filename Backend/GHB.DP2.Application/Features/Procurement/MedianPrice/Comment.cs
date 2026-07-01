namespace GHB.DP2.Application.Features.Procurement.MedianPrice;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.MedianPrice.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpMedianPrice;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record AssigneeCommentRequest
{
    public Guid ProcurementId { get; init; }

    public Guid MedianPriceId { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class AssigneeCommentMedianPriceEndpoint : MedianPriceEndpointBase<AssigneeCommentRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AssigneeCommentMedianPriceEndpoint(
        ILogger<AssigneeCommentMedianPriceEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, commandTextService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/median-price/{MedianPriceId:guid}/assignee-comment");
        this.Options(b =>
            b.WithTags(nameof(MedianPrice))
             .WithName("AssigneeCommentMedianPrice")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(AssigneeCommentRequest req, CancellationToken ct)
    {
        // Fetch median price data
        var medianPrice = await this.FetchMedianPriceAsync(req, ct);

        if (medianPrice is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลราคากลาง");
        }

        // Validate that the assignee is status is not Assigned
        if (medianPrice.Status != MedianPriceStatus.WaitingComment)
        {
            return TypedResults.BadRequest("ผู้รับผิดชอบราคากลางนี้ไม่ได้อยู่ในสถานะที่สามารถเพิ่มความคิดเห็นได้");
        }

        // Add comment to median price
        var assignee =
            medianPrice.Assignees
                       .Select(DelegatorExtensions.DelegatorToAssignee)
                       .FirstOrDefault(a => a.Delegatee == null
                                                 ? a.UserId == UserId.From(req.UserId)
                                                 : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (assignee is null)
        {
            return TypedResults.BadRequest("ผู้ใช้ไม่อยู่ในรายชื่อผู้รับผิดชอบราคากลางนี้");
        }

        if (string.IsNullOrWhiteSpace(req.Remark))
        {
            return TypedResults.BadRequest("กรุณาระบุความคิดเห็น");
        }

        var currentUser =
          medianPrice.Assignees
              .First(a => a.Id == assignee.Id);

        medianPrice.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Comment,
            $"เจ้าหน้าที่พัสดุให้ความเห็น",
            medianPrice.Status.ToString(),
            req.Remark));

        // Add comment to the assignee
        currentUser
            .SetDelegatee(assignee.DelegateeId)
            .SetRemark(req.Remark);

        await this.ReplaceDocumentsAsync(medianPrice, false, ct);

        // Save changes to the database
        this.dbContext.PpMedianPrices.Update(medianPrice);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async ValueTask<PpMedianPrice?> FetchMedianPriceAsync(AssigneeCommentRequest req, CancellationToken ct)
    {
        return await this.dbContext.PpMedianPrices
                         .Include(mp => mp.Assignees)
                         .FirstOrDefaultAsync(
                             mp =>
                                 mp.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                                 mp.Id == MedianPriceId.From(req.MedianPriceId),
                             ct);
    }
}