namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoSap;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAmendment.ContractAmendment.PoSap.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoSap;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApprovePoSapRequest(
    Guid ContractAmendmentId,
    Guid Id,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    AcceptorType Group,
    string? Remark);

public class ApprovePoSapEndpoint : PoSapEndpointBase<ApprovePoSapRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    public ApprovePoSapEndpoint(ILogger<ApprovePoSapEndpoint> logger, Dp2DbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment/PoSap")
             .WithName("ApprovePoSap")
             .Accepts<ApprovePoSapRequest>("application/json"));
        this.Post("contract-amendment/{ContractAmendmentId:guid}/po-sap/{Id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApprovePoSapRequest req, CancellationToken ct)
    {
        var poSap =
            await this.DbContext.CamContractAmendmentPoSaps
                      .Include(c => c.Acceptors)
                      .FirstOrDefaultAsync(
                          c =>
                              c.Id == CamContractAmendmentPoSapId.From(req.Id)
                              && c.CamContractAmendmentId == CamContractAmendmentId.From(req.ContractAmendmentId),
                          ct);

        if (poSap is null)
        {
            return TypedResults.NotFound("ไม่พบ PO SAP ที่ระบุ");
        }

        var acceptors = poSap.Acceptors
                             .Where(a => a.Type == req.Group && a is { IsActive: true, Status: AcceptorStatus.Pending })
                             .OrderBy(a => a.Sequence)
                             .Select(DelegatorExtensions.DelegatorToAcceptor)
                             .ToList();

        var current = acceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
                          ? a.UserId == req.UserId
                          : a.Delegatee?.SuUserId == UserId.From(req.UserId)
                            && a.Status == AcceptorStatus.Pending);

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var currentAcceptorUser = poSap.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptorUser.SetCurrent(false);

        if (poSap.Acceptors.Any(a => a.Status == AcceptorStatus.Pending))
        {
            var next =
                acceptors.First(a => a.Status == AcceptorStatus.Pending);

            next.SetCurrent();
        }
        else
        {
            _ = poSap.SetApproved();
        }

        this.DbContext.CamContractAmendmentPoSaps.Update(poSap);
        await this.DbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}