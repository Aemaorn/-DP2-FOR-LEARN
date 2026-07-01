namespace GHB.DP2.Application.Features.Procurement.Committee;

using GHB.DP2.Domain.Procurement;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetCommitRequest(
    Guid ProcurementId,
    CommitType Type);

public enum CommitType
{
    TorDraft,
    MedianPrice,
}

public record CommitteeInfo(
    Guid UserId,
    int Sequence,
    string FullName,
    string EmployeeCode,
    string PositionName,
    string? DepartmentCode,
    string DepartmentName,
    string CommitteePositionsCode,
    string CommitteePositionName,
    bool IsUnableToPerformDuties);

public record ObjectiveNReasonInfo(
    string Objective,
    string Reason,
    string SpecificDescription,
    string? TorTemplate)
{
    public static ObjectiveNReasonInfo CreateDefault()
    {
        return new ObjectiveNReasonInfo(string.Empty, string.Empty, string.Empty, null);
    }
}

public record CommitteeResponse(
    CommitteeInfo[] Committees,
    ObjectiveNReasonInfo ObjAndReason);

public class GetCommitteeEndpoint : EndpointBase<GetCommitRequest, Results<Ok<CommitteeResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetCommitteeEndpoint(ILogger<GetCommitteeEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("/procurement/{ProcurementId:guid}/commit/{Type}");
        this.Description(b => b
                              .WithTags(nameof(Committee))
                              .WithName("GetCommitteeById")
                              .Produces<Ok<IEmailSender<CommitteeInfo>>>()
                              .Produces<NotFound<string>>());
    }

    protected override async ValueTask<Results<Ok<CommitteeResponse>, NotFound<string>>> HandleRequestAsync(GetCommitRequest req, CancellationToken ct)
    {
        var appointQuery =
            this.dbContext.PpAppoints
                .Include(a => a.TorDraftCommittees)
                .ThenInclude(tc => tc.User)
                .ThenInclude(suUser => suUser.Employee)
                .ThenInclude(rawEmployee => rawEmployee.View)
                .Include(a => a.MedianPriceCommittees)
                .ThenInclude(mc => mc.User)
                .Where(p =>
                    p.ProcurementId == ProcurementId.From(req.ProcurementId) &&
                    p.IsActive);

        if (!await appointQuery.AnyAsync(ct))
        {
            return TypedResults.NotFound($"No active appointments found for procurement {req.ProcurementId}.");
        }

        var committee = req.Type switch
        {
            CommitType.TorDraft =>
                appointQuery
                    .SelectMany(s => s.TorDraftCommittees)
                    .ToArrayAsync(ct)
                    .Map(tor => tor.Map(c => new CommitteeInfo(
                        (Guid)c.SuUserId,
                        c.Sequence,
                        c.FullName,
                        (string)c.User.EmployeeCode,
                        c.FullPositionName,
                        c.User.Employee.PrimaryDepartment != null ? (string)c.User.Employee.PrimaryDepartment.Id : string.Empty,
                        c.User.Employee.View != null ? c.User.Employee.View.BusinessUnitName : string.Empty,
                        (string)c.CommitteePositionsCode,
                        c.CommitteePositionsName,
                        false))),
            CommitType.MedianPrice =>
                appointQuery
                    .SelectMany(s => s.MedianPriceCommittees)
                    .ToArrayAsync(ct)
                    .Map(mdp =>
                        mdp.Map(c => new CommitteeInfo(
                            (Guid)c.SuUserId,
                            c.Sequence,
                            c.FullName,
                            (string)c.User.EmployeeCode,
                            c.FullPositionName,
                            c.User.Employee.PrimaryDepartment != null ? (string)c.User.Employee.PrimaryDepartment.Id : string.Empty,
                            c.User.Employee.View != null ? c.User.Employee.View.BusinessUnitName : string.Empty,
                            (string)c.CommitteePositionsCode,
                            c.CommitteePositionsName,
                            false))),
            _ => throw new NotSupportedException("Unsupported committee type."),
        };

        var committeeList = await committee;

        var objective = ObjectiveNReasonInfo.CreateDefault();

        if (req.Type is CommitType.MedianPrice)
        {
            var torData = await this.dbContext
                                    .PpTorDrafts
                                    .Include(c => c.PpTorDraftObjects)
                                    .Include(c => c.PpTorDraftTechnicalSpecifications).Include(ppTorDraft => ppTorDraft.DocumentTemplate)
                                    .FirstOrDefaultAsync(w => w.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

            objective = torData is null
                ? ObjectiveNReasonInfo.CreateDefault()
                : new ObjectiveNReasonInfo(
                    string.Join(
                        Environment.NewLine,
                        torData.PpTorDraftObjects
                               .OrderBy(o => o.Sequence)
                               .Select(s => $"{s.Sequence}. {s.Description}")),
                    torData.Reason ?? string.Empty,
                    string.Join(
                        Environment.NewLine,
                        torData.PpTorDraftTechnicalSpecifications
                               .OrderBy(o => o.Sequence)
                               .Select(s => $"{s.Sequence}. {s.Name ?? string.Empty}")),
                    torData.DocumentTemplate?.Code ?? string.Empty);
        }

        var result = new CommitteeResponse([.. committeeList.OrderBy(c => c.Sequence)], objective);

        return TypedResults.Ok(result);
    }
}