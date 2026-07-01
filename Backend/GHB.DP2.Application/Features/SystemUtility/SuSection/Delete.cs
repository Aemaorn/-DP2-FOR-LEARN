namespace GHB.DP2.Application.Features.SystemUtility.SuSection;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteSuSectionRequest(string Id);

public class DeleteSuSection : SecureEndpointBase<DeleteSuSectionRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSuSection(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSuSection> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSection"));
        this.Delete("/st/sections/{Id}");
        this.AuditLog("จัดการข้อมูลส่วนงาน", "ลบข้อมูลส่วนงาน");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(DeleteSuSectionRequest req, CancellationToken ct)
    {
        var sectionId = SectionId.From(req.Id);

        var section = await this.dbContext.SuSections
            .Include(x => x.Approvers)
            .SingleOrDefaultAsync(x => x.Id == sectionId, ct);

        if (section is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลส่วนงาน");
        }

        this.dbContext.SuSections.Remove(section);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}