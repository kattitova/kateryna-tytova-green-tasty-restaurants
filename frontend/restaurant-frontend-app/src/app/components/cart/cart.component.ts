import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, forkJoin, map, of } from 'rxjs';

import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';
import { Order } from '@app/models/orders/order.model';
import { ReservationDetails } from '@app/models/reservations/reservation-details.model';
import { Reservation } from '@app/models/reservations/reservation.model';
import { CartStateService } from '@app/services/cart/cart-state.service';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { ToastService } from '@app/services/toast/toast.service';
import {
  isOrderOfCancelledReservation,
  SUBMITTED_ORDER_STATUSES,
  UNSUBMITTED_ORDER_STATUS,
} from '@app/shared/helpers/cart-orders';
import { toCartOrderMode } from '@app/shared/helpers/group-reservation.helpers';
import { buildPreOrderQueryParams } from '@app/shared/helpers/pre-order-navigation';
import { LoadingSpinnerComponent } from '@app/shared/custom-components/loading-spinner/loading-spinner.component';

import { CancelOrderModalComponent } from './components/cancel-order-modal/cancel-order-modal.component';
import { NotSubmittedOrderComponent } from './components/not-submitted-order/not-submitted-order.component';
import { SubmitOrderModalComponent } from './components/submit-order-modal/submit-order-modal.component';
import { SubmittedOrderComponent } from './components/submitted-order/submitted-order.component';
import { CartOrderView } from './models';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [
    SubmittedOrderComponent,
    NotSubmittedOrderComponent,
    CancelOrderModalComponent,
    SubmitOrderModalComponent,
    LoadingSpinnerComponent,
  ],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CartComponent implements OnInit {
  private readonly preOrdersService = inject(PreOrdersService);
  private readonly reservationService = inject(ReservationService);
  private readonly toast = inject(ToastService);
  private readonly cartState = inject(CartStateService);

  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly closeCartEvent = output<void>();

  readonly orders = signal<Order[]>([]);
  readonly isLoadingOrders = signal(false);

  /** The user's reservations by ID. `null` means not loaded yet, or the load failed. */
  readonly reservationsById = signal<ReadonlyMap<string, Reservation> | null>(null);

  readonly cancelOrderId = signal<string | null>(null);
  readonly submitOrderId = signal<string | null>(null);

  /** Orders without the ones of cancelled reservations. If the reservations did not load, all orders stay. */
  readonly visibleOrders = computed(() =>
    this.orders().filter((order) => !isOrderOfCancelledReservation(order, this.reservationsById()))
  );

  readonly submittedOrders = computed(() =>
    this.visibleOrders().filter((order) => SUBMITTED_ORDER_STATUSES.includes(order.status))
  );

  readonly unsubmittedOrders = computed(() =>
    this.visibleOrders().filter((order) => order.status === UNSUBMITTED_ORDER_STATUS)
  );

  readonly unsubmittedOrderViews = computed(() => this.toOrderViews(this.unsubmittedOrders()));

  readonly submittedOrderViews = computed(() => this.toOrderViews(this.submittedOrders()));

  readonly submittedOrder = computed(() => {
    const orderId = this.submitOrderId();

    if (!orderId) {
      return undefined;
    }

    return this.orders().find((order) => order.orderId === orderId);
  });

  ngOnInit(): void {
    this.loadOrdersAndGuestCounts();
  }

  closeCart(): void {
    this.closeCartEvent.emit();
  }

  openCancelModal(orderId: string): void {
    this.cancelOrderId.set(orderId);
    this.submitOrderId.set(null);
  }

  closeCancelModal(): void {
    this.cancelOrderId.set(null);
  }

  closeSubmitModal(): void {
    this.submitOrderId.set(null);
  }

  editOrderModal(): void {
    const orderId = this.submitOrderId();

    if (!orderId) {
      console.error('No order selected for editing');
      return;
    }

    this.editOrder(orderId);
    this.closeSubmitModal();
  }

  goToReservations(): void {
    this.closeCart();
    void this.router.navigate(['/reservations']);
  }

  cancelOrder(orderId: string): void {
    const order = this.findOrder(orderId);

    if (!order) {
      return;
    }

    this.preOrdersService
      .cancelPreOrderByReservationId(order.reservationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.closeCancelModal();
          this.loadOrders();
        },
        error: (error) => {
          console.error(`Error cancelling order ${orderId}:`, error);
        },
      });
  }

  editOrder(orderId: string): void {
    const order = this.findOrder(orderId);

    // Un-submitting a group order would leave it with no submit path in the cart.
    if (!order || this.orderMode(order) !== 'single') {
      return;
    }

    this.preOrdersService
      .unsubmitPreOrderByReservationId(order.reservationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loadOrders();
        },
        error: (error) => {
          console.error(`Error un-submitting order ${orderId}:`, error);
        },
      });
  }

  submitOrder(orderId: string): void {
    const order = this.findOrder(orderId);

    // A group owner must not submit alone, and an unknown size must not fall back to Submit.
    if (!order || this.orderMode(order) !== 'single') {
      return;
    }

    this.preOrdersService
      .submitPreOrderByReservationId(order.reservationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitOrderId.set(orderId);
          this.loadOrders();
        },
        error: (error) => {
          console.error(`Error submitting order ${orderId}:`, error);
        },
      });
  }

  /** Dishes are already stored by each edit, so Save makes no API call. */
  saveOrder(orderId: string): void {
    if (!this.findOrder(orderId)) {
      return;
    }

    this.toast.success('Your pre-order is saved.', 'Success');
    this.closeCart();
  }

  addDishToOrder(orderId: string): void {
    const order = this.findOrder(orderId);

    if (!order) {
      return;
    }

    const reservation = this.reservationsById()?.get(order.reservationId);
    const queryParams = buildPreOrderQueryParams(
      reservation ?? { id: order.reservationId, locationAddress: order.reservationAddress }
    );

    this.closeCart();
    void this.router.navigate(['/menu'], { queryParams });
  }

  updateDishQuantity(orderId: string, itemId: string, quantity: number): void {
    if (quantity <= 0) {
      return;
    }

    const originalOrder = this.findOrder(orderId);

    if (!originalOrder) {
      return;
    }

    const updatedOrder: Order = {
      ...originalOrder,
      orderItems: originalOrder.orderItems.map((item) =>
        item.dish.id === itemId
          ? {
              ...item,
              quantity,
            }
          : item
      ),
    };

    this.preOrdersService
      .editPreOrderByReservationId(updatedOrder.reservationId, updatedOrder)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loadOrders();
        },
        error: (error) => {
          console.error(`Error updating item ${itemId}:`, error);
        },
      });
  }

  removeDishFromOrder(orderId: string, itemId: string): void {
    const originalOrder = this.findOrder(orderId);

    if (!originalOrder) {
      return;
    }

    const updatedOrder: Order = {
      ...originalOrder,
      orderItems: originalOrder.orderItems.filter((item) => item.dish.id !== itemId),
    };

    this.preOrdersService
      .editPreOrderByReservationId(updatedOrder.reservationId, updatedOrder)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loadOrders();
        },
        error: (error) => {
          console.error(`Error removing item ${itemId}:`, error);
        },
      });
  }

  private findOrder(orderId: string): Order | undefined {
    const order = this.orders().find((item) => item.orderId === orderId);

    if (!order) {
      console.error(`Order with ID ${orderId} not found`);
    }

    return order;
  }

  private toOrderViews(orders: Order[]): CartOrderView[] {
    return orders.map((order) => ({
      order,
      mode: this.orderMode(order),
      details: this.orderDetails(order),
    }));
  }

  private orderMode(order: Order): CartOrderMode {
    return toCartOrderMode(this.reservationsById()?.get(order.reservationId)?.guestCount);
  }

  /** Details come from the reservation. Without it, only the address from the order is known. */
  private orderDetails(order: Order): ReservationDetails {
    const reservation = this.reservationsById()?.get(order.reservationId);

    if (!reservation) {
      return { address: order.reservationAddress };
    }

    return {
      address: reservation.locationAddress,
      startTime: reservation.startTime,
      endTime: reservation.endTime,
      guestCount: reservation.guestCount,
    };
  }

  /** First load: the orders and the guest counts. The spinner waits for both. */
  private loadOrdersAndGuestCounts(): void {
    this.isLoadingOrders.set(true);

    forkJoin({
      orders: this.preOrders$(),
      reservations: this.reservationsById$(),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingOrders.set(false))
      )
      .subscribe(({ orders, reservations }) => {
        this.orders.set(orders);
        this.reservationsById.set(reservations);
        this.cartState.update(orders, reservations);
      });
  }

  /** Later reloads: the reservations do not change while the cart is open. */
  private loadOrders(): void {
    this.isLoadingOrders.set(true);

    this.preOrders$()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoadingOrders.set(false))
      )
      .subscribe((orders) => {
        this.orders.set(orders);
        this.cartState.update(orders, this.reservationsById());
      });
  }

  private preOrders$(): Observable<Order[]> {
    return this.preOrdersService.getPreOrders().pipe(
      catchError((error: unknown) => {
        console.error('Error loading orders:', error);
        return of<Order[]>([]);
      })
    );
  }

  // On error the map stays null: every order gets mode 'unknown'. The interceptor shows the toast.
  private reservationsById$(): Observable<ReadonlyMap<string, Reservation> | null> {
    return this.reservationService.getReservationsForUser$().pipe(
      map(
        (reservations: Reservation[]) =>
          new Map(reservations.map((reservation) => [reservation.id, reservation]))
      ),
      catchError((error: unknown) => {
        console.error('Error loading reservations:', error);
        return of(null);
      })
    );
  }
}
