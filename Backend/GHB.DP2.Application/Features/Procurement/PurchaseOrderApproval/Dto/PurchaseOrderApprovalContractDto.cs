namespace GHB.DP2.Application.Features.Procurement.PurchaseOrderApproval.Dto;

using System.ComponentModel;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

public record PurchaseOrderApprovalContractDto(
    [property: Description("รหัสสัญญา")]
    Guid? Id,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รหัสงบประมาณ TOR")]
    Guid? TorDraftBudgetId,
    [property: Description("รหัสผู้ประกอบการใบสั่งซื้อ")]
    Guid? PurchaseOrderEntrepreneurId,
    [property: Description("รหัสงบประมาณอนุมัติหลักการเช่า")]
    Guid? PrincipleApprovalRentalBudgetId,
    [property: Description("รหัสผู้ประกอบการอนุมัติหลักการเช่า")]
    Guid? PrincipleApprovalRentalEntrepreneursId,
    [property: Description("รหัสงบประมาณอนุมัติใบสั่งซื้อ/จ้าง/เช่า")]
    Guid? PurchaseOrderApprovalBudgetId,
    [property: Description("รหัสผู้ประกอบการอนุมัติใบสั่งซื้อ/จ้าง/เช่า")]
    Guid? PurchaseOrderApprovalEntrepreneursId,
    [property: Description("เลขที่สัญญา")]
    string? ContractNumber,
    [property: Description("สามารถแก้ไขเลขที่สัญญาได้")]
    bool HasEditContractNumber,
    [property: Description("ราคาตกลง")]
    decimal AgreedPrice,
    [property: Description("เลขที่ใบสั่งซื้อ")]
    string PoNumber,
    [property: Description("ประเภทคณะกรรมการ")]
    string CommitteeType,
    PurchaseOrderApprovalEntrepreneursDto? Entrepreneurs)
{
    public class Validator : Validator<PurchaseOrderApprovalContractDto>
    {
        public Validator()
        {
            this.RuleFor(x => x.Sequence)
                .GreaterThan(0).WithMessage("ลำดับต้องมากกว่า 0");
            this.RuleFor(x => x.TorDraftBudgetId)
                .NotEmpty().WithMessage("ต้องระบุรหัสงบประมาณ TOR");
            this.RuleFor(x => x.PurchaseOrderEntrepreneurId)
                .NotEmpty().WithMessage("ต้องระบุรหัสผู้ประกอบการ");
            this.RuleFor(x => x.AgreedPrice)
                .GreaterThanOrEqualTo(0).WithMessage("ราคาตกลงต้องเป็นเลขศูนย์หรือมากกว่า");
            this.RuleFor(x => x.PoNumber)
                .NotEmpty().WithMessage("ต้องระบุเลขที่ใบสั่งซื้อ");
            this.RuleFor(x => x.CommitteeType)
                .NotEmpty().WithMessage("ต้องระบุประเภทคณะกรรมการ");
        }
    }
}

public record PurchaseOrderApprovalResponseDto(
    [property: Description("รหัสการอนุมัติใบสั่งซื้อสั่งจ้าง")]
    Guid? Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid ProcurementId,
    [property: Description("ประเภทสัญญา")]
    string ContractType,
    [property: Description("สถานะการอนุมัติ")]
    PurchaseOrderApprovalStatus Status,
    [property: Description("มีสิทธิ์ในการดำเนินการ")]
    bool HasPermission,
    [property: Description("ผู้อนุมัติ")]
    IEnumerable<AcceptorResponse> Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<AssigneeResponse> Assignees,
    [property: Description("กลุ่มงบประมาณสัญญา")]
    IEnumerable<PurchaseOrderApprovalContractGroupResponseDto>? ContractBudgetGroups,
    [property: Description("รหัสการแจ้งข้อมูลเบื้องต้น (จพ.004)")]
    Guid? PurchaseRequisitionId,
    [property: Description("วงเงินงบประมาณ")]
    decimal? ProcurementBudget,
    bool? IsInspectCommittee,
    [property: Description("คณะกรรมการ")] IEnumerable<PurchaseOrderApprovalCommittee>? Committees,
    [property: Description("ผู้รับมอบหมาย (จพ.004)")] IEnumerable<AssigneeResponse>? PurchaseRequisitionAssignees
);

public record PurchaseOrderApprovalCommittee(
    [property: Description("รหัสคณะกรรมการ")]
    Guid? Id,
    [property: Description("ประเภทกลุ่ม")] GroupType GroupType,
    [property: Description("รหัสผู้ใช้")] Guid SuUserId,
    [property: Description("ชื่อเต็ม")] string FullName,
    [property: Description("ชื่อตำแหน่ง")] string PositionName,
    [property: Description("รหัสตำแหน่งกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งกรรมการ")]
    string CommitteePositionsName,
    [property: Description("ลำดับ")] int Sequence,
    string FullPositionName);

public record PurchaseOrderApprovalContractGroupResponseDto(
    [property: Description("รหัสงบประมาณ")]
    Guid BudgetId,
    [property: Description("ลำดับงบประมาณ")]
    int BudgetSequence,
    [property: Description("คำอธิบายงบประมาณ")]
    string BudgetDescription,
    [property: Description("จำนวนงบประมาณ")]
    decimal? Budget,
    [property: Description("สัญญาในกลุ่ม")]
    IEnumerable<PurchaseOrderApprovalContractResponseDto>? Contracts);

public record PurchaseOrderApprovalContractResponseDto(
    [property: Description("รหัสสัญญา")]
    Guid Id,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รหัสผู้ประกอบการใบสั่งซื้อ")]
    Guid? PurchaseOrderEntrepreneurId,
    [property: Description("รหัสผู้ประกอบการอนุมัติหลักการเช่า")]
    Guid? PrincipleApprovalRentalEntrepreneursId,
    [property: Description("รหัสผู้ประกอบการอนุมัติใบสั่งซื้อ/จ้าง/เช่า")]
    Guid? PurchaseOrderApprovalEntrepreneursId,
    [property: Description("ชื่อผู้ประกอบการใบสั่งซื้อ")]
    string PurchaseOrderEntrepreneurName,
    [property: Description("อีเมลผู้ประกอบการใบสั่งซื้อ")]
    string PurchaseOrderEntrepreneurEmail,
    [property: Description("เลขที่สัญญา")]
    string? ContractNumber,
    [property: Description("สามารถแก้ไขเลขที่สัญญาได้")]
    bool HasEditContractNumber,
    [property: Description("ราคาตกลง")]
    decimal AgreedPrice,
    [property: Description("เลขที่ใบสั่งซื้อ")]
    string PoNumber,
    [property: Description("ประเภทคณะกรรมการ")]
    string CommitteeType,
    [property: Description("รหัสผู้ค้า")]
    Guid? VendorId
);

public record PurchaseOrderApprovalBudgetDto(
   [property: Description("รหัสสัญญา")]
   Guid? Id,
   [property: Description("รายการจัดซื้อจัดจ้าง")]
   string Description,
   [property: Description("วงเงิน (บาท)")]
   decimal BudgetAmount
);

public record PurchaseOrderApprovalEntrepreneursDto(
   Guid VendorId,
   int Sequence,
   bool EmailSend
);