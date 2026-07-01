import { describe, it, expect } from 'vitest';
import { stringEmpty, isNonEmptyString, isEmptyString, lowercaseFirst } from '@/helpers/string';

/**
 * Note: `stringEmpty` is currently misnamed and actually checks for non-empty strings.
 * These tests codify existing semantics to avoid regressions.
 */

describe('helpers/string', () => {
  describe('stringEmpty (misnamed, actually checks non-empty)', () => {
    it('returns true for non-empty non-whitespace strings', () => {
      expect(stringEmpty('abc')).toBe(true);
      expect(stringEmpty('  abc  ')).toBe(true);
    });

    it('returns false for empty or whitespace-only strings', () => {
      expect(stringEmpty('')).toBe(false);
      expect(stringEmpty('   ')).toBe(false);
    });

    it('returns false for nullish values', () => {
      expect(stringEmpty(null as unknown as string)).toBe(false);
      expect(stringEmpty(undefined as unknown as string)).toBe(false);
    });
  });

  describe('isNonEmptyString', () => {
    it('matches semantics of stringEmpty (non-empty check)', () => {
      expect(isNonEmptyString('abc')).toBe(true);
      expect(isNonEmptyString('  abc  ')).toBe(true);
      expect(isNonEmptyString('')).toBe(false);
      expect(isNonEmptyString('   ')).toBe(false);
      expect(isNonEmptyString(null as unknown as string)).toBe(false);
      expect(isNonEmptyString(undefined as unknown as string)).toBe(false);
    });
  });

  describe('isEmptyString', () => {
    it('is the logical negation of isNonEmptyString', () => {
      expect(isEmptyString('abc')).toBe(false);
      expect(isEmptyString('  abc  ')).toBe(false);
      expect(isEmptyString('')).toBe(true);
      expect(isEmptyString('   ')).toBe(true);
      expect(isEmptyString(null as unknown as string)).toBe(true);
      expect(isEmptyString(undefined as unknown as string)).toBe(true);
    });
  });

  describe('lowercaseFirst', () => {
    it('lowercases only the first character', () => {
      expect(lowercaseFirst('Abc')).toBe('abc');
      expect(lowercaseFirst('aBC')).toBe('aBC');
    });

    it('handles empty and nullish inputs', () => {
      expect(lowercaseFirst('')).toBe('');
      expect(lowercaseFirst(null as unknown as string)).toBe('');
      expect(lowercaseFirst(undefined as unknown as string)).toBe('');
    });

    it('handles leading whitespace by lowercasing the first character (which is whitespace)', () => {
      // This will keep the whitespace and not change subsequent characters
      expect(lowercaseFirst(' Abc')).toBe(' Abc');
    });
  });
});
