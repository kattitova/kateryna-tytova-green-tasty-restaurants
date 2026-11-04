import { Participant } from './participant.model';

/** GET /guest/groups/{groupReservationId}/sessions */
export interface GroupSessionsResponse {
  /** Guests only. */
  participants: Participant[];
  /** Total of all guests, including guests who have not submitted the cart. */
  total: number;
  /** The owner's own pre-order. */
  owner: Participant;
}
