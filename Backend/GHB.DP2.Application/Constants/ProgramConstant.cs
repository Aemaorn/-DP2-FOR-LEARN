namespace GHB.DP2.Application.Constants;

public static class ProgramConstant
{
    public abstract class Plan
    {
        public const string Url = "/pl/pl001/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "แผนจัดซื้อจัดจ้าง";
    }

    public abstract class ChangePlan
    {
        public const string Name = "ขอเปลี่ยนแปลงแผนจัดซื้อจัดจ้าง";
    }

    public abstract class CancelPlan
    {
        public const string Name = "ขอยกเลิกแผนจัดซื้อจัดจ้าง";
    }

    public abstract class ChangeTor
    {
        public const string Name = "ขอเปลี่ยนแปลงร่างขอบเขตของงาน (TOR)";
    }

    public abstract class CancelTor
    {
        public const string Name = "ขอยกเลิกร่างขอบเขตของงาน (TOR)";
    }

    public abstract class ChangeMedianPrice
    {
        public const string Name = "ขอเปลี่ยนแปลงกำหนดราคากลาง (ราคาอ้างอิง)";
    }

    public abstract class CancelMedianPrice
    {
        public const string Name = "ขอยกเลิกกำหนดราคากลาง (ราคาอ้างอิง)";
    }

    public abstract class PlanAnnouncement
    {
        public const string Url = "/pl/pl002/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง";
    }

    public abstract class Procurement
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "การจัดซื้อจัดจ้าง";
    }

    public abstract class PreProcurementAppointment
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง";
    }

    public abstract class PreProcurementTorDraft
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ร่างขอบเขตของงาน (TOR)";
    }

    public abstract class PreProcurementMedianPrice
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "กำหนดราคากลาง (ราคาอ้างอิง)";
    }

    public abstract class PreProcurementJorPor04
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "การแจ้งข้อมูลเบื้องต้น";
    }

    public abstract class PreProcurementJorPor05
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายงานขอซื้อขอจ้าง (จพ.005)";
    }

    public abstract class PreProcurementInvite
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "หนังสือเชิญชวนผู้ประกอบการ";
    }

    public abstract class PreProcurementJorPor06
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "จัดทำรายงานผลการพิจารณาและขออนุมัติสั่งซื้อ/สั่งจ้าง (จพ.006)";
    }

    public abstract class ProcurementPurchaseOrderApproval
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา";
    }

    public abstract class ContractInvitation
    {
        public const string Url = "/pp/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "หนังสือเชิญชวนทำสัญญา";
    }

    public abstract class ContractDraft
    {
        public const string Url = "/pp/detail/{0}";
        public const string VendorUrl = "/pp/detail/{0}?vendorId={1}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ร่างสัญญาและสัญญา";
    }

    public abstract class ContractAmendment
    {
        public const string Url = "/cam/cam01/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "บันทึกต่อท้ายสัญญา";
    }

    public abstract class CommitteeChange
    {
        public const string Url = "/cam/cam02/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ขอแก้ไขคณะกรรมการ";
    }

    public abstract class W119
    {
        public const string Url = "/pcm/pcm002/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายการจัดซื้อจัดจ้าง ว119";
    }

    public abstract class Urgent79Clause2
    {
        public const string Url = "/pcm/pcm003/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "จัดซื้อจัดจ้าง กรณีเร่งด่วน";
    }

    public abstract class PettyCash
    {
        public const string Url = "/pcm/pcm004/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายการจัดซื้อจัดจ้าง Petty Cash";
    }

    public abstract class PettyCashReimbursement
    {
        public const string Url = "/pcm/pcm006/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "การเบิกเงินชดเชยเงินสดย่อย";
    }

    public abstract class ProcurementList
    {
        public const string Url = "/pl/pl001";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายการจัดซื้อจัดจ้าง";
    }

    public abstract class PublishProcurement
    {
        public const string Url = "/pl/pl002";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ขออนุมัติเผยแพร่จัดซื้อจัดจ้าง";
    }

    public abstract class BranchSpaceRent
    {
        public const string Url = "/pcm/pcm005/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "เช่าพื้นที่ทำการสาขา";
    }

    public abstract class PrincipalApproval
    {
        public const string Url = "/pcm/pcm005/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ขออนุมัติหลักการ";
    }

    public abstract class PrincipalApprovalRental
    {
        public const string Url = "/pcm/pcm005/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ขออนุมัติเช่า";
    }

    public abstract class ContractAcceptance
    {
        public const string Url = "/cm/cm001/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายงานผลการตรวจรับ (จพ.008)";
    }

    public abstract class ContractAcceptancePeriod
    {
        public const string Url = "/cm/cm001/detail/{0}/period/{1}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายงานผลการตรวจรับ (จพ.008)";
    }

    public abstract class DisbursementApproval
    {
        public const string Url = "/cm/cm004/detail/{0}/disbursement/{1}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "บันทึกรายงานผลการตรวจรับ (จพ.008)";
    }

    public abstract class ContractTermination
    {
        public const string Url = "/cm/cm005/contract/{0}/detail/{1}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ขออนุมัติยกเลิกสัญญา";
    }

    public abstract class ContractGuaranteeReturn
    {
        public const string Name = "คืนหลักประกันสัญญา";
        public const string Url = "/cm/cm006/detail/{0}/{1}";
    }

    public abstract class ContractDraftVendorEdit
    {
        public const string Name = "บันทึกต่อท้าย";
        public const string Url = "/cm/cm007/detail/{0}";
    }

    public abstract class CertificatesRequisition
    {
        public const string Url = "/ca/ca02/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "ใบรองรับผลงาน";
    }

    public abstract class ExpenseDisbursement
    {
        public const string Url = "/ac/ac01/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "เบิกจ่าย (สำหรับบัญชี)";
    }

    public abstract class AuditAndRevenue
    {
        public const string Url = "/rp/rp001/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายงานสำนักงานการตรวจเงินแผ่นดินและกรมสรรพากร";
    }

    public abstract class ContractCompletionByQuarter
    {
        public const string Url = "/rp/rp002/detail/{0}";
        public const string Button = "ดูรายละเอียด";
        public const string Name = "รายงานสัญญาแล้วเสร็จตามไตรมาส";
    }
}