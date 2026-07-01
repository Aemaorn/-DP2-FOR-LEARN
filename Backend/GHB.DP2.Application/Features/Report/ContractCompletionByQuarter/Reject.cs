namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record RejectRpContractCompletionByQuartersRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    string? Remark
);

public class RejectRpContractCompletionByQuartersEndpoint : ContractCompletionByQuarterEndpoint<RejectRpContractCompletionByQuartersRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectRpContractCompletionByQuartersEndpoint(ILogger<RejectRpContractCompletionByQuartersEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("report/contract-completion-by-quarter/{id:guid}/reject");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("RejectRpContractCompletionByQuarters")
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .Produces<string>(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(RejectRpContractCompletionByQuartersRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpContractCompletionByQuarters
                               .Include(x => x.Acceptors)
                               .Include(x => x.DocumentHistories)
                               .FirstOrDefaultAsync(x => x.Id == RpContractCompletionByQuarterId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        if (entity.Status != RpContractCompletionByQuarterStatus.WaitingApproval)
        {
            return TypedResults.BadRequest("ปฏิเสธได้เฉพาะสถานะ รอผู้มีอำนาจเห็นชอบ/อนุมัตื เท่านั้น");
        }

        var acceptors = entity.Acceptors
                              .Where(a => a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .Select(DelegatorExtensions.DelegatorToAcceptor)
                              .ToList();

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == req.UserId
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var currentAcceptorUser = entity.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Reject(remark: req.Remark);

        entity.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"ส่งกลับแก้ไข ิ ข้อมูลสัญญาแล้วเสร็จตามไตรมาส",
                entity.Status.ToString(),
                req.Remark));

        entity.SetStatus(RpContractCompletionByQuarterStatus.Rejected);

        await this.ManageDocumentForRejectAsync(entity);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}