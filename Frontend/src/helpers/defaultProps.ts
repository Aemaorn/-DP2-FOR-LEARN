import type { PropType } from "vue";

export default function defaultProps<T>(defaultValue: T, isRequired: boolean = false) {
  return {
    type: null as unknown as PropType<T>,
    default: defaultValue,
    required: isRequired,
  };
}