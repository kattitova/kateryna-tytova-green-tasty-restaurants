import { round2 } from './money.helpers';

describe('round2', () => {
  const cases: [number, number][] = [
    [13.6, 13.6],
    [59.97, 59.97],
    [0, 0],
    [1.005 * 1000, 1005],
    [10.126, 10.13],
    [10.124, 10.12],
    [0.1 + 0.2, 0.3],
    [-2.346, -2.35],
  ];

  cases.forEach(([input, expected]) => {
    it(`rounds ${input} to ${expected}`, () => {
      expect(round2(input)).toBe(expected);
    });
  });
});
