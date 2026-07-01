namespace GHB.DP2.Application.Features.WorkList.Programs;

using GHB.DP2.Application.Common;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Infrastructure;
using Microsoft.EntityFrameworkCore;

internal sealed class ContractAmendmentProgram
{
    public async Task<SectionResult<ContractAmendmentItem>?> ProcessContractAmendmentSectionAsync(
        IDbContextFactory<Dp2ReadOnlyDbContext> dbContextFactory,
        GetWorklistSeparatedRequest req,
        CancellationToken ct)
    {
        if (!req.IncludeContractAmendment)
        {
            return null;
        }

        var topN = Math.Max(req.PageNumber, 1) * req.PageSize;

        await using var ctxAmendmentList = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxAmendmentCount = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxEditList = await dbContextFactory.CreateDbContextAsync(ct);
        await using var ctxEditCount = await dbContextFactory.CreateDbContextAsync(ct);

        var amendmentListTask = ctxAmendmentList.CamContractAmendments
            .AsNoTracking()
            .Include(a => a.ContractDraftVendor)
                .ThenInclude(v => v.Vendor)
            .Include(a => a.ContractDraftVendor)
                .ThenInclude(cd => cd.ContractDraft)
                .ThenInclude(cd => cd.Procurement)
                .ThenInclude(p => p.Department)
            .OrderByDescending(a => a.ContractDraftVendor!.ContractSignedDate)
            .ThenBy(a => a.CamContractAmendmentNumber)
            .Take(topN)
            .ToListAsync(ct);

        var amendmentCountTask = ctxAmendmentCount.CamContractAmendments.CountAsync(ct);

        var editListTask = ctxEditList.CaContractDraftVendorEdits
            .AsNoTracking()
            .Include(e => e.ContractType)
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.ContractSignedDate)
            .Take(topN)
            .ToListAsync(ct);

        var editCountTask = ctxEditCount.CaContractDraftVendorEdits
            .Where(e => !e.IsDeleted)
            .CountAsync(ct);

        await Task.WhenAll(amendmentListTask, amendmentCountTask, editListTask, editCountTask);

        var amendments = await amendmentListTask;
        var amendmentCount = await amendmentCountTask;
        var edits = await editListTask;
        var editCount = await editCountTask;

        var amendmentItems = amendments.Select(MapContractAmendment);
        var editItems = edits.Select(MapContractDraftVendorEdit);

        var merged = amendmentItems.Concat(editItems)
                                   .OrderByDescending(x => x.ContractSignedDate)
                                   .ThenBy(x => x.CamContractAmendmentNumber)
                                   .ToList();

        var skip = Math.Max(0, (req.PageNumber - 1) * req.PageSize);
        var pageItems = merged.Skip(skip).Take(req.PageSize).ToList();
        var result = new PaginatedQueryResult<ContractAmendmentItem>(pageItems, amendmentCount + editCount);

        return new SectionResult<ContractAmendmentItem>(result);
    }

    private static ContractAmendmentItem MapContractAmendment(CamContractAmendment a) => new(
        a.Id.Value,
        a.CamContractAmendmentNumber.Value,
        a.Type.ToString(),
        a.Status.ToString(),
        a.Remark,
        a.ContractDraftVendor?.Id,
        a.ContractDraftVendor?.ContractNumber ?? string.Empty,
        a.ContractDraftVendor?.PoNumber ?? string.Empty,
        a.ContractDraftVendor?.ContractName ?? string.Empty,
        a.ContractDraftVendor?.ContractSignedDate,
        a.ContractDraftVendor?.Budget ?? 0m,
        a.ContractDraftVendor?.ContractDraft?.Procurement?.Department?.Name ?? string.Empty,
        a.ContractDraftVendor?.ContractType?.Label ?? string.Empty);

    private static ContractAmendmentItem MapContractDraftVendorEdit(CaContractDraftVendorEdit e) => new(
        e.Id.Value,
        e.ContractNumber ?? string.Empty,
        "ContractDraftEditVendor",
        e.Status.ToString(),
        null,
        e.ContractDraftVendorId,
        e.ContractNumber ?? string.Empty,
        e.ContractName ?? string.Empty,
        e.PoNumber ?? string.Empty,
        e.ContractSignedDate,
        e.Budget,
        string.Empty,
        e.ContractType?.Label);
}
