import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';
import {
  isGroupReservation,
  MIN_GROUP_GUEST_COUNT,
  toCartOrderMode,
} from './group-reservation.helpers';

describe('MIN_GROUP_GUEST_COUNT', () => {
  it('is 2', () => {
    expect(MIN_GROUP_GUEST_COUNT).toBe(2);
  });
});

describe('isGroupReservation', () => {
  const cases: { guestCount: number; expected: boolean }[] = [
    { guestCount: 0, expected: false },
    { guestCount: 1, expected: false },
    { guestCount: 2, expected: true },
    { guestCount: 6, expected: true },
  ];

  cases.forEach(({ guestCount, expected }) => {
    it(`returns ${expected} for ${guestCount} guest(s)`, () => {
      expect(isGroupReservation(guestCount)).toBe(expected);
    });
  });
});

describe('toCartOrderMode', () => {
  const cases: { guestCount: number | undefined; expected: CartOrderMode }[] = [
    { guestCount: undefined, expected: 'unknown' },
    { guestCount: 0, expected: 'single' },
    { guestCount: 1, expected: 'single' },
    { guestCount: 2, expected: 'group' },
    { guestCount: 5, expected: 'group' },
  ];

  cases.forEach(({ guestCount, expected }) => {
    it(`returns '${expected}' for guestCount ${guestCount}`, () => {
      expect(toCartOrderMode(guestCount)).toBe(expected);
    });
  });
});
