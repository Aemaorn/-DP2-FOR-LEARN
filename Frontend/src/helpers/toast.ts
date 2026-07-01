import type { IToastOptions } from '@/models/shared/toast';

enum ToastType {
  SUCCESS = 'success',
  ERROR = 'error',
  WARNING = 'warning',
}

const dispatchToast = (
  type: ToastType,
  title: string,
  message: string,
  toastOption: IToastOptions
): void => {
  document.dispatchEvent(
    new CustomEvent('toast', {
      detail: {
        toastType: type,
        title,
        message,
        toastOption,
      },
    })
  );
};

const dispatchToastWithIcon = (
  icon: string,
  title: string,
  message: string,
  toastOption: IToastOptions
): void => {
  document.dispatchEvent(
    new CustomEvent('toast', {
      detail: {
        toastType: ToastType.SUCCESS,
        title,
        message,
        toastOption: {
          icon: icon,
          iconColor: toastOption.iconColor,
          progressbarColor: toastOption.progressbarColor,
          bgColor: toastOption.bgColor,
        } as IToastOptions,
      },
    })
  );
};

const ToastIconByType = (type: ToastType): IToastOptions => {
  switch (type) {
    case ToastType.SUCCESS:
      return {
        icon: 'check',
        iconColor: 'bg-green-600!',
        progressbarColor: 'bg-green-600!',
        bgColor: 'bg-green-50!',
      };
    case ToastType.ERROR:
      return {
        icon: 'close',
        iconColor: 'bg-red-600!',
        progressbarColor: 'bg-red-600!',
        bgColor: 'bg-red-50!',
      };
    case ToastType.WARNING:
      return {
        icon: 'priority_high',
        iconColor: 'bg-yellow-600!',
        progressbarColor: 'bg-yellow-600!',
        bgColor: 'bg-yellow-50!',
      };
  }
};

/**
 * **(Success Toast)**
 * @param title - หัวข้อ
 * @param message - ข้อความ
 */
const success = (title: string, message: string): void =>
  dispatchToast(ToastType.SUCCESS, title, message, ToastIconByType(ToastType.SUCCESS));

/**
 * **(Error Toast)**
 * @param title - หัวข้อ
 * @param message - ข้อความ
 */
const error = (title: string, message: string): void =>
  dispatchToast(ToastType.ERROR, title, message, ToastIconByType(ToastType.ERROR));

/**
 * **(ErrorDescription Toast)**
 * @param message - ข้อความ
 */
const errorDescription = (message: string): void =>
  dispatchToast(ToastType.ERROR, 'ข้อมูลไม่ถูกต้อง', message, ToastIconByType(ToastType.ERROR));

/**
 * **(Warning Toast)**
 * @param title - หัวข้อ
 * @param message - ข้อความ
 */
const warning = (title: string, message: string): void =>
  dispatchToast(ToastType.WARNING, title, message, ToastIconByType(ToastType.WARNING));

/**
 * .* @param icon - ใช้ Icon name ของ Material Design เท่านั้น https://fonts.google.com/icons
 */
const successIcon = (icon: string, title: string, message: string): void =>
  dispatchToastWithIcon(icon, title, message, ToastIconByType(ToastType.SUCCESS));

/**
 * .* @param icon - ใช้ Icon name ของ Material Design เท่านั้น https://fonts.google.com/icons
 */
const errorIcon = (icon: string, title: string, message: string): void =>
  dispatchToastWithIcon(icon, title, message, ToastIconByType(ToastType.ERROR));

/**
 * .* @param icon - ใช้ Icon name ของ Material Design เท่านั้น https://fonts.google.com/icons
 */
const warningIcon = (icon: string, title: string, message: string): void =>
  dispatchToastWithIcon(icon, title, message, ToastIconByType(ToastType.WARNING));

/**
 * **(Toast กลาง)** - สร้างข้อมูล -> บันทึกการสร้างข้อมูลสำเร็จ
 */
const createdMessageToast = (): void => success('สร้างข้อมูล', 'บันทึกการสร้างข้อมูลสำเร็จ');

/**
 * **(Toast กลาง)** - แก้ไขข้อมูล -> บันทึกการอัปเดตแก้ไขข้อมูลสำเร็จ
 */
const updatedMessageToast = (): void => successIcon('edit_square', 'แก้ไขข้อมูล', 'บันทึกการอัปเดตแก้ไขข้อมูลสำเร็จ');

/**
 * **(Toast กลาง)** - ลบข้อมูล -> ลบข้อมูลสำเร็จ
 */
const deletedMessageToast = (): void => successIcon('delete', 'ลบข้อมูล', 'ลบข้อมูลสำเร็จ');

/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> กรุณาระบุข้อมูลให้ครบถ้วน
 * และ activate tab ที่มี error + scroll ไปยัง field แรกที่ติด validate
 */
const invalidMessageToast = (): void => {
  error('ข้อมูลไม่ถูกต้อง', 'กรุณาระบุข้อมูลให้ครบถ้วน');

  setTimeout(() => {
    const firstError = document.querySelector<HTMLElement>('[data-p-invalid], .p-invalid, .error-border')
      ?? Array.from(document.querySelectorAll<HTMLElement>('small')).find(el =>
        el.textContent?.trim() && el.className.includes('text-red')
      ) ?? null;

    if (!firstError) return;

    const tabActivated = activateParentTab(firstError);

    if (tabActivated) {
      setTimeout(() => scrollAndFocus(firstError), 150);
    } else {
      scrollAndFocus(firstError);
    }
  }, 100);
};

const scrollAndFocus = (el: HTMLElement): void => {
  el.scrollIntoView({ behavior: 'smooth', block: 'center' });

  const focusable = el.matches('input, select, textarea')
    ? el
    : el.querySelector<HTMLElement>('input, select, textarea');

  if (focusable) {
    focusable.focus({ preventScroll: true });
  }
};

const activateParentTab = (el: HTMLElement): boolean => {
  const tabPanel = el.closest<HTMLElement>('[data-pc-name="tabpanel"]');
  if (!tabPanel || tabPanel.getAttribute('data-p-active') !== 'false') return false;

  // Try aria-controls approach
  const panelId = tabPanel.getAttribute('id');
  if (panelId) {
    const tabButton = document.querySelector<HTMLElement>(`[aria-controls="${panelId}"]`);
    if (tabButton) {
      tabButton.click();
      return true;
    }
  }

  // Fallback: match by index within the tabs container
  const tabsContainer = tabPanel.closest<HTMLElement>('[data-pc-name="tabs"]');
  if (!tabsContainer) return false;

  const panelsContainer = tabPanel.parentElement;
  if (!panelsContainer) return false;

  const allPanels = panelsContainer.querySelectorAll(':scope > [data-pc-name="tabpanel"]');
  const panelIndex = Array.from(allPanels).indexOf(tabPanel);
  if (panelIndex === -1) return false;

  const tabList = tabsContainer.querySelector('[data-pc-name="tablist"]');
  if (!tabList) return false;

  const allTabs = tabList.querySelectorAll<HTMLElement>('[data-pc-name="tab"]');
  const targetTab = allTabs[panelIndex];
  if (targetTab) {
    targetTab.click();
    return true;
  }

  return false;
};

/**
 * **(Toast กลาง)** - เห็นชอบ/อนุมัติ -> ยืนยันการเห็นชอบ/อนุมัติสำเร็จ
 */
const approvedMessageToast = (): void => success('เห็นชอบ/อนุมัติ', 'ยืนยันการเห็นชอบ/อนุมัติสำเร็จ');

/**
 * **(Toast กลาง)** - ส่งกลับแก้ไข -> ส่งกลับแก้ไขสำเร็จ
 */
const sendEditMessageToast = (): void => success('ส่งกลับแก้ไข', 'ส่งกลับแก้ไขสำเร็จ');

/**
 * **(Toast กลาง)** - ไม่เห็นชอบ -> ไม่เห็นชอบสำเร็จ
 */
const notAgreeMessageToast = (): void => success('ไม่เห็นชอบ', 'ไม่เห็นชอบสำเร็จ');

/**
 * **(Toast กลาง)** - เรียกคืนแก้ไข -> เรียกคืนแก้ไขสำเร็จ
 */
const recallEditMessageToast = (): void => success('เรียกคืนแก้ไข', 'เรียกคืนแก้ไขสำเร็จ');

/**
 * **(Toast กลาง)** - ส่งเห็นชอบ -> ส่งเห็นชอบสำเร็จ
 */
const sendApproveMessageToast = (): void => success('ส่งเห็นชอบ', 'ส่งเห็นชอบสำเร็จ');

/**
 * **(Toast กลาง)** - ส่งอนุมัติ -> ส่งอนุมัติสำเร็จ
 */
const sendConfirmMessageToast = (): void => success('ส่งอนุมัติ', 'ส่งอนุมัติสำเร็จ');

/**
 * **(Toast กลาง)** - ส่งเห็นชอบ/อนุมัติ -> ส่งเห็นชอบ/อนุมัติสำเร็จ
 */
const sendApproveConfirmMessageToast = (): void => success('ส่งเห็นชอบ/อนุมัติ', 'ส่งเห็นชอบ/อนุมัติสำเร็จ');

/**
 * **(Toast กลาง)** - มอบหมาย -> มอบหมายสำเร็จ
 */
const assignedMessageToast = (): void => success('มอบหมาย', 'มอบหมายสำเร็จ');

/**
 * **(Toast กลาง)** - ความเห็นเจ้าหน้าที่พัสดุ -> ส่งความเห็นเจ้าหน้าที่พัสดุสำเร็จ
 */
const remarkOfficerMessageToast = (): void => success('ความเห็นเจ้าหน้าที่พัสดุ', 'ส่งความเห็นเจ้าหน้าที่พัสดุสำเร็จ');

/**
 * **(Toast กลาง)** - เกิดข้อผิดพลาด -> ไม่พบข้อมูล, กรุณาลองใหม่อีกครั้ง
 */
const notFoundMessageToast = (): void => error('เกิดข้อผิดพลาด', 'ไม่พบข้อมูล, กรุณาลองใหม่อีกครั้ง');

/**
 * **(Toast กลาง)** - ขอเปลี่ยนแปลง -> ขออเปลี่ยนแปลงสำเร็จ
 */
const changedMessageToast = (): void => success('ขอเปลี่ยนแปลง', 'ขออเปลี่ยนแปลงสำเร็จ');

/**
 * **(Toast กลาง)** - ขอยกเลิก -> ขอยกเลิกสำเร็จ
 */
const canceledMessageToast = (): void => success('ขอยกเลิก', 'ขอยกเลิกสำเร็จ');

/**
 * **(Toast กลาง)** - ยืนยัน -> ยืนยันสำเร็จ
 */
const confirmMessageToast = (): void => success('ยืนยัน', 'ยืนยันสำเร็จ');

/**
 * **(Toast กลาง)** - เผยแพร่แผน → เผยแพร่แผนสำเร็จ
 */
const annoucementPlanMessageToast = (): void => success('เผยแพร่แผน', 'เผยแพร่แผนสำเร็จ');

/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีผู้รับผิดชอบอย่างน้อย 1 คน
 */
const assignAtLeastMessageToast = (): void => errorDescription('จะต้องมีผู้รับผิดชอบอย่างน้อย 1 คน');

/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติ 1 คน
 */
const approvalAtLeastMessageToast = (): void => errorDescription('จะต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติ อย่างน้อย 1 คน');

/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีเจ้าหน้าที่พัสดุให้ความเห็นต้องให้ความเห็นอย่างน้อย 1 คน
 */
const assignneeCommentAtLeastMessageToast = (): void => errorDescription('จะต้องมีเจ้าหน้าที่พัสดุให้ความเห็นต้องให้ความเห็นอย่างน้อย 1 คน');

/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีสายงานเห็นชอบ/อนุมัติอย่างน้อย 1 คน
 */
const departmentAtLeastMessageToast = (): void => errorDescription('จะต้องมีสายงานเห็นชอบ/อนุมัติอย่างน้อย 1 คน');

/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีส่วนงานอย่างน้อย 1 คน
 */
const segmentAtLeastMessageToast = (): void => errorDescription('จะต้องมีส่วนงานอย่างน้อย 1 คน');
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีผู้ชนะอย่างน้อย 1 รายการ
 */
const winnerAtLeastMessageToast = (): void => errorDescription('จะต้องมีผู้ชนะอย่างน้อย 1 รายการ');
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้างอย่างน้อย 1 คน
 */
const committeeAtLeastMessageToast = (): void => errorDescription('จะต้องมีผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้างอย่างน้อย 1 คน');
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีสายงานอย่างน้อย 1 คน
 */
const lineOfWorkAtLeastMessageToast = (): void => errorDescription('จะต้องมีสายงานอย่างน้อย 1 คน');
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> ต้องมีตำแหน่งประธานกรรมการในตำแหน่งในคณะกรรมการ
 */
const mustHaveLeaderBoardToast = (): void => errorDescription('ต้องมีตำแหน่งประธานกรรมการในตำแหน่งในคณะกรรมการ');
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> กรุณาเพิ่มเอกสารแนบอย่างน้อย 1 ไฟล์ หรือ ลบประเภทเอกสาร
 */
const fileAtLeastOrRemove = (): void => errorDescription('กรุณาเพิ่มเอกสารแนบอย่างน้อย 1 ไฟล์ หรือ ลบประเภทเอกสาร');
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> กรุณาเพิ่มฝ่ายเห็นชอบอย่างน้อย 1 คน
 */
const factionAtLeastMessageToast = (): void => errorDescription("กรุณาเพิ่มฝ่ายเห็นชอบอย่างน้อย 1 คน");
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จะต้องมีรายการจัดซื้อจัดจ้างอย่างน้อย 1 รายการ
 */
const planSelectedAtLeastMessageToast = (): void => errorDescription('จะต้องมีรายการจัดซื้อจัดจ้างอย่างน้อย 1 รายการ');
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> ยอดเงินเกิน 500,000 บาท ตามมาตรา 79 วรรคสอง
 */
const overBudget79W2MessageToast = (): void => errorDescription("ยอดเงินเกิน 500,000 บาท");
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> ยอดเงินเกิน 10,000 บาท ตามมาตรา ว119
 */
const overBudgetW119MessageToast = (): void => errorDescription("ยอดเงินเกิน 10,000 บาท ตามมาตรา ว119");
/**
 * **(Toast กลาง)** - ข้อมูลไม่ถูกต้อง -> จำนวนเงินรวมรหัสบัญชีต้องเท่ากับจำนวนเงินรวมรายการพัสดุ
 */
const glAccountExceedsTotalPriceMessageToast = (): void => errorDescription("จำนวนเงินรวมรหัสบัญชีต้องเท่ากับจำนวนเงินรวมรายการพัสดุ");

/**
 * 🔔 **Toast Helper**
 *
 * ฟังก์ชัน Toast ที่ใช้แสดงข้อความสถานะการดำเนินการต่าง ๆ
 *
 * ─────────────────
 * ✅ **Shared Toasts**
 * ──────────────────
 *
 * - **createdMessageToast**
 *   ➤ สร้างข้อมูล → บันทึกการสร้างข้อมูลสำเร็จ
 *
 * - **updatedMessageToast**
 *   ➤ แก้ไขข้อมูล → บันทึกการอัปเดตแก้ไขข้อมูลสำเร็จ
 *
 * - **deletedMessageToast**
 *   ➤ ลบข้อมูล → ลบข้อมูลสำเร็จ
 *
 * - **approvedMessageToast**
 *   ➤ เห็นชอบ/อนุมัติ → ยืนยันการเห็นชอบ/อนุมัติสำเร็จ
 *
 * - **notAgreeMessageToast**
 *   ➤ ไม่เห็นชอบ → ไม่เห็นชอบสำเร็จ
 *
 * - **sendEditMessageToast**
 *   ➤ ส่งกลับแก้ไข → ส่งกลับแก้ไขสำเร็จ
 *
 * - **recallEditMessageToast**
 *   ➤ เรียกคืนแก้ไข -> เรียกคืนแก้ไขสำเร็จ
 *
 * - **sendConfirmMessageToast**
 *   ➤ ส่งอนุมัติ -> ส่งอนุมัติสำเร็จ
 *
 * - **sendApproveMessageToast**
 *   ➤ ส่งเห็นชอบ → ส่งเห็นชอบสำเร็จ
 *
 * - **assignedMessageToast**
 *   ➤ มอบหมาย → มอบหมายสำเร็จ
 *
 * - **remarkOfficerMessageToast**
 *   ➤ ความเห็นเจ้าหน้าที่พัสดุ → ส่งความเห็นเจ้าหน้าที่พัสดุสำเร็จ
 *
 * - **invalidMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง → กรุณาระบุข้อมูลให้ครบถ้วน
 *
 * - **notFoundMessageToast**
 *   ➤ เกิดข้อผิดพลาด → ไม่พบข้อมูล, กรุณาลองใหม่อีกครั้ง
 *
 * - **changedMessageToast**
 *   ➤ ขอเปลี่ยนแปลง -> ขออเปลี่ยนแปลงสำเร็จ
 *
 * - **canceledMessageToast**
 *   ➤ ขอยกเลิก → ขอยกเลิกสำเร็จ
 *
 * - **confirmMessageToast**
 *   ➤ ยืนยัน → ยืนยันสำเร็จ
 *
 * - **annoucementPlanMessageToast**
 *   ➤ เผยแพร่แผน → เผยแพร่แผนสำเร็จ
 *
 * - **assignAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> จะต้องมีผู้รับผิดชอบอย่างน้อย 1 คน
 *
 * - **approvalAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> จะต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติ 1 คน
 *
 * - **assignneeCommentAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> จะต้องมีเจ้าหน้าที่พัสดุให้ความเห็นต้องให้ความเห็นอย่างน้อย 1 คน
 *
 * - **departmentAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> จะต้องมีสายงานเห็นชอบ/อนุมัติอย่างน้อย 1 คน
 *
 * - **segmentAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> จะต้องมีส่วนงานอย่างน้อย 1 คน
 *
 * - **winnerAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> กรุณาเลือกผู้ชนะอย่างน้อย 1 รายการ
 *
 * - **committeeAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> จะต้องมีผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้างอย่างน้อย 1 คน
 *
 * - **lineOfWorkAtLeastMessageToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> จะต้องมีสายงานอย่างน้อย 1 คน
 *
 * - **mustHaveLeaderBoardToast**
 *   ➤ ข้อมูลไม่ถูกต้อง -> ต้องมีตำแหน่งประธานกรรมการในตำแหน่งในคณะกรรมการ
 *
 * - **fileAtLeastOrRemove**
 *   ➤ ข้อมูลไม่ถูกต้อง -> กรุณาเพิ่มเอกสารแนบอย่างน้อย 1 ไฟล์ หรือ ลบประเภทเอกสาร
 *
 * ─────────────────
 * 🛠 **Custom Message Toast**
 * ─────────────────
 *
 * - ใช้กรณีที่ต้องการกำหนดหัวข้อ (`title`) และข้อความ (`message`) เอง
 * - รองรับประเภท:
 *   • **success**
 *   • **error**
 *   • **warning**
 */

const ToastHelper = {
  success,
  error,
  errorDescription,
  warning,
  successIcon,
  errorIcon,
  warningIcon,
  /**
    * - **createdMessageToast**
    *   ➤ สร้างข้อมูล → บันทึกการสร้างข้อมูลสำเร็จ
  */
  createdMessageToast,
  /**
  * - **updatedMessageToast**
  *   ➤ แก้ไขข้อมูล → บันทึกการอัปเดตแก้ไขข้อมูลสำเร็จ
*/
  updatedMessageToast,
  /**
  * - **deletedMessageToast**
  *   ➤ ลบข้อมูล → ลบข้อมูลสำเร็จ
*/
  deletedMessageToast,
  /**
  * - **invalidMessageToast**
  *   ➤ ข้อมูลไม่ถูกต้อง → กรุณาระบุข้อมูลให้ครบถ้วน
*/
  invalidMessageToast,
  /**
  * - **approvedMessageToast**
  *   ➤ เห็นชอบ/อนุมัติ → ยืนยันการเห็นชอบ/อนุมัติสำเร็จ
*/
  approvedMessageToast,
  /**
    * - **sendEditMessageToast**
    *  ➤ ส่งกลับแก้ไข → ส่งกลับแก้ไขสำเร็จ
  */
  sendEditMessageToast,
  /**
    * - **sendEditMessageToast**
    *  ➤ เรียกคืนแก้ไข → เรียกคืนแก้ไขสำเร็จ
  */
  recallEditMessageToast,
  /**
    * - **sendApproveMessageToast**
    *   ➤ ส่งเห็นชอบ → ส่งเห็นชอบสำเร็จ
  */
  sendApproveMessageToast,
  /**
  * - **sendConfirmMessageToast**
  *   ➤ ส่งอนุมัติ -> ส่งอนุมัติสำเร็จ
*/
  sendConfirmMessageToast,
  /**
    * - **sendApproveConfirmMessageToast**
    *   ➤ ส่งเห็นชอบ/อนุมัติ -> ส่งเห็นชอบ/อนุมัติสำเร็จ
  */
  sendApproveConfirmMessageToast,
  /**
 * - **assignedMessageToast**
 *   ➤ มอบหมาย → มอบหมายสำเร็จ
*/
  assignedMessageToast,
  /**
  * - **remarkOfficerMessageToast**
  *   ➤ ความเห็นเจ้าหน้าที่พัสดุ → ส่งความเห็นเจ้าหน้าที่พัสดุสำเร็จ
*/
  remarkOfficerMessageToast,
  /**
  * - **notFoundMessageToast**
  *   ➤ เกิดข้อผิดพลาด → ไม่พบข้อมูล, กรุณาลองใหม่อีกครั้ง
*/
  notFoundMessageToast,
  /**
    * - **changedMessageToast**
    *   ➤ ขอแก้ไข → ขอแก้ไขสำเร็จ
  */
  changedMessageToast,
  /**
    * - **canceledMessageToast**
    *   ➤ ขอยกเลิก → ขอยกเลิกสำเร็จ
  */
  canceledMessageToast,
  /**
    * - **confirmMessageToast**
    *   ➤ ยืนยัน → ยืนยันสำเร็จ
  */
  confirmMessageToast,
  /**
    * - **annoucementPlanMessageToast**
    *   ➤ เผยแพร่แผน → เผยแพร่แผนสำเร็จ
  */
  annoucementPlanMessageToast,
  /**
  * - **assignAtLeastMessageToast**
  *   ➤ ข้อมูลไม่ถูกต้อง → กรุณามอบหมายผู้รับผิดชอบอย่างน้อย 1 คน
  */
  assignAtLeastMessageToast,
  /**
  * - **approvalAtLeastMessageToast**
  *   ➤ ข้อมูลไม่ถูกต้อง → จะต้องมีผู้มีอำนาจเห็นชอบ/อนุมัติ อย่างน้อย 1 คน
 */
  approvalAtLeastMessageToast,
  /**
  * - **assignneeCommentAtLeastMessageToast**
  *   ➤ ข้อมูลไม่ถูกต้อง → เจ้าหน้าที่พัสดุให้ความเห็นต้องให้ตวามเห็นอย่างน้อย 1 คน
  */
  assignneeCommentAtLeastMessageToast,
  /**
  * - **departmentAtLeastMessageToast**
  *   ➤ ข้อมูลไม่ถูกต้อง → กรุณามอบหมายสายงานเห็นชอบ/อนุมัติอย่างน้อย 1 คน
  */
  departmentAtLeastMessageToast,
  /**
  * - **segmentAtLeastMessageToast**
  *   ➤ ข้อมูลไม่ถูกต้อง → กรุณามอบหมายส่วนงานอย่างน้อย 1 คน
  */
  segmentAtLeastMessageToast,
  /**
    * - **winnerAtLeastMessageToast**
    *   ➤ ข้อมูลไม่ถูกต้อง → จะต้องมีผู้ชนะอย่างน้อย 1 รายการ
    */
  winnerAtLeastMessageToast,
  /**
    * - **winnerAtLeastMessageToast**
    *   ➤ ข้อมูลไม่ถูกต้อง → จะต้องมีผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้างอย่างน้อย 1 คน
    */
  committeeAtLeastMessageToast,
  /**
    * - **winnerAtLeastMessageToast**
    *   ➤ ข้อมูลไม่ถูกต้อง → จะต้องมีสายงานอย่างน้อย 1 คน
    */
  lineOfWorkAtLeastMessageToast,
  /**
    * - **mustHaveLeaderBoardToast**
    *   ➤ ข้อมูลไม่ถูกต้อง → ต้องมีตำแหน่งประธานกรรมการในตำแหน่งในคณะกรรมการ
    */
  mustHaveLeaderBoardToast,
  /**
    * - **notAgreeMessageToast**
    *   ➤ ไม่เห็นชอบ → ไม่เห็นชอบสำเร็จ
    */
  notAgreeMessageToast,
  /**
    * - **fileAtLeastOrRemove**
    *   ➤ ข้อมูลไม่ถูกต้อง → กรุณาเพิ่มเอกสารแนบอย่างน้อย 1 ไฟล์ หรือ ลบประเภทเอกสาร
    */
  fileAtLeastOrRemove,
  /**
    * - **fileAtLeastOrRemove**
    *   ➤ ข้อมูลไม่ถูกต้อง → กรุณาเพิ่มฝ่ายเห็นชอบอย่างน้อย 1 คน
    */
  factionAtLeastMessageToast,
  /**
  * - **fileAtLeastOrRemove**
  *   ➤ ข้อมูลไม่ถูกต้อง → ยอดเงินเกิน 500,000 บาท ตามมาตรา 79 วรรคสอง
  */
  overBudget79W2MessageToast,
  /**
* - **fileAtLeastOrRemove**
*   ➤ ข้อมูลไม่ถูกต้อง → ยอดเงินเกิน 10,000 บาท ตามมาตรา ว119
*/
  overBudgetW119MessageToast,
  /**
  * - **glAccountExceedsTotalPriceMessageToast**
  *   ➤ ข้อมูลไม่ถูกต้อง → จำนวนเงินรวมรหัสบัญชีต้องเท่ากับจำนวนเงินรวมรายการพัสดุ
  */
  glAccountExceedsTotalPriceMessageToast,
  /**
   * - **planSelectedAtLeastMessageToast**
   *   ➤ ข้อมูลไม่ถูกต้อง → จะต้องมีรายการจัดซื้อจัดจ้างอย่างน้อย 1 รายการ
   */
  planSelectedAtLeastMessageToast
};

export default ToastHelper;