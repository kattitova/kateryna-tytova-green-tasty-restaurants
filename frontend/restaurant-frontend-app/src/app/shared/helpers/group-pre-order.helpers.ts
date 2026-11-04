import { GroupPreOrderView, GroupSeat } from '@app/models/group-orders/group-pre-order-view.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupStatusResponse } from '@app/models/group-orders/group-status.model';

export type { GroupPreOrderView, GroupSeat } from '@app/models/group-orders/group-pre-order-view.model';

const MINUTE_MS = 60 * 1000;
const HOUR_MS = 60 * MINUTE_MS;
const DAY_MS = 24 * HOUR_MS;

function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

/** First letters of the first two words, upper case. */
function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0].toUpperCase())
    .join('');
}

export function buildGroupPreOrderView(
  sessions: GroupSessionsResponse,
  status: GroupStatusResponse,
  ownerName: string,
): GroupPreOrderView {
  const { owner, participants } = sessions;
  const { bonus } = status;
  const seatCount = status.maxCapacity;

  const ownerOrdered = owner.isSubmitted;
  const ownerSubtotal = ownerOrdered ? owner.total : 0;
  const orderedGuests = participants.filter((guest) => guest.isSubmitted);
  const guestsSubtotal = orderedGuests.reduce((sum, guest) => sum + guest.total, 0);
  const grandTotal = round2(ownerSubtotal + guestsSubtotal);

  const percentAmount = round2((grandTotal * bonus.discountPercent) / 100);
  const discountAmount = bonus.state === 'Achieved' ? percentAmount : 0;
  const unlockDiscountAmount = bonus.state === 'InProgress' ? percentAmount : 0;

  const seats: GroupSeat[] = [
    { kind: 'owner', ordered: ownerOrdered, initials: initialsOf(ownerName) },
    ...participants.map(
      (guest): GroupSeat => ({
        kind: 'guest',
        ordered: guest.isSubmitted,
        initials: initialsOf(guest.name),
        name: guest.name,
        participantId: guest.id,
      }),
    ),
  ];
  for (let seatNumber = seats.length + 1; seatNumber <= seatCount; seatNumber++) {
    seats.push({ kind: 'empty', seatNumber });
  }

  const isOpen = status.status === 'Open';

  return {
    seatCount,
    orderedCount: status.orderedCount,
    remainingCount: Math.max(0, seatCount - status.orderedCount),
    seats,
    ownerOrdered,
    ownerSubtotal,
    orderedGuestCount: orderedGuests.length,
    guestsSubtotal,
    grandTotal,
    discountAmount,
    finalTotal: round2(grandTotal - discountAmount),
    unlockDiscountAmount,
    isOpen,
    isSubmitted: status.status === 'Submitted',
    canSubmit: isOpen && status.orderedCount >= 1,
    bonus,
  };
}

export function formatTimeLeft(deadline: Date, now: Date): string | null {
  const diff = deadline.getTime() - now.getTime();
  if (diff <= 0) {
    return null;
  }

  if (diff >= DAY_MS) {
    const days = Math.floor(diff / DAY_MS);
    const hours = Math.floor((diff % DAY_MS) / HOUR_MS);
    return `${days} ${days === 1 ? 'day' : 'days'} ${hours} h left`;
  }

  if (diff >= HOUR_MS) {
    return `${Math.floor(diff / HOUR_MS)} h left`;
  }

  return `${Math.max(1, Math.floor(diff / MINUTE_MS))} min left`;
}
