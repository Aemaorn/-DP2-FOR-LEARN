namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetCommitteeRequest(Guid ProcurementId, Guid SourceId, string SourceType, string CommitteeGroupType);

public record GetCommitteeResponse(IEnumerable<CommitteeDto> Committees);

public record CommitteeDto(
    Guid SuUserId,
    string FullName,
    string FullPositionName,
    string CommitteePositionsCode,
    string CommitteePositionsName,
    int Sequence);

public class GetCommitteeBySourceType : EndpointBase<GetCommitteeRequest, Results<Ok<GetCommitteeResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetCommitteeBySourceType(
        Dp2DbContext dbContext,
        ILogger<GetCommitteeBySourceType> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Get("/procurement/{ProcurementId:guid}/source/{SourceId:guid}/source-type/{SourceType}/committee-group-type/{CommitteeGroupType}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<GetCommitteeResponse>, NotFound<string>>> HandleRequestAsync(GetCommitteeRequest req, CancellationToken ct)
    {
        var response = (req.SourceType, req.CommitteeGroupType) switch
        {
            ("Appoint", "TOR") => await this.GetAppointTorCommitteeAsync(PpAppointId.From(req.SourceId), ct),
            ("Appoint", "MedianPrice") => await this.GetAppointMedianPriceCommitteeAsync(PpAppointId.From(req.SourceId), ct),
            ("PurchaseRequisition", "ProcurementCommittee") => await this.GetPurchaseRequisitionCommitteeAsync(PpPurchaseRequisitionId.From(req.SourceId), GHB.DP2.Domain.Procurement.PpPurchaseRequisition.GroupType.ProcurementCommittee, ct),
            ("PurchaseRequisition", "MaintenanceInspectionCommittee") => await this.GetPurchaseRequisitionCommitteeAsync(PpPurchaseRequisitionId.From(req.SourceId), GHB.DP2.Domain.Procurement.PpPurchaseRequisition.GroupType.MaintenanceInspectionCommittee, ct),
            ("PurchaseRequisition", "InspectionCommittee") => await this.GetPurchaseRequisitionCommitteeAsync(PpPurchaseRequisitionId.From(req.SourceId), GHB.DP2.Domain.Procurement.PpPurchaseRequisition.GroupType.InspectionCommittee, ct),
            ("PurchaseRequisition", "ConstructionSupervisor") => await this.GetPurchaseRequisitionCommitteeAsync(PpPurchaseRequisitionId.From(req.SourceId), GHB.DP2.Domain.Procurement.PpPurchaseRequisition.GroupType.ConstructionSupervisor, ct),
            ("Jp005", "ProcurementCommittee") => await this.GetJp05CommitteeAsync(PJp005Id.From(req.SourceId), PJp005CommitteeGroupType.ProcurementCommittee, ct),
            ("Jp005", "InspectionCommittee") => await this.GetJp05CommitteeAsync(PJp005Id.From(req.SourceId), PJp005CommitteeGroupType.InspectionCommittee, ct),
            ("PrincipleApproval", "RentCommittee") => await this.GetPrincipleApprovalCommitteeAsync(PPrincipleApprovalId.From(req.SourceId), CommitteeGroupType.RentCommittee, ct),
            ("PrincipleApproval", "AcceptanceCommittee") => await this.GetPrincipleApprovalCommitteeAsync(PPrincipleApprovalId.From(req.SourceId), CommitteeGroupType.AcceptanceCommittee, ct),
            ("PurchaseOrderApproval", "InspectionCommittee") => await this.GetPurchaseOrderApprovalCommitteeAsync(PurchaseOrderApprovalId.From(req.SourceId), Domain.Procurement.PPurchaseOrderApproval.GroupType.InspectionCommittee, ct),
            _ => throw new ArgumentOutOfRangeException(),
        };

        return TypedResults.Ok(response);
    }

    private async Task<GetCommitteeResponse> GetAppointTorCommitteeAsync(PpAppointId appointId, CancellationToken ct = default)
    {
        var committee = await this.dbContext.PpAppoints
                                  .Include(a => a.TorDraftCommittees)
                                  .Where(a => a.Id == appointId)
                                  .SelectMany(a => a.TorDraftCommittees)
                                  .Select(a => new CommitteeDto(
                                      a.SuUserId.Value,
                                      a.FullName,
                                      a.FullPositionName,
                                      a.CommitteePositionsCode.Value,
                                      a.CommitteePositionsName,
                                      a.Sequence))
                                  .ToListAsync(ct);

        return new GetCommitteeResponse(committee);
    }

    private async Task<GetCommitteeResponse> GetAppointMedianPriceCommitteeAsync(PpAppointId appointId, CancellationToken ct = default)
    {
        var committee = await this.dbContext.PpAppoints
                                  .Include(a => a.MedianPriceCommittees)
                                  .Where(a => a.Id == appointId)
                                  .SelectMany(a => a.MedianPriceCommittees)
                                  .Select(a => new CommitteeDto(
                                      a.SuUserId.Value,
                                      a.FullName,
                                      a.FullPositionName,
                                      a.CommitteePositionsCode.Value,
                                      a.CommitteePositionsName,
                                      a.Sequence))
                                  .ToListAsync(ct);

        return new GetCommitteeResponse(committee);
    }

    private async Task<GetCommitteeResponse> GetPurchaseRequisitionCommitteeAsync(PpPurchaseRequisitionId purchaseRequisitionId, GHB.DP2.Domain.Procurement.PpPurchaseRequisition.GroupType groupType, CancellationToken ct = default)
    {
        var committee = await this.dbContext.PpPurchaseRequisitions
                                  .Include(p => p.Committees)
                                  .Where(p => p.Id == purchaseRequisitionId)
                                  .SelectMany(p => p.Committees)
                                  .Where(p => p.GroupType == groupType)
                                  .Select(p => new CommitteeDto(
                                      p.SuUserId.Value,
                                      p.FullName,
                                      string.Empty,
                                      p.CommitteePositionsCode.Value,
                                      p.CommitteePositionsName,
                                      p.Sequence))
                                  .ToListAsync(ct);

        return new GetCommitteeResponse(committee);
    }

    private async Task<GetCommitteeResponse> GetJp05CommitteeAsync(PJp005Id jp005Id, PJp005CommitteeGroupType groupType, CancellationToken ct = default)
    {
        var committee = await this.dbContext.PJp005S
                                  .Include(p => p.Committees)
                                  .Where(p => p.Id == jp005Id)
                                  .SelectMany(p => p.Committees)
                                  .Where(p => p.GroupType == groupType)
                                  .Select(p => new CommitteeDto(
                                      p.SuUserId.Value,
                                      p.FullName,
                                      p.FullPositionName,
                                      p.CommitteePositionsCode.Value,
                                      p.CommitteePositionsName,
                                      p.Sequence))
                                  .ToListAsync(ct);

        return new GetCommitteeResponse(committee);
    }

    private async Task<GetCommitteeResponse> GetPurchaseOrderApprovalCommitteeAsync(PurchaseOrderApprovalId purchaseOrderApprovalId, Domain.Procurement.PPurchaseOrderApproval.GroupType groupType, CancellationToken ct = default)
    {
        var committee = await this.dbContext.PPurchaseOrderApprovals
                                  .Include(p => p.Committees)
                                  .Where(p => p.Id == purchaseOrderApprovalId)
                                  .SelectMany(p => p.Committees)
                                  .Where(p => p.GroupType == groupType)
                                  .Select(p => new CommitteeDto(
                                      p.SuUserId.Value,
                                      p.FullName,
                                      p.FullPositionName,
                                      p.CommitteePositionsCode.Value,
                                      p.CommitteePositionsName,
                                      p.Sequence))
                                  .ToListAsync(ct);

        return new GetCommitteeResponse(committee);
    }

    private async Task<GetCommitteeResponse> GetPrincipleApprovalCommitteeAsync(PPrincipleApprovalId principleApprovalId, CommitteeGroupType groupType, CancellationToken ct = default)
    {
        var committee = await this.dbContext.PPrincipleApprovals
                                  .Include(p => p.PrincipleApprovalCommittees)
                                  .Where(p => p.Id == principleApprovalId)
                                  .SelectMany(p => p.PrincipleApprovalCommittees)
                                  .Where(p => p.GroupType == groupType)
                                  .Select(p => new CommitteeDto(
                                      p.SuUserId.Value,
                                      p.FullName,
                                      p.FullPositionName,
                                      p.CommitteePositionsCode.Value,
                                      p.CommitteePositionsName,
                                      p.Sequence))
                                  .ToListAsync(ct);

        return new GetCommitteeResponse(committee);
    }
}