namespace GHB.DP2.Application.Features.Plan.PlanAnnouncement.DTO;

using System.ComponentModel;
using GHB.DP2.Domain.Plan;

public class PlanSelectedInfo
{
    [Description("รหัสแผน")]
    public Guid PlanId { get; set; }

    [Description("เลขที่แผน")]
    public string PlanNumber { get; set; }

    [Description("ชื่อแผน")]
    public string PlanTitle { get; set; }

    [Description("งบประมาณ")]
    public decimal Budget { get; set; }

    [Description("ชื่อหน่วยงาน")]
    public string DepartmentName { get; set; }

    [Description("ชื่อวิธีการจัดหา")]
    public string SupplyMethodName { get; set; }

    [Description("ชื่อประเภทวิธีการจัดหา")]
    public string? SupplyMethodTypeName { get; set; }

    [Description("เลข eGP")]
    public string? EgpNumber { get; set; }

    [Description("ขอเปลี่ยนแปลง")]
    public bool IsChange { get; set; }

    [Description("ขอยกเลิก")]
    public bool IsCancel { get; set; }
}

public class PlanAnnouncementSelectedDto : PlanSelectedInfo
{
    [Description("รหัสแผนที่เลือก")]
    public Guid Id { get; set; }

    [Description("รหัสแผนต้นทาง")]
    public PlanId? RefId { get; set; }
}

public record PlanSelectedRequest(Guid? Id, Guid PlanId, string? EgpNumber);