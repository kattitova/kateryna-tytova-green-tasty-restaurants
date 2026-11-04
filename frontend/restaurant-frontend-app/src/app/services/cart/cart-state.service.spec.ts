import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { Order } from '@app/models/orders/order.model';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { ReservationService } from '@app/services/reservation/reservation.service';

import { CartStateService } from './cart-state.service';

describe('CartStateService', () => {
  let service: CartStateService;
  let preOrders: jasmine.SpyObj<PreOrdersService>;
  let reservations: jasmine.SpyObj<ReservationService>;

  const orderWithDish = {
    orderId: 'order-1',
    reservationId: 'res-1',
    status: 0,
    orderItems: [{ quantity: 1, dish: { id: 'dish-1' } }],
  } as Order;

  beforeEach(() => {
    preOrders = jasmine.createSpyObj<PreOrdersService>('PreOrdersService', ['getPreOrders']);
    reservations = jasmine.createSpyObj<ReservationService>('ReservationService', [
      'getReservationsForUser$',
    ]);

    TestBed.configureTestingModule({
      providers: [
        { provide: PreOrdersService, useValue: preOrders },
        { provide: ReservationService, useValue: reservations },
      ],
    });

    service = TestBed.inject(CartStateService);
  });

  it('starts with no dishes', () => {
    expect(service.hasDishes()).toBeFalse();
  });

  it('has dishes after a refresh that finds an active order with dishes', () => {
    preOrders.getPreOrders.and.returnValue(of([orderWithDish]));
    reservations.getReservationsForUser$.and.returnValue(
      of([{ id: 'res-1', status: ReservationStatus.Reserved } as Reservation])
    );

    service.refresh();

    expect(service.hasDishes()).toBeTrue();
  });

  it('asks both requests to skip the error toast', () => {
    preOrders.getPreOrders.and.returnValue(of([]));
    reservations.getReservationsForUser$.and.returnValue(of([]));

    service.refresh();

    expect(preOrders.getPreOrders).toHaveBeenCalledOnceWith(true);
    expect(reservations.getReservationsForUser$).toHaveBeenCalledOnceWith(true);
  });

  it('has no dishes when the order belongs to a cancelled reservation', () => {
    preOrders.getPreOrders.and.returnValue(of([orderWithDish]));
    reservations.getReservationsForUser$.and.returnValue(
      of([{ id: 'res-1', status: ReservationStatus.Cancelled } as Reservation])
    );

    service.refresh();

    expect(service.hasDishes()).toBeFalse();
  });

  it('keeps the indicator when only the reservations fail to load', () => {
    preOrders.getPreOrders.and.returnValue(of([orderWithDish]));
    reservations.getReservationsForUser$.and.returnValue(throwError(() => new Error('down')));

    service.refresh();

    expect(service.hasDishes()).toBeTrue();
  });

  it('keeps the current value when the orders fail to load', () => {
    service.update([orderWithDish], null);
    preOrders.getPreOrders.and.returnValue(throwError(() => new Error('down')));
    reservations.getReservationsForUser$.and.returnValue(of([]));

    service.refresh();

    expect(service.hasDishes()).toBeTrue();
  });

  it('ignores a slow refresh that finishes after clear (logout)', () => {
    const orders$ = new Subject<Order[]>();
    preOrders.getPreOrders.and.returnValue(orders$);
    reservations.getReservationsForUser$.and.returnValue(of([]));

    service.refresh();
    service.clear();
    orders$.next([orderWithDish]);
    orders$.complete();

    expect(service.hasDishes()).toBeFalse();
  });

  it('lets the newest refresh win when two refreshes overlap', () => {
    const first$ = new Subject<Order[]>();
    const second$ = new Subject<Order[]>();
    preOrders.getPreOrders.and.returnValues(first$, second$);
    reservations.getReservationsForUser$.and.returnValue(of([]));

    service.refresh();
    service.refresh();
    second$.next([]);
    second$.complete();
    first$.next([orderWithDish]);
    first$.complete();

    expect(service.hasDishes()).toBeFalse();
  });

  it('loads one time per user and again after clear', () => {
    preOrders.getPreOrders.and.returnValue(of([]));
    reservations.getReservationsForUser$.and.returnValue(of([]));

    service.ensureLoaded('customer-1');
    service.ensureLoaded('customer-1');
    expect(preOrders.getPreOrders).toHaveBeenCalledTimes(1);

    service.ensureLoaded('customer-2');
    expect(preOrders.getPreOrders).toHaveBeenCalledTimes(2);

    service.clear();
    service.ensureLoaded('customer-2');
    expect(preOrders.getPreOrders).toHaveBeenCalledTimes(3);
  });

  it('updates from data that the cart already loaded, and clears', () => {
    service.update([orderWithDish], null);
    expect(service.hasDishes()).toBeTrue();

    service.clear();
    expect(service.hasDishes()).toBeFalse();
  });
});
