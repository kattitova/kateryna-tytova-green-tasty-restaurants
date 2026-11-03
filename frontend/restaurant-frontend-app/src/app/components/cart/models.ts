import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';
import { Order } from '@app/models/orders/order.model';

export interface CartOrderView {
  order: Order;
  mode: CartOrderMode;
}
