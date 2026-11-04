import { CurrencyPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';

import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';
import { Order } from '@app/models/orders/order.model';
import { ReservationDetails } from '@app/models/reservations/reservation-details.model';
import { ReservationDetailsComponent } from '@app/shared/custom-components/reservation-details/reservation-details.component';

@Component({
  selector: 'app-submitted-order',
  standalone: true,
  imports: [CurrencyPipe, ReservationDetailsComponent],
  templateUrl: './submitted-order.component.html',
  styleUrl: './submitted-order.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubmittedOrderComponent {
  readonly order = input.required<Order>();
  readonly orderMode = input.required<CartOrderMode>();
  readonly details = input.required<ReservationDetails>();

  readonly canceled = output<void>();
  readonly edited = output<void>();

  readonly totalPrice = computed(() =>
    this.order()
      .orderItems.filter((item) => item.state !== 2)
      .reduce(
        (total, item) =>
          total + item.dish.price * item.quantity,
        0,
      ),
  );

  // Fail closed: only a single-guest order can go back to edit. A group order is submitted from the group flow.
  readonly canEdit = computed(() => this.orderMode() === 'single');

  cancelOrder(): void {
    this.canceled.emit();
  }

  editOrder(): void {
    if (this.canEdit()) {
      this.edited.emit();
    }
  }
}
