namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using System.Linq;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPettyCashAvailableGlAccountRequest(
    string? Keyword = null,
    string? DepartmentCode = null,
    Guid? PettyCashId = null);

public class GetPettyCashAvailableGlAccountEndpoint : EndpointBase<GetPettyCashAvailableGlAccountRequest, Ok<IEnumerable<PettyCashReimbursementGlItemResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPettyCashAvailableGlAccountEndpoint(ILogger<GetPettyCashAvailableGlAccountEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("petty-cash-reimbursement/gl-accounts");
        this.Description(b => b
                              .WithTags("Procurement/PPettyCashReimbursement")
                              .WithName("GetPettyCashAvailableGlAccounts")
                              .Produces<Ok<IEnumerable<PettyCashReimbursementGlItemResponse>>>()
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Ok<IEnumerable<PettyCashReimbursementGlItemResponse>>> HandleRequestAsync(GetPettyCashAvailableGlAccountRequest req, CancellationToken ct)
    {
        var glAccountsQuery = this.dbContext.PPettyCashGLAccounts
                                  .AsNoTracking()
                                  .Include(g => g.PettyCash)
                                  .ThenInclude(p => p.Department)
                                  .Include(g => g.BudgetType)
                                  .Include(g => g.GLAccount)
                                  .Where(g => !this.dbContext.PPettyCashReimbursementItems
                                                   .Any(i => i.PettyCashGlAccount.Id == g.Id)
                                                   && g.PettyCash.Status == PettyCashStatus.Completed)
                                  .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), g =>
                                      EF.Functions.ILike((string)g.PettyCash.PettyCashNumber, $"%{req.Keyword}%") ||
                                      EF.Functions.ILike(g.PettyCash.Subject, $"%{req.Keyword}%") ||
                                      EF.Functions.ILike(g.GLAccount.Label, $"%{req.Keyword}%"))
                                  .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), g => g.PettyCash.PettyCaseDepartmentCode == req.DepartmentCode!)
                                  .WhereIfTrue(req.PettyCashId.HasValue, g => g.PettyCash.Id == PettyCashId.From(req.PettyCashId!.Value))
                                  .OrderByDescending(g => g.PettyCash.PettyCashDate)
                                  .ThenBy(g => g.PettyCash.PettyCashNumber)
                                  .ThenBy(g => g.Sequence);

        var data = await glAccountsQuery
                         .Select(g => new
                         {
                             PettyCashGlAccountId = g.Id.Value,
                             PettyCashId = g.PettyCash.Id.Value,
                             PettyCashNumber = g.PettyCash.PettyCashNumber.Value,
                             g.PettyCash.PettyCashDate,
                             g.PettyCash.Subject,
                             DepartmentName = g.PettyCash.Department.Name,
                             g.Sequence,
                             g.SoId,
                             BudgetTypeCode = g.BudgetTypeCode.Value,
                             BudgetTypeLabel = g.BudgetType.Label,
                             GlAccountCode = g.GLAccountCode.Value,
                             GlAccountLabel = g.GLAccount.Label,
                             g.ProjectNumber,
                             Amount = g.PettyCash.Vendors.Sum(v => v.VendorParcels.Sum(x => x.TotalPriceVat)),
                         })
                         .ToListAsync(ct);

        var list = data
                   .Select((x, idx) => new PettyCashReimbursementGlItemResponse(
                       null,
                       x.PettyCashGlAccountId,
                       idx + 1,
                       x.PettyCashDate,
                       x.PettyCashNumber,
                       x.Subject,
                       x.SoId,
                       x.DepartmentName,
                       x.BudgetTypeCode,
                       x.BudgetTypeLabel,
                       x.GlAccountCode,
                       x.GlAccountLabel,
                       x.ProjectNumber,
                       x.Amount))
                   .ToList();

        return TypedResults.Ok<IEnumerable<PettyCashReimbursementGlItemResponse>>(list);
    }
}