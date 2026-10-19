import { city, fmt } from './format';

describe('fmt', () => {
  it('returns a dash for missing or non-finite numbers', () => {
    expect(fmt(null)).toBe('—');
    expect(fmt(undefined)).toBe('—');
    expect(fmt(NaN)).toBe('—');
    expect(fmt(Infinity)).toBe('—');
  });

  it('uses no decimals and en-US grouping from 1000', () => {
    expect(fmt(1000)).toBe('1,000');
    expect(fmt(12345.67)).toBe('12,346');
    expect(fmt(-2500.4, 3)).toBe('-2,500');
  });

  it('uses at most one decimal from 10', () => {
    expect(fmt(10)).toBe('10');
    expect(fmt(12.345, 3)).toBe('12.3');
    expect(fmt(999.96)).toBe('1,000');
  });

  it('uses up to d decimals below 10', () => {
    expect(fmt(1.2345)).toBe('1.2');
    expect(fmt(1.2345, 3)).toBe('1.235');
    expect(fmt(0.5, 0)).toBe('1');
    expect(fmt(0)).toBe('0');
  });
});

describe('city', () => {
  it('takes the part before the first comma', () => {
    expect(city('Oslo, Norway')).toBe('Oslo');
    expect(city(null)).toBe('—');
  });
});
