import { GroupBonus } from './group-bonus.model';

export type GroupReservationStatus = 'Open' | 'Submitted' | 'Closed';

/** GET /guest/groups/{groupReservationId}/status */
export interface GroupStatusResponse {
  currentParticipants: number;
  maxCapacity: number;
  status: GroupReservationStatus;
  orderedCount: number;
  bonus: GroupBonus;
}
