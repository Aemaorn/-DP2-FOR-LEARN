namespace GHB.DP2.Application.Features.Plan.Dto;

using System.ComponentModel;

public record PlanSelectedReplate(
    [property: Description("ลำดับแผนประกาศ")]
    int RowNumber,
    [property: Description("เลขที่แผน")]
    string PlanNumber,
    [property: Description("เลขที่ e-GP")]
    string? EgpNumber,
    [property: Description("ชื่อแผน")] string PlanTitle,
    [property: Description("งบประมาณ")] string Budget,
    [property: Description("วันที่คาดว่าจะดำเนินการจัดซื้อจัดจ้าง")]
    string ExpectingProcurementAt);

public record CreatePlanReplace(
    [property: Description("ผู้จัดทำ")] string Signature,
    [property: Description("ชื่อผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้จัดทำ")]
    string PositionName,
    [property: Description("ส่วนและฝ่ายผู้จัดทำ")]
    string? DepartmentName);

public record AcceptorReplace(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ/อนุมัติ")]
    string PositionName,
    [property: Description("ปฏิบัติหน้าที่แทน")]
    string Delegate);

public record PublicPlanReplace(
    [property: Description("ลายเซ็นผู้ประกาศเผยแผน")]
    string? Signature,
    [property: Description("ชื่อผู้ประกาศเผยแผน")]
    string FullName,
    [property: Description("ตำแหน่งผู้ประกาศเผยแผน")]
    string PositionName,
    [property: Description("ผู้ปฏิบัติหน้าที่แทนผู้ประกาศเผยแผน")]
    string Delegate);

public record ChangePlanReplace(
    [property: Description("ชื่อโครงการเก่า")]
    string NameOld,
    [property: Description("งบประมาณโครงการเก่า")]
    string BudgetOld,
    [property: Description("ประมาณกาณช่วงเวลาจัดซื้อจัดจ้างเก่า")]
    string ExpectingProcurementAtOld,
    [property: Description("ชื่อโครงการใหม่")]
    string NameNew,
    [property: Description("งบประมาณโครงการใหม่")]
    string BudgetNew,
    [property: Description("ประมาณกาณช่วงเวลาจัดซื้อจัดจ้างใหม่")]
    string ExpectingProcurementAtNew);