import { OrderItem } from '../dishes/order-item.model';
import { GroupBonusTier } from '../group-orders/group-bonus.model';

export interface Order {
  orderId: string;
  reservationAddress: string;
  reservationTime: string;
  reservationId: string;
  orderItems: OrderItem[];
  status: number;
  groupBonusTier?: GroupBonusTier | null;
  discountPercent?: number;
}

export interface WaiterPreOrderState {
  reservationId: string;
  createdByName: string;
  createdByRole: string;
  tableName: string;
}
