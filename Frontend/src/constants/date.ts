const currentYear = new Date().getFullYear();

const startYear = 2565;
const endYear = currentYear + 1 + 543;

export const YearOptions = Array.from({ length: endYear - startYear + 1 }, (_, i) => {
  const year = startYear + i;
  return {
    label: year.toString(),
    value: year,
  };
}).sort((a, b) => b.value - a.value);

export const BudgetYearOptions = Array.from({ length: 2 }, (_, i) => {
  const year = currentYear + i;

  return {
    label: (year + 543).toString(),
    value: (year + 543),
  };
}).sort((a, b) => b.value - a.value);

export const MonthOptions = [
  { label: 'ม.ค.', value: 1 },
  { label: 'ก.พ.', value: 2 },
  { label: 'มี.ค.', value: 3 },
  { label: 'เม.ย.', value: 4 },
  { label: 'พ.ค.', value: 5 },
  { label: 'มิ.ย.', value: 6 },
  { label: 'ก.ค.', value: 7 },
  { label: 'ส.ค.', value: 8 },
  { label: 'ก.ย.', value: 9 },
  { label: 'ต.ค.', value: 10 },
  { label: 'พ.ย.', value: 11 },
  { label: 'ธ.ค.', value: 12 },
];

export const QuarterOptions = [
  { label: 'ไตรมาสที่ 1 (ม.ค. - มี.ค.)', value: 1 },
  { label: 'ไตรมาสที่ 2 (เม.ย. - มิ.ย.)', value: 2 },
  { label: 'ไตรมาสที่ 3 (ก.ค. - ก.ย.)', value: 3 },
  { label: 'ไตรมาสที่ 4 (ต.ค. - ธ.ค.)', value: 4 },
];

export const getYearOptionsWithValue = (selectedValue?: number) => {
  if (!selectedValue) {
    return BudgetYearOptions;
  }

  const currentBuddhistYear = currentYear + 543;
  const nextBuddhistYear = currentBuddhistYear + 1;

  const isOutsideRange = selectedValue < currentBuddhistYear || selectedValue > nextBuddhistYear;

  if (!isOutsideRange) {
    return BudgetYearOptions;
  }

  const allOptions = [
    ...BudgetYearOptions,
    {
      label: selectedValue.toString(),
      value: selectedValue,
    }
  ];

  return allOptions.sort((a, b) => b.value - a.value);
};
