import { formatCurrency } from '@/helpers/currency';
import { defineRule } from 'vee-validate';

defineRule('required', (value: string | number | boolean | Record<string, any> | any[] | null | undefined | Date) => {
  if (value == null) {
    return 'กรุณาระบุข้อมูล';
  }

  if (typeof value === 'string' && value.trim().length === 0) {
    return 'กรุณาระบุข้อมูล';
  }

  if (Array.isArray(value) && value.length === 0) {
    return 'ต้องมีอย่างน้อย 1 รายการ';
  }

  if (typeof value === 'object' && !Array.isArray(value) && !(value instanceof Date)) {
    if (Object.keys(value).length === 0) {
      return 'กรุณาระบุข้อมูล';
    }
  }

  return true;
});


defineRule('max_value', (value: string, [target]: string) => {
  if (!value) {
    return true;
  }

  const numVal = Number(value);
  const validateNumVal = Number(target);

  if (numVal > validateNumVal) {
    return `ระบุค่าได้ไม่เกิน ${formatCurrency(validateNumVal)}`;
  }

  return true;
});

defineRule('min_value', (value: string, [target]: string) => {
  const numVal = Number(value);

  if (!value && typeof (numVal) !== 'number') {
    return true;
  }

  const validateNumVal = Number(target);

  if (numVal < validateNumVal) {
    return `ระบุค่าได้ไม่น้อยกว่า ${formatCurrency(validateNumVal)}`;
  }

  return true;
});

defineRule('email', (value?: string) => {
  if (!value) {
    return true;
  }

  if (!/^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i.test(value)) {
    return 'รูปแบบอีเมลไม่ถูกต้อง';
  }
  return true;
});

defineRule('phone', (value?: string) => {
  if (!value) return true;

  const cleaned = value.replace(/\s|-/g, '');

  if (/^\d{4}$/.test(cleaned)) return true;

  if (/^0[2-7]\d{7}$/.test(cleaned)) return true;

  if (/^0[689]\d{8}$/.test(cleaned)) return true;

  if (/^[+][\d-]{7,15}$/.test(value)) return true;

  return 'รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง';
});

defineRule('digits5', (value?: string) => {
  if (!value) return true;

  if (!/^\d{5}$/.test(value)) {
    return 'กรุณาระบุตัวเลข 5 หลัก';
  }

  return true;
});

defineRule('digits13', (value?: string) => {
  if (!value) return true;

  if (!/^\d{13}$/.test(value)) {
    return 'กรุณาระบุตัวเลข 13 หลัก';
  }

  return true;
});

defineRule('citizenCard', (value?: string) => {
  if (!value) {
    return true;
  }

  const idPattern = /^\d{13}$/;
  if (!idPattern.test(value)) {
    return 'รูปแบบเลขบัตรประชาชนไม่ถูกต้อง';
  }

  const digits = value.split('').map(Number);
  let sum = 0;

  for (let i = 0; i < 12; i++) {
    sum += digits[i] * (13 - i);
  }

  const checkDigit = (11 - (sum % 11)) % 10;

  if (checkDigit !== digits[12]) {
    return 'เลขบัตรประชาชนไม่ถูกต้อง';
  }

  return true;
});