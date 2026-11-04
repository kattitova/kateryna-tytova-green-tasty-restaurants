import { Injectable, inject, signal } from '@angular/core';
import { catchError, forkJoin, map, of } from 'rxjs';

import { Order } from '@app/models/orders/order.model';
import { Reservation } from '@app/models/reservations/reservation.model';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { cartHasDishes } from '@app/shared/helpers/cart-orders';

/**
 * Tells the header whether the customer's cart has dishes.
 * The cart calls `update` after each load. Other places that change the cart call `refresh`.
 */
@Injectable({ providedIn: 'root' })
export class CartStateService {
  private readonly preOrdersService = inject(PreOrdersService);
  private readonly reservationService = inject(ReservationService);

  private readonly hasDishesState = signal(false);
  readonly hasDishes = this.hasDishesState.asReadonly();

  // Each refresh, update and clear gets a new revision. A response of an older refresh is ignored,
  // so a slow request cannot overwrite a newer state (for example after a logout).
  private revision = 0;

  // The user whose cart state is loaded. A new header on every page must not reload the same user.
  private loadedForUserId: string | null = null;

  /** Loads the cart state one time per user. Changes to the cart call `refresh` or `update` directly. */
  ensureLoaded(userId: string): void {
    if (this.loadedForUserId === userId) {
      return;
    }

    this.loadedForUserId = userId;
    this.refresh();
  }

  /**
   * Loads the cart in the background. Errors do not show a toast: the indicator is not worth one.
   * If the orders fail to load, the indicator keeps its current value.
   */
  refresh(): void {
    const revision = ++this.revision;

    forkJoin({
      orders: this.preOrdersService.getPreOrders(true).pipe(catchError(() => of(null))),
      reservations: this.reservationService.getReservationsForUser$(true).pipe(
        map((reservations) => new Map(reservations.map((reservation) => [reservation.id, reservation]))),
        catchError(() => of(null))
      ),
    }).subscribe(({ orders, reservations }) => {
      if (revision !== this.revision || orders === null) {
        return;
      }

      this.hasDishesState.set(cartHasDishes(orders, reservations));
    });
  }

  update(orders: readonly Order[], reservationsById: ReadonlyMap<string, Reservation> | null): void {
    this.revision++;
    this.hasDishesState.set(cartHasDishes(orders, reservationsById));
  }

  clear(): void {
    this.revision++;
    this.loadedForUserId = null;
    this.hasDishesState.set(false);
  }
}
