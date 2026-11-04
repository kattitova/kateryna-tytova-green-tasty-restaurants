import { OrderItem } from '@app/models/dishes/order-item.model';
import { GroupBonusTier } from '@app/models/group-orders/group-bonus.model';
import { OrderBill } from '@app/models/orders/order-bill.model';
import { round2 } from './money.helpers';

/** BE/Constants/Orders/OrderItemState.cs: Canceled = 2 */
export const CANCELED_ORDER_ITEM_STATE = 2;

export function buildOrderBill(
  items: readonly OrderItem[],
  discountPercent: number | null | undefined,
): OrderBill {
  const percent = discountPercent != null && discountPercent > 0 ? discountPercent : 0;
  const subtotal = round2(
    items
      .filter((item) => item.state !== CANCELED_ORDER_ITEM_STATE)
      .reduce((sum, item) => sum + item.dish.price * item.quantity, 0),
  );
  const discountAmount = round2((subtotal * percent) / 100);

  return {
    subtotal,
    discountPercent: percent,
    discountAmount,
    finalTotal: round2(subtotal - discountAmount),
  };
}

export function groupBonusLabel(
  tier: GroupBonusTier | null | undefined,
  discountPercent: number | null | undefined,
): string | null {
  if (tier === 'FreeDessert') {
    return 'Free dessert';
  }
  if (tier === 'TenPercentDiscount' && discountPercent != null && discountPercent > 0) {
    return `Group bonus: −${discountPercent}%`;
  }
  return null;
}
