import { GroupBonus } from '@app/models/group-orders/group-bonus.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupReservationStatus, GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { buildGroupPreOrderView, formatTimeLeft } from './group-pre-order.helpers';

const HOUR = 60 * 60 * 1000;

function participant(id: string, name: string, total: number, isSubmitted: boolean, type: 'Owner' | 'Guest' = 'Guest'): Participant {
  return { id, type, name, items: [], total, isSubmitted };
}

function sessions(owner: Participant, guests: Participant[]): GroupSessionsResponse {
  return { owner, participants: guests, total: guests.reduce((sum, g) => sum + g.total, 0) };
}

function status(
  maxCapacity: number,
  orderedCount: number,
  bonus: GroupBonus,
  state: GroupReservationStatus = 'Open',
): GroupStatusResponse {
  return { currentParticipants: maxCapacity, maxCapacity, status: state, orderedCount, bonus };
}

const achievedTenPercent: GroupBonus = { state: 'Achieved', tier: 'TenPercentDiscount', discountPercent: 10, deadline: null };
const inProgressTenPercent: GroupBonus = { state: 'InProgress', tier: 'TenPercentDiscount', discountPercent: 10, deadline: '2030-01-09T12:00:00Z' };

describe('buildGroupPreOrderView', () => {
  const owner = participant('owner-g1', 'Owner', 27, true, 'Owner');
  const anna = participant('guest-anna', 'Anna Smith', 36, true);
  const ben = participant('guest-ben', 'Ben', 73, true);

  it('sums the owner and the submitted guests and leaves out a waiting guest', () => {
    const waiting = participant('guest-cara', 'Cara', 50, false);

    const view = buildGroupPreOrderView(sessions(owner, [anna, ben, waiting]), status(4, 3, achievedTenPercent), 'Olga');

    expect(view.ownerSubtotal).toBe(27);
    expect(view.guestsSubtotal).toBe(109);
    expect(view.orderedGuestCount).toBe(2);
    expect(view.grandTotal).toBe(136);
  });

  it('applies the 10 % discount to the total when the bonus is achieved', () => {
    const view = buildGroupPreOrderView(sessions(owner, [anna, ben]), status(3, 3, achievedTenPercent), 'Olga');

    expect(view.discountAmount).toBeCloseTo(13.6, 2);
    expect(view.finalTotal).toBeCloseTo(122.4, 2);
  });

  it('shows no discount but the unlock amount while the bonus is in progress', () => {
    const view = buildGroupPreOrderView(sessions(owner, [anna, ben]), status(4, 3, inProgressTenPercent), 'Olga');

    expect(view.discountAmount).toBe(0);
    expect(view.finalTotal).toBe(136);
    expect(view.unlockDiscountAmount).toBeCloseTo(13.6, 2);
  });

  it('lists the owner, the guests in API order and numbered empty seats', () => {
    const guests = [anna, ben, participant('guest-cara', 'Cara', 0, false)];

    const view = buildGroupPreOrderView(sessions(owner, guests), status(6, 3, inProgressTenPercent), 'Olga Petrenko');

    expect(view.seatCount).toBe(6);
    expect(view.remainingCount).toBe(3);
    expect(view.seats).toEqual([
      { kind: 'owner', ordered: true, initials: 'OP' },
      { kind: 'guest', ordered: true, initials: 'AS', name: 'Anna Smith', participantId: 'guest-anna' },
      { kind: 'guest', ordered: true, initials: 'B', name: 'Ben', participantId: 'guest-ben' },
      { kind: 'guest', ordered: false, initials: 'C', name: 'Cara', participantId: 'guest-cara' },
      { kind: 'empty', seatNumber: 5 },
      { kind: 'empty', seatNumber: 6 },
    ]);
  });

  it('cannot submit when nobody has ordered', () => {
    const view = buildGroupPreOrderView(
      sessions(participant('owner-g1', 'Owner', 0, false, 'Owner'), []),
      status(4, 0, inProgressTenPercent),
      'Olga',
    );

    expect(view.canSubmit).toBeFalse();
  });

  it('cannot submit when the group is already submitted', () => {
    const view = buildGroupPreOrderView(sessions(owner, [anna]), status(4, 2, achievedTenPercent, 'Submitted'), 'Olga');

    expect(view.canSubmit).toBeFalse();
    expect(view.isSubmitted).toBeTrue();
  });

  it('can submit an open group with at least one pre-order', () => {
    const view = buildGroupPreOrderView(sessions(owner, []), status(4, 1, inProgressTenPercent), 'Olga');

    expect(view.canSubmit).toBeTrue();
  });
});

describe('formatTimeLeft', () => {
  const now = new Date('2030-01-01T00:00:00Z');

  it('shows days and hours for 27 hours', () => {
    expect(formatTimeLeft(new Date(now.getTime() + 27 * HOUR), now)).toBe('1 day 3 h left');
  });

  it('shows minutes for 45 minutes', () => {
    expect(formatTimeLeft(new Date(now.getTime() + 45 * 60 * 1000), now)).toBe('45 min left');
  });

  it('returns null for a deadline in the past', () => {
    expect(formatTimeLeft(new Date(now.getTime() - HOUR), now)).toBeNull();
  });
});
