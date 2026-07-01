namespace GHB.DP2.Application.Features.SystemUtility.SuRole;

using GHB.DP2.Application.Features.SystemUtility.SuRole.Dto;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetProgram : EndpointBase<Ok<IEnumerable<ProgramPermissionResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetProgram(
        Dp2DbContext dbContext,
        ILogger<GetProgram> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuRole"));
        this.Get("/st/st004/program");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<IEnumerable<ProgramPermissionResponse>>> HandleRequestAsync(CancellationToken ct)
    {
        var programData = await this.dbContext.SuPrograms
                                    .Include(suProgram => suProgram.Parent)
                                    .Where(w => w.Path != null)
                                    .OrderBy(o => o.Sorting)
                                    .AsNoTracking()
                                    .ToListAsync(ct);

        var res = programData.Select(
            s => new ProgramPermissionResponse(
                s.Sorting,
                s.Id.Value,
                s.Code,
                s.Label,
                Permission.None,
                s.Parent?.Label ?? s.Label));

        return TypedResults.Ok(res);
    }
}