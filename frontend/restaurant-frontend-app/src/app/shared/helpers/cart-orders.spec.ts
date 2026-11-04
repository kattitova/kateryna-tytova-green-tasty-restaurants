import { Order } from '@app/models/orders/order.model';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';

import { cartHasDishes, isOrderOfCancelledReservation } from './cart-orders';

describe('cart orders', () => {
  const order = (status: number, dishCount = 1, reservationId = 'res-1'): Order =>
    ({
      orderId: `order-${status}`,
      reservationId,
      status,
      orderItems: Array.from({ length: dishCount }, (_, index) => ({ quantity: 1, dish: { id: `d${index}` } })),
    }) as Order;

  const reservations = (status: ReservationStatus): ReadonlyMap<string, Reservation> =>
    new Map([['res-1', { id: 'res-1', status } as Reservation]]);

  describe('isOrderOfCancelledReservation', () => {
    it('is true only for an order of a cancelled reservation', () => {
      expect(isOrderOfCancelledReservation(order(0), reservations(ReservationStatus.Cancelled))).toBeTrue();
      expect(isOrderOfCancelledReservation(order(0), reservations(ReservationStatus.Reserved))).toBeFalse();
    });

    it('is false when the reservations are not loaded or the reservation is unknown', () => {
      expect(isOrderOfCancelledReservation(order(0), null)).toBeFalse();
      expect(isOrderOfCancelledReservation(order(0, 1, 'res-2'), reservations(ReservationStatus.Cancelled))).toBeFalse();
    });
  });

  describe('cartHasDishes', () => {
    [0, 1, 3].forEach((status) => {
      it(`is true for an order with status ${status} that has dishes`, () => {
        expect(cartHasDishes([order(status)], reservations(ReservationStatus.Reserved))).toBeTrue();
      });
    });

    it('is false for a cancelled pre-order (status 2)', () => {
      expect(cartHasDishes([order(2)], reservations(ReservationStatus.Reserved))).toBeFalse();
    });

    it('is false for an order without dishes', () => {
      expect(cartHasDishes([order(0, 0)], reservations(ReservationStatus.Reserved))).toBeFalse();
    });

    it('is false for an order of a cancelled reservation', () => {
      expect(cartHasDishes([order(0)], reservations(ReservationStatus.Cancelled))).toBeFalse();
    });

    it('is true when the reservations did not load', () => {
      expect(cartHasDishes([order(0)], null)).toBeTrue();
    });

    it('is false for no orders', () => {
      expect(cartHasDishes([], null)).toBeFalse();
    });
  });
});
