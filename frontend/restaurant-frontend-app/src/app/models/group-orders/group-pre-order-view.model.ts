import { GroupBonus } from './group-bonus.model';

export type GroupSeat =
  | { kind: 'owner'; ordered: boolean; initials: string }
  | { kind: 'guest'; ordered: boolean; initials: string; name: string; participantId: string }
  | { kind: 'empty'; seatNumber: number };

export interface GroupPreOrderView {
  seatCount: number;
  orderedCount: number;
  /** max(0, seatCount - orderedCount) */
  remainingCount: number;
  /** Owner, then guests in API order, then empty seats numbered up to seatCount. */
  seats: GroupSeat[];
  ownerOrdered: boolean;
  /** 0 when the owner has not ordered. */
  ownerSubtotal: number;
  orderedGuestCount: number;
  /** Submitted guests only. */
  guestsSubtotal: number;
  grandTotal: number;
  discountAmount: number;
  finalTotal: number;
  unlockDiscountAmount: number;
  isOpen: boolean;
  isSubmitted: boolean;
  canSubmit: boolean;
  bonus: GroupBonus;
}
