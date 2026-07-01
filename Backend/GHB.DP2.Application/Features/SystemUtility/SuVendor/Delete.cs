namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteSuVendorRequest
{
    public Guid Id { get; init; }
}

public class DeleteSuVendor : SecureEndpointBase<DeleteSuVendorRequest, Results<NoContent, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSuVendor(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSuVendor> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Delete("/st/st003/{Id:guid}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, BadRequest<string>>> HandleRequestAsync(DeleteSuVendorRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuVendors
                             .FirstOrDefaultAsync(x => x.Id == SuVendorId.From(req.Id), ct);

        if (data == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        var isCaContractDraftVendorsAnyUsed = await this.dbContext.CaContractDraftVendors
                                  .Where(w => w.Vendor.VendorId == data.Id)
                                  .AnyAsync(ct);

        var isPrincipleApprovalRentalEn = await this.dbContext.PPrincipleApprovalRentalEntrepreneurs
                                                    .AnyAsync(w => w.Vendor.Id == data.Id, ct);

        var isPurchaseOrderEn = await this.dbContext.PJp006S
                                          .SelectMany(s => s.Entrepreneurs)
                                          .Where(w => w.SuVendorId == data.Id)
                                          .AnyAsync(ct);

        var isCamContractEn = await this.dbContext.CamContractAmendmentPoAddendums
                                        .AnyAsync(c => c.Vendor.Id == data.Id, ct);

        if (isCaContractDraftVendorsAnyUsed || isPrincipleApprovalRentalEn || isPurchaseOrderEn || isCamContractEn)
        {
            return TypedResults.BadRequest("ไม่สามารถลบข้อมูลได้ เนื่องจากมีการใช้งานข้อมูลนี้แล้ว");
        }

        this.dbContext.SuVendors.Remove(data);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.NoContent();
    }
}