import type { Option } from "@/models/shared/option";

export const ToDateOnly = (val?: Date | string, lang: string = 'th'): string => {
  if (!val) {
    return '';
  }

  if (typeof val === 'string') {
    val = new Date(val);
  }

  return new Intl.DateTimeFormat(lang, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(val);
};

export const ConvertStartToEndDate = (startDate?: Date, endDate?: Date): [startDate?: Date, endDate?: Date] => {
  if (startDate) {
    startDate = setStartDate(new Date(startDate));
  }

  if (endDate) {
    endDate = setEndDate(new Date(endDate));
  }

  return [startDate, endDate];
};

export const setStartDate = (date: Date) => {
  date.setHours(0, 0, 0, 0);

  return date;
}

export const setEndDate = (date: Date) => {
  date.setHours(23, 59, 59, 59);

  return date;
}

export const ToTHDateFullMonthOnly = (val?: Date | string, lang: string = 'th'): string => {
  if (!val) {
    return '';
  }

  if (typeof val === 'string') {
    val = new Date(val);
  }

  return new Intl.DateTimeFormat(lang, {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  }).format(val);
};

export const ToDateTime = (val?: Date | string, lang: string = 'th'): string => {
  if (!val) {
    return '';
  }

  if (typeof val === 'string') val = new Date(val);

  const option: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: false,
  };

  let format = new Intl.DateTimeFormat(lang, option).format(val);

  if (lang == 'th') {
    format += ' น.';
  }

  return format;
};

export const ToDateTimeFully = (val?: Date | string, lang: string = 'th'): string => {
  if (!val) {
    return '';
  }

  if (typeof val === 'string') val = new Date(val);

  const option: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  };

  let format = new Intl.DateTimeFormat(lang, option).format(val);

  if (lang == 'th') {
    format += ' น.';
  }

  return format;
}

export const getMonthOptions = (): Option[] => [
  'มกราคม', 'กุมภาพันธ์', 'มีนาคม', 'เมษายน',
  'พฤษภาคม', 'มิถุนายน', 'กรกฎาคม', 'สิงหาคม',
  'กันยายน', 'ตุลาคม', 'พฤศจิกายน', 'ธันวาคม'
].map((label, index) => ({
  value: index + 1,
  label,
}));

export const getThaiMonthName = (month: number): string => {
  const monthMap: Record<number, string> = {
    0: 'มกราคม',
    1: 'กุมภาพันธ์',
    2: 'มีนาคม',
    3: 'เมษายน',
    4: 'พฤษภาคม',
    5: 'มิถุนายน',
    6: 'กรกฎาคม',
    7: 'สิงหาคม',
    8: 'กันยายน',
    9: 'ตุลาคม',
    10: 'พฤศจิกายน',
    11: 'ธันวาคม'
  };

  return monthMap[month] ?? '';
}

export const timeFromNowSimple = (date: Date | string): string => {
  const now = new Date();
  const target = typeof date === "string" ? new Date(date) : date;
  const diff = now.getTime() - target.getTime();

  if (diff <= 0) return "ผ่านมาแล้ว";

  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  if (days > 0) return `${days} วัน`;

  const hours = Math.floor(diff / (1000 * 60 * 60));
  if (hours > 0) return `${hours} ชั่วโมง`;

  const minutes = Math.floor(diff / (1000 * 60));
  if (minutes > 0) return `${minutes} นาที`;

  return `${Math.floor(diff / 1000)} วินาที`;
};

export const FindDiffDate = (startDate: Date, endDate: Date) => {
  const [start, end] = ConvertStartToEndDate(startDate, endDate);

  if (!start || !end) {
    return { years: 0, months: 0, days: 0 };
  }

  let years = end.getFullYear() - start.getFullYear();
  let months = end.getMonth() - start.getMonth();
  let days = end.getDate() - start.getDate() + 1; // +1 for inclusive

  // Adjust if days is negative
  if (days < 0) {
    months -= 1;
    const prevMonth = new Date(end.getFullYear(), end.getMonth(), 0);
    days += prevMonth.getDate();
  }

  // Adjust if months is negative
  if (months < 0) {
    years -= 1;
    months += 12;
  }

  // Check if we have a complete year
  if (months === 12) {
    years += 1;
    months = 0;
  }

  // Check if we have a complete month
  const startDay = start.getDate();
  const endDay = end.getDate();
  const daysInEndMonth = new Date(end.getFullYear(), end.getMonth() + 1, 0).getDate();

  if (endDay >= daysInEndMonth && startDay === 1) {
    months += 1;
    days = 0;
  }

  // Final check for year overflow
  if (months >= 12) {
    years += Math.floor(months / 12);
    months = months % 12;
  }

  return { years, months, days };
};

export const FindDiffMonth = (start: Date, end: Date): number => {
  let months = (end.getFullYear() - start.getFullYear()) * 12;
  months += end.getMonth() - start.getMonth();

  if (end.getDate() < start.getDate()) {
    months -= 1;
  }

  return months;
}