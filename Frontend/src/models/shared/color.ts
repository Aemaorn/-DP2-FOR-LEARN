export type ColorClass = {
  bgColorClass: string;
  textColorClass: string;
};

export type ColorLabel = {
  color: string;
  label: string;
}

// Base on Color in Tailwindcss ref: https://tailwindcss.com/docs/colors
export type Color =
  | 'red'
  | 'orange'
  | 'amber'
  | 'yellow'
  | 'lime'
  | 'green'
  | 'emerald'
  | 'teal'
  | 'cyan'
  | 'sky'
  | 'blue'
  | 'indigo'
  | 'violet'
  | 'purple'
  | 'fuchsia'
  | 'pink'
  | 'rose'
  | 'slate'
  | 'gray'
  | 'zinc'
  | 'neutral'
  | 'stone';