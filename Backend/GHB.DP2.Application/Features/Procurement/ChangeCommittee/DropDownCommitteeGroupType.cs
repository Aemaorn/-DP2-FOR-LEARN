namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using GHB.DP2.Application.Features.Dropdown;
using GHB.DP2.Domain.Procurement.ChangeCommittee;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DropDownCommitteeGroupTypeResponse(
    string Value,
    string Label);

public class DropDownCommitteeGroupType : EndpointBase<Ok<IEnumerable<DropDownCommitteeGroupTypeResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public DropDownCommitteeGroupType(Dp2DbContext dbContext, ILogger<GetRoleCode> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/committee-group-type");
    }

    protected override async ValueTask<Ok<IEnumerable<DropDownCommitteeGroupTypeResponse>>> HandleRequestAsync(CancellationToken ct)
    {
        var query = await this.dbContext.CommitteeChanges
                              .AsNoTracking()
                              .Include(c => c.Procurement)
                              .Include(c => c.Acceptors)
                              .ThenInclude(a => a.User)
                              .ToListAsync(ct);

        var result = query.Map(c => new DropDownCommitteeGroupTypeResponse(
            c.CommitteeType.ToString(),
            GetCommitteeGroupTypeName(c.CommitteeType)));

        return TypedResults.Ok(result);
    }

    private static string GetCommitteeGroupTypeName(CommitteeType committeeType)
    {
        return committeeType switch
        {
            CommitteeType.TOR => "บุคคล/คณะกรรมการจัดทำร่างขอบเขตงาน",
            CommitteeType.MedianPrice => "บุคคล/คณะกรรมการกำหนดราคากลาง",
            CommitteeType.ProcurementCommittee => "ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง",
            CommitteeType.InspectionCommittee => "ผูู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ",
            CommitteeType.MaintenanceInspectionCommittee => "คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)",
            CommitteeType.ConstructionSupervisor => "ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)",
            _ => committeeType.ToString(),
        };
    }
}