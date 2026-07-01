namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.ExpenseDisbursement.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class RejectExpenseDisbursementRequest
{
    public Guid Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remarks { get; init; }
}

public class RejectExpenseDisbursementEndpoint : ExpenseDisbursementAbstractEndpoint<RejectExpenseDisbursementRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectExpenseDisbursementEndpoint(
        ILogger<RejectExpenseDisbursementEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("expense-disbursement/{Id:guid}/reject");
        this.Options(b => b
            .WithTags("Procurement/ExpenseDisbursement")
            .WithName("RejectExpenseDisbursement")
            .Produces<Ok>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectExpenseDisbursementRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PExpenseDisbursements
            .Include(e => e.Acceptors)
            .ThenInclude(e => e.User)
            .ThenInclude(e => e.Employee)
            .SingleOrDefaultAsync(e => e.Id == PExpenseDisbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล เบิกจ่าย");
        }

        if (entity.Status != PExpenseDisbursementStatus.WaitingApproval)
        {
            return TypedResults.BadRequest("ไม่สามารถตีกลับเอกสารในสถานะนี้ได้");
        }

        var approvers = entity.Acceptors
            .Where(a => a.Type == AcceptorType.Approver && a.Status == AcceptorStatus.Pending && a.IsActive)
            .ToArray();

        var acceptor = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                                .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == req.UserId
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติที่ใช้งานได้");
        }

        if (!acceptor.ArePreviousAcceptorsApproved(entity.Acceptors))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        var currentAcceptorUser = entity.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remarks);

        entity.SetRejected(req.Remarks);

        this.dbContext.PExpenseDisbursements.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}