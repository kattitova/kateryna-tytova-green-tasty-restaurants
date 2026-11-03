import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';

export const MIN_GROUP_GUEST_COUNT = 2;

export function isGroupReservation(guestCount: number): boolean {
  return guestCount >= MIN_GROUP_GUEST_COUNT;
}

export function toCartOrderMode(guestCount: number | undefined): CartOrderMode {
  if (guestCount === undefined) {
    return 'unknown';
  }

  return isGroupReservation(guestCount) ? 'group' : 'single';
}
