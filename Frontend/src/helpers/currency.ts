export const formatCurrency = (value: number) => {
  if (isNaN(value)) return '0';
  return value.toLocaleString('th-TH', { minimumFractionDigits: 2, });
};

export const formatNumber = (value: number | string) => {
  const convertNumber = Number(value);

  if (isNaN(convertNumber)) return '-'

  return convertNumber.toLocaleString();
};

export const formatPercent = (val: number | null | undefined) => {
  if (val === null || val === undefined) return '-';
  return `${Number(val).toLocaleString(undefined, { maximumFractionDigits: 2 })}%`;
};

const THAI_NUMBER_TEXT = ['', 'หนึ่ง', 'สอง', 'สาม', 'สี่', 'ห้า', 'หก', 'เจ็ด', 'แปด', 'เก้า'];
const THAI_POSITION_TEXT = ['', 'สิบ', 'ร้อย', 'พัน', 'หมื่น', 'แสน', 'ล้าน'];

const parseAmountParts = (amount: number | string): { integer: string; fraction: string } => {
  const [integer, fraction] = Number(amount).toFixed(2).replace(/,/g, '').split('.');
  return { integer, fraction };
};

// Build Thai text for a single digit within its group, including its position text where applicable.
const buildThaiIntegerPart = (
  digit: number,
  pos: number,
  groupPos: number,
  len: number,
  hasPrevInGroup: boolean,
): string => {
  if (digit === 0) return '';

  // Tens place logic
  if (groupPos === 1) {
    if (digit === 2) return 'ยี่' + THAI_POSITION_TEXT[groupPos]; // ยี่สิบ
    if (digit === 1) return THAI_POSITION_TEXT[groupPos]; // สิบ
    return THAI_NUMBER_TEXT[digit] + THAI_POSITION_TEXT[groupPos]; // สามสิบ, สี่สิบ, ...
  }

  // Ones place special-case for 'เอ็ด'
  if (groupPos === 0 && digit === 1) {
    const lastOverallWithPreceding = (pos === 0 && len > 1);
    return (lastOverallWithPreceding || hasPrevInGroup) ? 'เอ็ด' : 'หนึ่ง';
  }

  // Other positions
  return THAI_NUMBER_TEXT[digit] + THAI_POSITION_TEXT[groupPos];
};

const formatThaiInteger = (integer: string): string => {
  let bahtText = '';
  const len = integer.length;
  const seenNonZeroInGroup: Record<number, boolean> = {};

  for (let i = 0; i < len; i++) {
    const digit = +integer[i];
    const pos = len - i - 1;
    const groupIdx = Math.floor(pos / 6);
    const groupPos = pos % 6; // 0 ones, 1 tens, ... within a million-group

    const hasPrevInGroup = !!seenNonZeroInGroup[groupIdx];
    const part = buildThaiIntegerPart(digit, pos, groupPos, len, hasPrevInGroup);
    bahtText += part;
    bahtText += (pos % 6 === 0 && pos > 0) ? 'ล้าน' : '';

    seenNonZeroInGroup[groupIdx] = seenNonZeroInGroup[groupIdx] || digit !== 0;
  }

  return (bahtText || 'ศูนย์') + 'บาท';
};

const formatThaiSatang = (fraction: string): string => {
  if (fraction === '00') return 'ถ้วน';

  let satangText = '';
  if (!fraction.startsWith('0')) {
    if (fraction.startsWith('1')) {
      satangText += 'สิบ';
    } else if (fraction.startsWith('2')) {
      satangText += 'ยี่สิบ';
    } else {
      satangText += THAI_NUMBER_TEXT[+fraction[0]] + 'สิบ';
    }
  }

  if (fraction[1] !== '0') {
    if (fraction[1] === '1' && !fraction.startsWith('0')) {
      satangText += 'เอ็ด';
    } else {
      satangText += THAI_NUMBER_TEXT[+fraction[1]];
    }
  }

  return satangText + 'สตางค์';
};

export const numberToThaiText = (amount: number | string): string => {
  const { integer, fraction } = parseAmountParts(amount);
  const bahtText = formatThaiInteger(integer);
  const satangText = formatThaiSatang(fraction);
  return bahtText + satangText;
}