import { Order } from '@app/models/orders/order.model';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';

/** Order status 0 = Created: the customer still edits the pre-order. */
export const UNSUBMITTED_ORDER_STATUS = 0;

/** Order statuses 1 = Submitted and 3 = SubmittedByCustomer. */
export const SUBMITTED_ORDER_STATUSES: readonly number[] = [1, 3];

/** Order statuses that the cart shows. 2 = Cancelled is hidden. */
export const CART_ORDER_STATUSES: readonly number[] = [UNSUBMITTED_ORDER_STATUS, ...SUBMITTED_ORDER_STATUSES];

/** True when the reservation of the order is known and cancelled. Unknown or not loaded reservations give false. */
export function isOrderOfCancelledReservation(
  order: Order,
  reservationsById: ReadonlyMap<string, Reservation> | null
): boolean {
  return reservationsById?.get(order.reservationId)?.status === ReservationStatus.Cancelled;
}

/** True when the cart shows at least one order with dishes. One rule for the cart and the header indicator. */
export function cartHasDishes(
  orders: readonly Order[],
  reservationsById: ReadonlyMap<string, Reservation> | null
): boolean {
  return orders.some(
    (order) =>
      CART_ORDER_STATUSES.includes(order.status) &&
      !isOrderOfCancelledReservation(order, reservationsById) &&
      order.orderItems.length > 0
  );
}
