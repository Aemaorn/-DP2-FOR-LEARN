import { Cm007AccordionTab, Cm007Status } from "@/enums/CM/cm007";
import type { ColorLabel } from "@/models/shared/color";
import type { Component } from "vue";

const Cm007StatusName = (status?: Cm007Status): string => {
  switch (status) {
    case Cm007Status.All:
      return 'ทั้งหมด';
    case Cm007Status.Draft:
      return "แบบร่าง";
    case Cm007Status.Editing:
      return "อยู่ระหว่างแก้ไข";
    case Cm007Status.Rejected:
      return "ส่งกลับแก้ไข";
    case Cm007Status.WaitingCommitteeApproval:
      return "อยู่ระหว่าง คกก. เห็นชอบ";
    case Cm007Status.WaitingAssignment:
      return "รอ จพ. มอบหมาย";
    case Cm007Status.WaitingComment:
      return "อยู่ระหว่าง จพ. ให้ความเห็น";
    case Cm007Status.WaitingApproval:
      return "รออนุมัติ";
    case Cm007Status.Approved:
      return "ตรวจสอบแล้ว";
    case Cm007Status.RejectedToAssignee:
      return "ส่งกลับแก้ไข";
    case Cm007Status.WaitingAddendumAssignment:
      return "รอมอบหมายผู้จัดทำบันทึกต่อท้าย";
    case Cm007Status.WaitingDraftAddendum:
      return "รอร่างเอกสารบันทึกต่อท้าย";
    case Cm007Status.WaitingReview:
      return "รอตรวจสอบ";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const Cm007BadgeStatus = (value?: Cm007Status): ColorLabel => {
  const label = Cm007StatusName(value);

  switch (value) {
    case Cm007Status.All:
    case Cm007Status.Draft:
      return { color: "gray", label };

    case Cm007Status.Editing:
      return { color: "blue", label };

    case Cm007Status.WaitingCommitteeApproval:
    case Cm007Status.WaitingAssignment:
    case Cm007Status.WaitingComment:
    case Cm007Status.WaitingApproval:
      return { color: "yellow", label };

    case Cm007Status.Approved:
      return { color: "green", label };

    case Cm007Status.Rejected:
    case Cm007Status.RejectedToAssignee:
      return { color: "red", label };

    case Cm007Status.WaitingAddendumAssignment:
    case Cm007Status.WaitingDraftAddendum:
    case Cm007Status.WaitingReview:
      return { color: "yellow", label };

    default:
      return { color: "red", label };
  }
};

const Cm007AccordionTabName = (value: Cm007AccordionTab): string => {
  switch (value) {
    case Cm007AccordionTab.Committee:
      return 'คณะกรรมการตรวจรับ';
    case Cm007AccordionTab.Assignee:
      return 'เจ้าหน้าที่พัสดุให้ความเห็น';
    case Cm007AccordionTab.Acceptor:
      return 'ผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case Cm007AccordionTab.AddendumDrafter:
      return 'มอบหมายผู้รับผิดชอบบันทึกต่อท้ายสัญญา';
    case Cm007AccordionTab.Reviewer:
      return 'ผู้ตรวจสอบเอกสารบันทึกต่อท้าย';
    default:
      return 'เกิดข้อผิดพลาด';
  }
};

const TEMPLATE_SECTION_MAP: Record<string, { componentCode: string; label: string }[]> = {
  // Template1 — CFormat002 (สัญญาซื้อขาย)
  CFormat002: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'CAPurchase2', label: 'สัญญาข้อ 2 การรับรองคุณภาพ' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Delivery', label: 'สัญญาข้อ 4 การส่งมอบ' },
    { componentCode: 'CAPurchase5', label: 'สัญญาข้อ 5 การตรวจรับ' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 6 การชำระเงิน' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'CAPurchase9', label: 'สัญญาข้อ 9 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 10 ค่าปรับ' },
    { componentCode: 'CAPurchase11', label: 'สัญญาข้อ 11 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'CAPurchase12', label: 'สัญญาข้อ 12 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ' },
    { componentCode: 'CAPurchase13', label: 'สัญญาข้อ 13 การใช้เรือไทย' },
  ],
  // Template2 — CFormat003 (สัญญาจะขายจะซื้อฯ)
  CFormat003: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PurchaseOpenEnd2', label: 'สัญญาข้อ 2 การรับรองคุณภาพ' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'PurchaseOpenEnd4', label: 'สัญญาข้อ 4 การออกใบสั่งซื้อแต่ละคราว' },
    { componentCode: 'PurchaseOpenEnd5', label: 'สัญญาข้อ 5 การส่งมอบ' },
    { componentCode: 'PurchaseOpenEnd6', label: 'สัญญาข้อ 6 การตรวจรับ' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 7 การชำระเงิน' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 8 การรับประกันความชำรุดบกพร่อง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 9 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'PurchaseOpenEnd10', label: 'สัญญาข้อ 10 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 11 ค่าปรับ' },
    { componentCode: 'PurchaseOpenEnd12', label: 'สัญญาข้อ 12 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'PurchaseOpenEnd13', label: 'สัญญาข้อ 13 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ' },
    { componentCode: 'PurchaseOpenEnd14', label: 'สัญญาข้อ 14 การใช้เรือไทย' },
  ],
  // Template3 — CFormat004
  CFormat004: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PurchaseComputer2', label: 'สัญญาข้อ 2 การรับรองคุณภาพ' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Delivery', label: 'สัญญาข้อ 4 การส่งมอบและติดตั้ง' },
    { componentCode: 'PurchaseComputer5', label: 'สัญญาข้อ 5 การตรวจรับ' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 6 การชำระเงิน' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'PurchaseComputer9', label: 'สัญญาข้อ 9 การโอนกรรมสิทธิ์' },
    { componentCode: 'PurchaseComputer10', label: 'สัญญาข้อ 10 การอบรม' },
    { componentCode: 'PurchaseComputer11', label: 'สัญญาข้อ 11 คู่มือการใช้คอมพิวเตอร์' },
    { componentCode: 'PurchaseComputer12', label: 'สัญญาข้อ 12 การรับประกันความเสียหาย' },
    { componentCode: 'PurchaseComputer13', label: 'สัญญาข้อ 13 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 14 ค่าปรับ' },
    { componentCode: 'PurchaseComputer15', label: 'สัญญาข้อ 15 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'PurchaseComputer16', label: 'สัญญาข้อ 16 การงดหรือลดค่าปรับ หรือขยายเวลาในการปฏิบัติตามสัญญา' },
    { componentCode: 'PurchaseComputer17', label: 'สัญญาข้อ 17 การใช้เรือไทย' },
  ],
  // Template4 — CFormat005
  CFormat005: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'PurchaseSoftwareLicenseAnd1', label: 'สัญญาข้อ 1 คำนิยาม' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 2 ข้อตกลงซื้อขายและอนุญาตให้ใช้สิทธิ' },
    { componentCode: 'PurchaseSoftwareLicenseAnd3', label: 'สัญญาข้อ 3 การรับรองและการอนุญาตให้ใช้สิทธิ' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 4 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Delivery', label: 'สัญญาข้อ 5 การส่งมอบและการจ่ายเงิน' },
    { componentCode: 'PurchaseSoftwareLicenseAnd6', label: 'สัญญาข้อ 6 การตรวจรับ' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 7 การชำระเงิน' },
    { componentCode: 'PurchaseSoftwareLicenseAnd8', label: 'สัญญาข้อ 8 สิทธิของผู้ซื้อ' },
    { componentCode: 'DefectWarranty', label: 'สัญญาข้อ 9 การรับประกันความชำรุดบกพร่อง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 10 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'PurchaseSoftwareLicenseAnd11', label: 'สัญญาข้อ 11 การอบรม' },
    { componentCode: 'PurchaseSoftwareLicenseAnd12', label: 'สัญญาข้อ 12 คู่มือการใช้โปรแกรมคอมพิวเตอร์และการให้คำแนะนำ' },
    { componentCode: 'PurchaseSoftwareLicenseAnd13', label: 'สัญญาข้อ 13 การรักษาความลับทางการค้า' },
    { componentCode: 'PurchaseSoftwareLicenseAnd14', label: 'สัญญาข้อ 14 ความคุ้มครองเกี่ยวกับลิขสิทธิ์' },
    { componentCode: 'PurchaseSoftwareLicenseAnd15', label: 'สัญญาข้อ 15 โปรแกรมคอมพิวเตอร์ที่ได้รับการแก้ไขพัฒนาให้ดีขึ้น' },
    { componentCode: 'PurchaseSoftwareLicenseAnd16', label: 'สัญญาข้อ 16 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 17 ค่าปรับ' },
    { componentCode: 'PurchaseSoftwareLicenseAnd18', label: 'สัญญาข้อ 18 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'PurchaseSoftwareLicenseAnd19', label: 'สัญญาข้อ 19 การส่งคืนโปรแกรมคอมพิวเตอร์' },
    { componentCode: 'PurchaseSoftwareLicenseAnd20', label: 'สัญญาข้อ 20 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ' },
  ],
  // Template5 — CFormat012
  CFormat012: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Exchange3', label: 'สัญญาข้อ 3 การตรวจสอบสิ่งของ' },
    { componentCode: 'Exchange4', label: 'สัญญาข้อ 4 การรับรองคุณภาพ' },
    { componentCode: 'Delivery', label: 'สัญญาข้อ 5 การส่งมอบและการจ่ายเงิน' },
    { componentCode: 'Exchange6', label: 'สัญญาข้อ 6 การตรวจรับ' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'Exchange9', label: 'สัญญาข้อ 9 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 10 ค่าปรับ' },
    { componentCode: 'Exchange10', label: 'สัญญาข้อ 11 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ' },
    { componentCode: 'Exchange11', label: 'สัญญาข้อ 12 ข้อจำกัดความรับผิดของผู้ให้แลกเปลี่ยน' },
    { componentCode: 'Exchange12', label: 'สัญญาข้อ 13 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
  ],
  // Template6 — CFormat001 / CFormat016
  CFormat001: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 3 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน' },
    { componentCode: 'AdvancePayment', label: 'สัญญาข้อ 5 เงินค่าจ้างล่วงหน้า' },
    { componentCode: 'RetentionPayment', label: 'สัญญาข้อ 6 การหักเงินประกันผลงาน' },
    { componentCode: 'TerminationInfoDuration', label: 'สัญญาข้อ 7 (ก) กำหนดเวลาแล้วเสร็จและสิทธิ์ของผู้ว่าจ้างในการบอกเลิกสัญญา' },
    { componentCode: 'TerminationInfoDate', label: 'สัญญาข้อ 7 (ข) กำหนดเวลาแล้วเสร็จและสิทธิ์ของผู้ว่าจ้างในการบอกเลิกสัญญา' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 8 ความรับผิดชอบในความชำรุดบกพร่องของงานจ้าง' },
    { componentCode: 'HireConstruction9', label: 'สัญญาข้อ 9 การจ้างช่วง' },
    { componentCode: 'HireConstruction10', label: 'สัญญาข้อ 10 การควบคุมงานของผู้รับจ้าง' },
    { componentCode: 'HireConstruction11', label: 'สัญญาข้อ 11 ความรับผิดของผู้รับจ้าง' },
    { componentCode: 'HireConstruction12', label: 'สัญญาข้อ 12 การจ่ายเงินแก่ลูกจ้าง' },
    { componentCode: 'HireConstruction13', label: 'สัญญาข้อ 13 การตรวจงานจ้าง' },
    { componentCode: 'HireConstruction14', label: 'สัญญาข้อ 14 แบบรูปและรายการละเอียดคลาดเคลื่อน' },
    { componentCode: 'HireConstruction15', label: 'สัญญาข้อ 15 การควบคุมงานโดยผู้ว่าจ้าง' },
    { componentCode: 'HireConstruction16', label: 'สัญญาข้อ 16 งานพิเศษและการแก้ไขงาน' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 17 ค่าปรับ' },
    { componentCode: 'HireConstruction18', label: 'สัญญาข้อ 18 สิทธิของผู้ว่าจ้างภายหลังบอกเลิกสัญญา' },
    { componentCode: 'HireConstruction19', label: 'สัญญาข้อ 19 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'HireConstruction20', label: 'สัญญาข้อ 20 การทำบริเวณก่อสร้างให้เรียบร้อย' },
    { componentCode: 'HireConstruction21', label: 'สัญญาข้อ 21 การงดหรือลดค่าปรับ หรือการขยายเวลาปฏิบัติงานตามสัญญา' },
    { componentCode: 'HireConstruction22', label: 'สัญญาข้อ 22 การใช้เรือไทย' },
    { componentCode: 'HireConstruction23', label: 'สัญญาข้อ 23 มาตรฐานฝีมือช่าง' },
  ],
  CFormat016: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 3 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน' },
    { componentCode: 'AdvancePayment', label: 'สัญญาข้อ 5 เงินค่าจ้างล่วงหน้า' },
    { componentCode: 'RetentionPayment', label: 'สัญญาข้อ 6 การหักเงินประกันผลงาน' },
    { componentCode: 'TerminationInfoDuration', label: 'สัญญาข้อ 7 (ก) กำหนดเวลาแล้วเสร็จและสิทธิ์ของผู้ว่าจ้างในการบอกเลิกสัญญา' },
    { componentCode: 'TerminationInfoDate', label: 'สัญญาข้อ 7 (ข) กำหนดเวลาแล้วเสร็จและสิทธิ์ของผู้ว่าจ้างในการบอกเลิกสัญญา' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 8 ความรับผิดชอบในความชำรุดบกพร่องของงานจ้าง' },
    { componentCode: 'HireConstruction9', label: 'สัญญาข้อ 9 การจ้างช่วง' },
    { componentCode: 'HireConstruction10', label: 'สัญญาข้อ 10 การควบคุมงานของผู้รับจ้าง' },
    { componentCode: 'HireConstruction11', label: 'สัญญาข้อ 11 ความรับผิดของผู้รับจ้าง' },
    { componentCode: 'HireConstruction12', label: 'สัญญาข้อ 12 การจ่ายเงินแก่ลูกจ้าง' },
    { componentCode: 'HireConstruction13', label: 'สัญญาข้อ 13 การตรวจงานจ้าง' },
    { componentCode: 'HireConstruction14', label: 'สัญญาข้อ 14 แบบรูปและรายการละเอียดคลาดเคลื่อน' },
    { componentCode: 'HireConstruction15', label: 'สัญญาข้อ 15 การควบคุมงานโดยผู้ว่าจ้าง' },
    { componentCode: 'HireConstruction16', label: 'สัญญาข้อ 16 งานพิเศษและการแก้ไขงาน' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 17 ค่าปรับ' },
    { componentCode: 'HireConstruction18', label: 'สัญญาข้อ 18 สิทธิของผู้ว่าจ้างภายหลังบอกเลิกสัญญา' },
    { componentCode: 'HireConstruction19', label: 'สัญญาข้อ 19 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'HireConstruction20', label: 'สัญญาข้อ 20 การทำบริเวณก่อสร้างให้เรียบร้อย' },
    { componentCode: 'HireConstruction21', label: 'สัญญาข้อ 21 การงดหรือลดค่าปรับ หรือการขยายเวลาปฏิบัติงานตามสัญญา' },
    { componentCode: 'HireConstruction22', label: 'สัญญาข้อ 22 การใช้เรือไทย' },
    { componentCode: 'HireConstruction23', label: 'สัญญาข้อ 23 มาตรฐานฝีมือช่าง' },
  ],
  // Template7 — CFormat013
  CFormat013: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 3 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน' },
    { componentCode: 'AdvancePayment', label: 'สัญญาข้อ 5 เงินค่าจ้างล่วงหน้า' },
    { componentCode: 'TerminationInfoDate', label: 'สัญญาข้อ 6 กำหนดเวลาแล้วเสร็จและสิทธิของผู้ว่าจ้างในการบอกเลิกสัญญา' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 7 ความรับผิดชอบในความชำรุดบกพร่องของงานจ้าง' },
    { componentCode: 'HireCustomWork8', label: 'สัญญาข้อ 8 การจ้างช่วง' },
    { componentCode: 'HireCustomWork9', label: 'สัญญาข้อ 9 ความรับผิดของผู้รับจ้าง' },
    { componentCode: 'HireCustomWork10', label: 'สัญญาข้อ 10 การจ่ายเงินแก่ลูกจ้าง' },
    { componentCode: 'HireCustomWork11', label: 'สัญญาข้อ 11 การตรวจรับงานจ้าง' },
    { componentCode: 'HireCustomWork12', label: 'สัญญาข้อ 12 รายละเอียดของงานจ้างคลาดเคลื่อน' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 13 ค่าปรับ' },
    { componentCode: 'HireCustomWork14', label: 'สัญญาข้อ 14 สิทธิของผู้ว่าจ้างภายหลังบอกเลิกสัญญา' },
    { componentCode: 'HireCustomWork15', label: 'สัญญาข้อ 15 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'HireCustomWork16', label: 'สัญญาข้อ 16 การงดหรือลดค่าปรับ หรือการขยายเวลาปฏิบัติงานตามสัญญา' },
    { componentCode: 'HireCustomWork17', label: 'สัญญาข้อ 17 การใช้เรือไทย' },
  ],
  // Template8 — CFormat009
  CFormat009: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 3 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 5 หน้าที่และความรับผิดชอบของผู้รับจ้าง' },
    { componentCode: 'HireBuildingCleaningService6', label: 'สัญญาข้อ 6 การจ้างช่วง' },
    { componentCode: 'HireBuildingCleaningService7', label: 'สัญญาข้อ 7 การควบคุมงานของผู้รับจ้าง' },
    { componentCode: 'HireBuildingCleaningService8', label: 'สัญญาข้อ 8 การตรวจงานจ้าง' },
    { componentCode: 'HireBuildingCleaningService9', label: 'สัญญาข้อ 9 การแก้ไขเปลี่ยนแปลงงาน และต่อสัญญาจ้างในกรณีจำเป็น' },
    { componentCode: 'HireBuildingCleaningService10', label: 'สัญญาข้อ 10 การบอกเลิกสัญญา' },
    { componentCode: 'HireBuildingCleaningService11', label: 'สัญญาข้อ 11 การควบคุมงานโดยผู้ว่าจ้าง' },
    { componentCode: 'HireBuildingCleaningService12', label: 'สัญญาข้อ 12 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'HireBuildingCleaningService13', label: 'สัญญาข้อ 13 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา' },
  ],
  // Template9 — CFormat014
  CFormat014: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 3 ค่าจ้างงานออกแบบและการจ่ายเงิน' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 4 หน้าที่ของผู้ให้บริการงานออกแบบ' },
    { componentCode: 'HireDesignAndSupervision5', label: 'สัญญาข้อ 5 ข้อตกลงว่าจ้างงานควบคุมงานก่อสร้าง' },
    { componentCode: 'HireDesignAndSupervision6', label: 'สัญญาข้อ 6 ค่าจ้างควบคุมงานก่อสร้างและการจ่ายเงิน' },
    { componentCode: 'HireDesignAndSupervision7', label: 'สัญญาข้อ 7 หน้าที่ของผู้ให้บริการงานควบคุมงานก่อสร้าง' },
    { componentCode: 'HireDesignAndSupervision8', label: 'สัญญาข้อ 8 ค่าจ้างงานควบคุมงานกรณีผู้รับจ้างปฏิบัติงานล่วงเลยกำหนดเวลา' },
    { componentCode: 'HireDesignAndSupervision9', label: 'สัญญาข้อ 9 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่ายของงานออกแบบและควบคุมงานก่อสร้าง' },
    { componentCode: 'HireDesignAndSupervision10', label: 'สัญญาข้อ 10 การงดหรือลดค่าปรับ หรือการขยายเวลาการปฏิบัติงานออกแบบและควบคุมงานก่อสร้าง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 11 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'HireDesignAndSupervision12', label: 'สัญญาข้อ 12 การจ้างช่วงงานออกแบบและควบคุมงานก่อสร้าง' },
    { componentCode: 'HireDesignAndSupervision13', label: 'สัญญาข้อ 13 การโอนสิทธิประโยชน์ของผู้ให้บริการงานออกแบบและควบคุมงานก่อสร้าง' },
  ],
  // Template10 — CFormat007
  CFormat007: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'TerminationInfoDuration', label: 'สัญญาข้อ 3 ระยะเวลาให้บริการ' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน' },
    { componentCode: 'WarrantyMA', label: 'สัญญาข้อ 5 การรับประกันผลงาน' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 6 การให้บริการ' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 7 ความรับผิดชอบของผู้รับจ้าง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'ElementCAMHirerMaintenance9', label: 'สัญญาข้อ 9 การจ้างช่วง' },
    { componentCode: 'ElementCAMHirerMaintenance10', label: 'สัญญาข้อ 10 การบอกเลิกสัญญา' },
    { componentCode: 'ElementCAMHirerMaintenance11', label: 'สัญญาข้อ 11 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'ElementCAMHirerMaintenance12', label: 'สัญญาข้อ 12 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา' },
  ],
  // Template11 — CFormat010
  CFormat010: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 2 การจ่ายเงิน' },
    { componentCode: 'ElementCAMHirerSecurityService3', label: 'สัญญาข้อ 3 หน้าที่และความรับผิดของผู้รับจ้าง' },
    { componentCode: 'ElementCAMHirerSecurityService4', label: 'สัญญาข้อ 4 หน้าที่และความรับผิดชอบของผู้ว่าจ้าง' },
    { componentCode: 'ElementCAMHirerSecurityService5', label: 'สัญญาข้อ 5 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'ElementCAMHirerSecurityService6', label: 'สัญญาข้อ 6 การบอกเลิกสัญญา' },
    { componentCode: 'ElementCAMHirerSecurityService7', label: 'สัญญาข้อ 7 การจ้างช่วง' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'ElementCAMHirerSecurityService9', label: 'สัญญาข้อ 9 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
  ],
  // Template12 — CFormat015
  CFormat015: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 3 ค่าจ้างและการจ่ายเงิน' },
    { componentCode: 'AdvancePayment', label: 'สัญญาข้อ 4 เงินค่าจ้างล่วงหน้า' },
    { componentCode: 'ElementCAMHirerSecurityService5', label: 'สัญญาข้อ 5 ความรับผิดชอบของที่ปรึกษา' },
    { componentCode: 'ElementCAMHirerSecurityService6', label: 'สัญญาข้อ 6 การระงับการทำงานชั่วคราวและการบอกเลิกสัญญา' },
    { componentCode: 'ElementCAMHirerSecurityService7', label: 'สัญญาข้อ 7 สิทธิและหน้าที่ของที่ปรึกษา' },
    { componentCode: 'ElementCAMHirerSecurityService8', label: 'สัญญาข้อ 8 ความรับผิดชอบของที่ปรึกษาต่อบุคคลภายนอก' },
    { componentCode: 'ElementCAMHirerSecurityService9', label: 'สัญญาข้อ 9 พันธะหน้าที่ของผู้ว่าจ้าง' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 10 ค่าปรับ' },
    { componentCode: 'ElementCAMHirerSecurityService11', label: 'สัญญาข้อ 11 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'RetentionPayment', label: 'สัญญาข้อ 12 (ก) เงินประกันผลงาน' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 12 (ข) หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'ElementCAMHirerSecurityService13', label: 'สัญญาข้อ 13 การจ้างช่วง' },
    { componentCode: 'ElementCAMHirerSecurityService14', label: 'สัญญาข้อ 14 การโอนสิทธิตามสัญญา' },
    { componentCode: 'ElementCAMHirerSecurityService15', label: 'สัญญาข้อ 15 การงดหรือลดค่าปรับ หรือขยายเวลาปฏิบัติงานตามสัญญา' },
  ],
  // Template13 — CFormat011
  CFormat011: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'CopierLeaseInfo', label: 'สัญญาข้อ 2 ค่าเช่าเครื่องถ่ายเอกสาร' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Delivery', label: 'สัญญาข้อ 4 การส่งมอบ' },
    { componentCode: 'RentCopier5', label: 'สัญญาข้อ 5 การตรวจรับ' },
    { componentCode: 'RentCopier6', label: 'สัญญาข้อ 6 การงดหรือลดค่าปรับ หรือขยายเวลาในการปฏิบัติตามสัญญา' },
    { componentCode: 'Warranty', label: 'สัญญาข้อ 7 การบำรุงรักษาตรวจสภาพและซ่อมแซมเครื่องถ่ายเอกสารที่เช่า' },
    { componentCode: 'RentCopier8', label: 'สัญญาข้อ 8 หน้าที่ของผู้ให้เช่า' },
    { componentCode: 'RentCopier9', label: 'สัญญาข้อ 9 ค่าปรับกรณีความชำรุดบกพร่องของเครื่องถ่ายเอกสาร' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 10 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'RentCopier11', label: 'สัญญาข้อ 11 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 12 ค่าปรับกรณีส่งมอบล่าช้า' },
    { componentCode: 'RentCopier13', label: 'สัญญาข้อ 13 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'RentCopier14', label: 'สัญญาข้อ 14 การโอนสิทธิของผู้ให้เช่า' },
    { componentCode: 'RentCopier15', label: 'สัญญาข้อ 15 การนำเครื่องถ่ายเอกสารที่เช่ากลับคืนเมื่อสัญญาสิ้นสุดลง' },
    { componentCode: 'RentCopier16', label: 'สัญญาข้อ 16 ข้อจำกัดความรับผิดของผู้เช่า' },
  ],
  // Template14 — CFormat006
  CFormat006: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'RentComputer1', label: 'สัญญาข้อ 1 คำนิยาม' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 2 ข้อตกลง' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'ComputerLeaseInfo', label: 'สัญญาข้อ 4 ระยะเวลาการเช่า' },
    { componentCode: 'RentComputer5', label: 'สัญญาข้อ 5 การชำระค่าเช่า' },
    { componentCode: 'RentComputer6', label: 'สัญญาข้อ 6 การรับรองคุณภาพ' },
    { componentCode: 'Delivery', label: 'สัญญาข้อ 7 การส่งมอบและติดตั้ง' },
    { componentCode: 'RentComputer8', label: 'สัญญาข้อ 8 การตรวจรับ' },
    { componentCode: 'RentComputer9', label: 'สัญญาข้อ 9 การบำรุงรักษา' },
    { componentCode: 'RentComputer10', label: 'สัญญาข้อ 10 การซ่อมแซมแก้ไข' },
    { componentCode: 'RentComputer11', label: 'สัญญาข้อ 11 การใช้ประโยชน์' },
    { componentCode: 'RentComputer12', label: 'สัญญาข้อ 12 การจัดอบรม' },
    { componentCode: 'RentComputer13', label: 'สัญญาข้อ 13 คู่มือการใช้คอมพิวเตอร์' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 14 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'RentComputer15', label: 'สัญญาข้อ 15 ข้อตกลงการใช้โปรแกรม' },
    { componentCode: 'RentComputer16', label: 'สัญญาข้อ 16 การรับประกันความเสียหาย' },
    { componentCode: 'RentComputer17', label: 'สัญญาข้อ 17 ความรับผิดต่อความเสียหาย' },
    { componentCode: 'RentComputer18', label: 'สัญญาข้อ 18 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 19 ค่าปรับ' },
    { componentCode: 'RentComputer20', label: 'สัญญาข้อ 20 การนำคอมพิวเตอร์กลับคืนไป' },
    { componentCode: 'RentComputer21', label: 'สัญญาข้อ 21 การโอนกรรมสิทธิ์ให้บุคคลอื่น' },
    { componentCode: 'RentComputer22', label: 'สัญญาข้อ 22 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'RentComputer23', label: 'สัญญาข้อ 23 การงดหรือลดค่าปรับ หรือขยายเวลาในการปฏิบัติตามสัญญา' },
    { componentCode: 'RentComputer24', label: 'สัญญาข้อ 24 การโอนสิทธิและหน้าที่ตามสัญญา' },
  ],
  // Template15 — CFormat008
  CFormat008: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลง' },
    { componentCode: 'CarLeaseInfo', label: 'สัญญาข้อ 2 ค่าเช่ารถยนต์' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Payment', label: 'สัญญาข้อ 4 การจ่ายเงิน' },
    { componentCode: 'Redelivery', label: 'สัญญาข้อ 5 การตรวจรับ' },
    { componentCode: 'RentCar6', label: 'สัญญาข้อ 6 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา' },
    { componentCode: 'RentCar7', label: 'สัญญาข้อ 7 หน้าที่ของผู้ให้เช่า' },
    { componentCode: 'RentCar8', label: 'สัญญาข้อ 8 การบอกเลิกสัญญา' },
    { componentCode: 'Mulct', label: 'สัญญาข้อ 9 ค่าปรับกรณีส่งมอบล่าช้า' },
    { componentCode: 'RentCar10', label: 'สัญญาข้อ 10 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย' },
    { componentCode: 'RentCar11', label: 'สัญญาข้อ 11 การใช้ประโยชน์จากรถยนต์ที่เช่า' },
    { componentCode: 'RentCar12', label: 'สัญญาข้อ 12 การรับมอบรถยนต์ที่เช่ากลับคืน' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 13 หลักประกันการปฏิบัติตามสัญญา' },
    { componentCode: 'RentCar14', label: 'สัญญาข้อ 14 ข้อจำกัดความรับผิดของผู้เช่า' },
  ],
  // TemplateRental — CMRentalTpl001-004
  CMRentalTpl001: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลงซื้อขาย' },
    { componentCode: 'Rental2', label: 'สัญญาข้อ 2 คำรับรองเกี่ยวกับกรรมสิทธิ์ในสถานที่เช่า' },
    { componentCode: 'Rental3', label: 'สัญญาข้อ 3 การส่งมอบ' },
    { componentCode: 'Period', label: 'สัญญาข้อ 4 ระยะเวลาเช่า' },
    { componentCode: 'RentalFee', label: 'สัญญาข้อ 5 ค่าเช่า' },
    { componentCode: 'Rental6', label: 'สัญญาข้อ 6 คำมั่นจะให้เช่าต่อไปอีกเมื่อครบกำหนดระยะเวลาเช่า' },
    { componentCode: 'Rental7', label: 'สัญญาข้อ 7 สิทธิและหน้าที่ของผู้ให้เช่า' },
    { componentCode: 'Rental8', label: 'สัญญาข้อ 8 สิทธิและหน้าที่ของผู้เช่า' },
    { componentCode: 'Rental9', label: 'สัญญาข้อ 9 การบังคับค่าเสียหายและค่าใช้จ่าย' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Rental11', label: 'สัญญาข้อ 11 การพักการเช่า' },
    { componentCode: 'Rental12', label: 'สัญญาข้อ 12 การสิ้นสุดของสัญญาเช่า' },
    { componentCode: 'Rental13', label: 'สัญญาข้อ 13 การบอกกล่าว' },
    { componentCode: 'Rental14', label: 'สัญญาข้อ 14 การประกันภัย' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 15 เงินประกันการเช่า' },
  ],
  CMRentalTpl002: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลงซื้อขาย' },
    { componentCode: 'Rental2', label: 'สัญญาข้อ 2 คำรับรองเกี่ยวกับกรรมสิทธิ์ในสถานที่เช่า' },
    { componentCode: 'Rental3', label: 'สัญญาข้อ 3 การส่งมอบ' },
    { componentCode: 'Period', label: 'สัญญาข้อ 4 ระยะเวลาเช่า' },
    { componentCode: 'RentalFee', label: 'สัญญาข้อ 5 ค่าเช่า' },
    { componentCode: 'Rental6', label: 'สัญญาข้อ 6 คำมั่นจะให้เช่าต่อไปอีกเมื่อครบกำหนดระยะเวลาเช่า' },
    { componentCode: 'Rental7', label: 'สัญญาข้อ 7 สิทธิและหน้าที่ของผู้ให้เช่า' },
    { componentCode: 'Rental8', label: 'สัญญาข้อ 8 สิทธิและหน้าที่ของผู้เช่า' },
    { componentCode: 'Rental9', label: 'สัญญาข้อ 9 การบังคับค่าเสียหายและค่าใช้จ่าย' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Rental11', label: 'สัญญาข้อ 11 การพักการเช่า' },
    { componentCode: 'Rental12', label: 'สัญญาข้อ 12 การสิ้นสุดของสัญญาเช่า' },
    { componentCode: 'Rental13', label: 'สัญญาข้อ 13 การบอกกล่าว' },
    { componentCode: 'Rental14', label: 'สัญญาข้อ 14 การประกันภัย' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 15 เงินประกันการเช่า' },
  ],
  CMRentalTpl003: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลงซื้อขาย' },
    { componentCode: 'Rental2', label: 'สัญญาข้อ 2 คำรับรองเกี่ยวกับกรรมสิทธิ์ในสถานที่เช่า' },
    { componentCode: 'Rental3', label: 'สัญญาข้อ 3 การส่งมอบ' },
    { componentCode: 'Period', label: 'สัญญาข้อ 4 ระยะเวลาเช่า' },
    { componentCode: 'RentalFee', label: 'สัญญาข้อ 5 ค่าเช่า' },
    { componentCode: 'Rental6', label: 'สัญญาข้อ 6 คำมั่นจะให้เช่าต่อไปอีกเมื่อครบกำหนดระยะเวลาเช่า' },
    { componentCode: 'Rental7', label: 'สัญญาข้อ 7 สิทธิและหน้าที่ของผู้ให้เช่า' },
    { componentCode: 'Rental8', label: 'สัญญาข้อ 8 สิทธิและหน้าที่ของผู้เช่า' },
    { componentCode: 'Rental9', label: 'สัญญาข้อ 9 การบังคับค่าเสียหายและค่าใช้จ่าย' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Rental11', label: 'สัญญาข้อ 11 การพักการเช่า' },
    { componentCode: 'Rental12', label: 'สัญญาข้อ 12 การสิ้นสุดของสัญญาเช่า' },
    { componentCode: 'Rental13', label: 'สัญญาข้อ 13 การบอกกล่าว' },
    { componentCode: 'Rental14', label: 'สัญญาข้อ 14 การประกันภัย' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 15 เงินประกันการเช่า' },
  ],
  CMRentalTpl004: [
    { componentCode: 'EditPo', label: 'เพิ่ม PO ใหม่ และวงเงินตามสัญญา' },
    { componentCode: 'SalesAgreement', label: 'สัญญาข้อ 1 ข้อตกลงซื้อขาย' },
    { componentCode: 'Rental2', label: 'สัญญาข้อ 2 คำรับรองเกี่ยวกับกรรมสิทธิ์ในสถานที่เช่า' },
    { componentCode: 'Rental3', label: 'สัญญาข้อ 3 การส่งมอบ' },
    { componentCode: 'Period', label: 'สัญญาข้อ 4 ระยะเวลาเช่า' },
    { componentCode: 'RentalFee', label: 'สัญญาข้อ 5 ค่าเช่า' },
    { componentCode: 'Rental6', label: 'สัญญาข้อ 6 คำมั่นจะให้เช่าต่อไปอีกเมื่อครบกำหนดระยะเวลาเช่า' },
    { componentCode: 'Rental7', label: 'สัญญาข้อ 7 สิทธิและหน้าที่ของผู้ให้เช่า' },
    { componentCode: 'Rental8', label: 'สัญญาข้อ 8 สิทธิและหน้าที่ของผู้เช่า' },
    { componentCode: 'Rental9', label: 'สัญญาข้อ 9 การบังคับค่าเสียหายและค่าใช้จ่าย' },
    { componentCode: 'PartOfContract', label: 'สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา' },
    { componentCode: 'Rental11', label: 'สัญญาข้อ 11 การพักการเช่า' },
    { componentCode: 'Rental12', label: 'สัญญาข้อ 12 การสิ้นสุดของสัญญาเช่า' },
    { componentCode: 'Rental13', label: 'สัญญาข้อ 13 การบอกกล่าว' },
    { componentCode: 'Rental14', label: 'สัญญาข้อ 14 การประกันภัย' },
    { componentCode: 'ContractPerformance', label: 'สัญญาข้อ 15 เงินประกันการเช่า' },
  ],
};

const SECTION_COMPONENT_MAP: Record<string, () => Promise<Component>> = {
  EditPo: () => import('@/views/PP/components/PP010/components/Sub/EditPo.vue'),
  SalesAgreement: () => import('@/views/PP/components/PP010/components/Sub/SalesAgreement.vue'),
  PartOfContract: () => import('@/views/PP/components/PP010/components/Sub/PartOfContract.vue'),
  Delivery: () => import('@/views/PP/components/PP010/components/Sub/Delivery.vue'),
  Payment: () => import('@/views/PP/components/PP010/components/Sub/Payment.vue'),
  Warranty: () => import('@/views/PP/components/PP010/components/Sub/Warranty.vue'),
  ContractPerformance: () => import('@/views/PP/components/PP010/components/Sub/ContractPerformance.vue'),
  Mulct: () => import('@/views/PP/components/PP010/components/Sub/Mulct.vue'),
  AdvancePayment: () => import('@/views/PP/components/PP010/components/Sub/AdvancePayment.vue'),
  RetentionPayment: () => import('@/views/PP/components/PP010/components/Sub/RetentionPayment.vue'),
  TerminationInfoDuration: () => import('@/views/PP/components/PP010/components/Sub/TerminationInfoDuration.vue'),
  TerminationInfoDate: () => import('@/views/PP/components/PP010/components/Sub/TerminationInfoDate.vue'),
  Redelivery: () => import('@/views/PP/components/PP010/components/Sub/Redelivery.vue'),
  DefectWarranty: () => import('@/views/PP/components/PP010/components/Sub/DefectWarranty.vue'),
  WarrantyMA: () => import('@/views/PP/components/PP010/components/Sub/WarrantyMA.vue'),
  ComputerLeaseInfo: () => import('@/views/PP/components/PP010/components/Sub/ComputerLeaseInfo.vue'),
  CopierLeaseInfo: () => import('@/views/PP/components/PP010/components/Sub/CopierLeaseInfo.vue'),
  CarLeaseInfo: () => import('@/views/PP/components/PP010/components/Sub/CarLeaseInfo.vue'),
  Period: () => import('@/views/PP/components/PP010/components/Sub/Period.vue'),
  RentalFee: () => import('@/views/PP/components/PP010/components/Sub/RentalFee.vue'),
};

const Cm007Constants = {
  Cm007StatusName,
  Cm007BadgeStatus,
  Cm007AccordionTabName,
  TEMPLATE_SECTION_MAP,
  SECTION_COMPONENT_MAP,
};

export default Cm007Constants;
