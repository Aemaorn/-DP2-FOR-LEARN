// Shared helper for the demo-only mock data used by st011/st012/st013 (province/district/subdistrict).
// Persists to localStorage so data stays consistent across tabs and page reloads until the real
// backend endpoints exist.
const STORAGE_PREFIX = 'mock-st-';

export const loadMockList = <T>(key: string, seed: T[]): T[] => {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + key);

    if (raw) return JSON.parse(raw) as T[];
  } catch {
    // corrupted storage — fall back to the seed data
  }

  return seed;
};

export const saveMockList = <T>(key: string, data: T[]): void => {
  try {
    localStorage.setItem(STORAGE_PREFIX + key, JSON.stringify(data));
  } catch {
    // storage unavailable/full — demo data just won't persist this time
  }
};
