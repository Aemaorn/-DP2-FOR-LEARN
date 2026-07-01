import { Pcm005ApproveAccordionType } from "@/enums/PCM005/approve";

/**
* **ฟังชั่นที่ใช้ในการแสดงผลชื่อส่วนอนุมัติเห็นชอบ**
* @param status Enum Pcm005ApproveAccordionType
*/
const AccordionName = (value: Pcm005ApproveAccordionType) => {
  switch (value) {
    case Pcm005ApproveAccordionType.Acceptor:
      return 'ผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case Pcm005ApproveAccordionType.Assignee:
      return 'มอบหมายผู้รับผิดชอบสัญญา';
  }
}

const PcmApproveHelper = {
  AccordionName,
};

export default PcmApproveHelper;