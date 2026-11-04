import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';
import { Order } from '@app/models/orders/order.model';
import { ReservationDetails } from '@app/models/reservations/reservation-details.model';

export interface CartOrderView {
  order: Order;
  mode: CartOrderMode;
  details: ReservationDetails;
}
