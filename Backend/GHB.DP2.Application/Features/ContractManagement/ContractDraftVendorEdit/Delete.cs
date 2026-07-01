namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteContractDraftVendorEditRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public class DeleteContractDraftVendorEditEndpoint : EndpointBase<DeleteContractDraftVendorEditRequest, NoContent>
{
    private readonly Dp2DbContext dbContext;

    public DeleteContractDraftVendorEditEndpoint(
        Dp2DbContext dbContext,
        ILogger<DeleteContractDraftVendorEditEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractDraftVendorEdit")
             .WithName("DeleteContractDraftVendorEdit")
             .Produces(204));

        this.Delete("contract/contract-draft-vendor-edit/{id}");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(DeleteContractDraftVendorEditRequest req, CancellationToken ct)
    {
        var contractDraftVendorEdit = await this.dbContext.CaContractDraftVendorEdits
                                                .Include(caContractDraftVendorEdit => caContractDraftVendorEdit.Acceptors)
                                                .Include(caContractDraftVendorEdit => caContractDraftVendorEdit.ContractDraftVendor)
                                                .ThenInclude(caContractDraftVendor => caContractDraftVendor.ContractDraft)
                                                .FirstOrDefaultAsync(e => e.Id == ContractDraftVendorEditId.From(req.Id), ct);

        if (contractDraftVendorEdit is null)
        {
            this.ThrowError("ไม่พบข้อมูล บันทึกต่อท้าย", StatusCodes.Status404NotFound);
        }

        var inCommittee = contractDraftVendorEdit.Acceptors
                                                 .Where(a => a.Type == AcceptorType.AcceptanceCommittee)
                                                 .Any(ap => ap.UserId == req.UserId);

        var procurementId = ProcurementId.From(contractDraftVendorEdit.ProcurementId);
        var userId = UserId.From(req.UserId);

        var inJp005Committee = await this.dbContext.PJp005S
                                          .Where(j => j.ProcurementId == procurementId)
                                          .SelectMany(j => j.Committees)
                                          .AnyAsync(c => c.GroupType == PJp005CommitteeGroupType.ProcurementCommittee && c.SuUserId == userId, ct);

        var poaQuery = this.dbContext.PPurchaseOrderApprovals.Where(p => p.ProcurementId == procurementId);

        var inPoaCommittee = await poaQuery.SelectMany(p => p.Committees)
                                           .AnyAsync(c => c.SuUserId == userId, ct);

        var inPoaAssignee = await poaQuery.SelectMany(p => p.Assignees)
                                          .AnyAsync(a => a.Type == AssigneeType.Assignee && a.UserId == userId, ct);

        var inAssignee = inJp005Committee || inPoaCommittee || inPoaAssignee;

        if (!inCommittee && !inAssignee)
        {
            this.ThrowError("ไม่มีสิทธิ์ดำเนินการ เนื่องจากคุณไม่ได้เป็นกรรมการหรือผู้รับผิดชอบในกระบวนการจัดซื้อจัดจ้างนี้", StatusCodes.Status403Forbidden);
        }

        this.dbContext.CaContractDraftVendorEdits.Remove(contractDraftVendorEdit);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}