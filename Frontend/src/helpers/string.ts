/**
 * Deprecated: Misnamed helper that returns true when the input string is non-empty (after trimming).
 * @deprecated Use `isNonEmptyString` for presence checks or `isEmptyString` for absence checks.
 */
export const stringEmpty = (value: string | null | undefined): boolean => {
  return value != null && value.trim() !== '';
};

/**
 * Returns true if the input has non-whitespace characters.
 * Note: This reflects the current semantics of `stringEmpty` (despite its name).
 */
export const isNonEmptyString = (value: string | null | undefined): boolean => {
  return value != null && value.trim() !== '';
};

/**
 * Returns true if the input is null/undefined or only whitespace.
 */
export const isEmptyString = (value: string | null | undefined): boolean => {
  return !isNonEmptyString(value);
};

/**
 * Lowercase only the first character of the provided string.
 * - If the value is nullish, returns an empty string.
 * - If the first character is already lowercase, the input is returned unchanged.
 */
export const lowercaseFirst = (value: string | null | undefined): string => {
  if (!value) return '';
  if (value.length === 0) return '';
  return value.charAt(0).toLowerCase() + value.slice(1);
};