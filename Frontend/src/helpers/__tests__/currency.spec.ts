import { describe, it, expect } from 'vitest';
import { numberToThaiText } from '../currency';

// Note: These tests assert Thai text spelling conventions encoded in currency.ts

describe('numberToThaiText', () => {
  it('formats 0 as ศูนย์บาทถ้วน', () => {
    expect(numberToThaiText(0)).toBe('ศูนย์บาทถ้วน');
  });

  it('formats integer baht without satang', () => {
    expect(numberToThaiText(1)).toBe('หนึ่งบาทถ้วน');
    expect(numberToThaiText(11)).toBe('สิบเอ็ดบาทถ้วน');
    expect(numberToThaiText(21)).toBe('ยี่สิบเอ็ดบาทถ้วน');
    expect(numberToThaiText(101)).toBe('หนึ่งร้อยเอ็ดบาทถ้วน');
    expect(numberToThaiText(1000000)).toBe('หนึ่งล้านบาทถ้วน');
  });

  it('formats satang only (no tens) correctly', () => {
    expect(numberToThaiText(0.01)).toBe('ศูนย์บาทหนึ่งสตางค์');
    expect(numberToThaiText(0.09)).toBe('ศูนย์บาทเก้าสตางค์');
  });

  it('formats satang with tens correctly', () => {
    expect(numberToThaiText(0.10)).toBe('ศูนย์บาทสิบสตางค์');
    expect(numberToThaiText(0.11)).toBe('ศูนย์บาทสิบเอ็ดสตางค์');
    expect(numberToThaiText(0.20)).toBe('ศูนย์บาทยี่สิบสตางค์');
    expect(numberToThaiText(0.50)).toBe('ศูนย์บาทห้าสิบสตางค์');
  });

  it('formats mixed baht and satang', () => {
    expect(numberToThaiText(12.20)).toBe('สิบสองบาทยี่สิบสตางค์');
    expect(numberToThaiText(25.01)).toBe('ยี่สิบห้าบาทหนึ่งสตางค์');
    expect(numberToThaiText(111.11)).toBe('หนึ่งร้อยสิบเอ็ดบาทสิบเอ็ดสตางค์');
  });

  it('accepts string inputs', () => {
    expect(numberToThaiText('1000000')).toBe('หนึ่งล้านบาทถ้วน');
    expect(numberToThaiText('0.50')).toBe('ศูนย์บาทห้าสิบสตางค์');
  });

  it('formats large numbers in the millions correctly', () => {
    expect(numberToThaiText(1234567)).toBe('หนึ่งล้านสองแสนสามหมื่นสี่พันห้าร้อยหกสิบเจ็ดบาทถ้วน');
    expect(numberToThaiText(12345678.9)).toBe('สิบสองล้านสามแสนสี่หมื่นห้าพันหกร้อยเจ็ดสิบแปดบาทเก้าสิบสตางค์');
    expect(numberToThaiText(123456789)).toBe('หนึ่งร้อยยี่สิบสามล้านสี่แสนห้าหมื่นหกพันเจ็ดร้อยแปดสิบเก้าบาทถ้วน');
  });

  it('formats numbers in the billions (พันล้าน) correctly', () => {
    expect(numberToThaiText(1000000000)).toBe('หนึ่งพันล้านบาทถ้วน');
    expect(numberToThaiText(2000000000)).toBe('สองพันล้านบาทถ้วน');
    expect(numberToThaiText(1234567890)).toBe('หนึ่งพันสองร้อยสามสิบสี่ล้านห้าแสนหกหมื่นเจ็ดพันแปดร้อยเก้าสิบบาทถ้วน');
  });

  // TODO: Support multi-"ล้าน" groups (e.g., ล้านล้าน) in implementation
  // When supported, enable this test and adjust implementation accordingly
  it.skip('formats numbers with multiple "ล้าน" groups (ล้านล้าน)', () => {
    expect(numberToThaiText(1000000000000)).toBe('หนึ่งล้านล้านบาทถ้วน');
  });
});
