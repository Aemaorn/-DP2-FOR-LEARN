import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const GHBPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#FFF1E8',
      100: '#FFE0CC',
      200: '#FFC299',
      300: '#FFA366',
      400: '#FE7733',
      500: '#FE5000',
      600: '#E54800',
      700: '#993000',
      800: '#732400',
      900: '#4D1800',
    },
  },
  components: {
    button: {
      colorScheme: {
        light: {
          root: {
            success: {
              background: 'oklch(59.6% 0.145 163.225)',
              hoverBackground: 'oklch(50.8% 0.118 165.612)',
              activeBackground: 'oklch(50.8% 0.118 165.612)',
              borderColor: 'oklch(59.6% 0.145 163.225)',
              hoverBorderColor: 'oklch(50.8% 0.118 165.612)',
              activeBorderColor: 'oklch(50.8% 0.118 165.612)',
            },
            warn: {
              background: 'oklch(85.2% 0.199 91.936)',
              hoverBackground: 'oklch(79.5% 0.184 86.047)',
              activeBackground: 'oklch(79.5% 0.184 86.047)',
              borderColor: 'oklch(85.2% 0.199 91.936)',
              hoverBorderColor: 'oklch(79.5% 0.184 86.047)',
              activeBorderColor: 'oklch(79.5% 0.184 86.047)',
            },
          },
          outlined: {
            success: {
              activeBackground: 'oklch(97.9% 0.021 166.113)',
              hoverBackground: 'oklch(97.9% 0.021 166.113)',
              borderColor: 'oklch(59.6% 0.145 163.225)',
              color: 'oklch(59.6% 0.145 163.225)',
            },
            warn: {
              activeBackground: 'oklch(98.7% 0.026 102.212)',
              hoverBackground: 'oklch(98.7% 0.026 102.212)',
              borderColor: 'oklch(85.2% 0.199 91.936)',
              color: 'oklch(85.2% 0.199 91.936)',
            },
          },
        },
      },
    },
  },
});

export const GHBThemeOption = {
  darkModeSelector: '.dark',
  cssLayer: {
    name: 'primevue',
    order: 'theme, base, primevue, component',
  },
  locale: {
    fileSizeTypes: ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
    dayNames: ['อาทิตย์', 'จันทร์', 'อังคาร', 'พุธ', 'พฤหัสบดี', 'ศุกร์', 'เสาร์'],
    dayNamesShort: ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'],
    dayNamesMin: ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'],
    monthNames: [
      'มกราคม',
      'กุมภาพันธ์',
      'มีนาคม',
      'เมษายน',
      'พฤษภาคม',
      'มิถุนายน',
      'กรกฎาคม',
      'สิงหาคม',
      'กันยายน',
      'ตุลาคม',
      'พฤศจิกายน',
      'ธันวาคม',
    ],
    monthNamesShort: [
      'ม.ค.',
      'ก.พ.',
      'มี.ค.',
      'เม.ย.',
      'พ.ค.',
      'มิ.ย.',
      'ก.ค.',
      'ส.ค.',
      'ก.ย.',
      'ต.ค.',
      'พ.ย.',
      'ธ.ค.',
    ],
    today: 'วันนี้',
    clear: 'ล้าง',
    chooseYear: 'เลือกปี',
    chooseMonth: 'เลือกเดือน',
    chooseDate: 'เลือกวัน',
    dateFormat: 'dd/mm/yy',
    weekHeader: 'สัปดาห์',
  },
};
