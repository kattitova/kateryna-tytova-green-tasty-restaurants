import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';

import { Order } from '@app/models/orders/order.model';
import { Reservation } from '@app/models/reservations/reservation.model';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { ToastService } from '@app/services/toast/toast.service';

import { CartComponent } from './cart.component';

describe('CartComponent', () => {
  let component: CartComponent;
  let fixture: ComponentFixture<CartComponent>;

  let preOrdersServiceMock: jasmine.SpyObj<PreOrdersService>;
  let routerMock: jasmine.SpyObj<Router>;
  let reservationServiceMock: jasmine.SpyObj<ReservationService>;
  let toastMock: jasmine.SpyObj<ToastService>;

  const unsubmittedOrder = {
    orderId: 'order-1',
    status: 0,
    reservationId: 'res-1',
    reservationAddress: 'Test address',
    reservationTime: '2026-09-07T12:15:00Z',
    orderItems: [
      {
        quantity: 2,
        dish: {
          id: 'dish-1',
          name: 'Caesar Salad',
          price: 10,
        },
      },
    ],
  } as Order;

  const reservation = (id: string, guestCount: number): Reservation =>
    ({ id, guestCount }) as Reservation;

  const createComponent = (): void => {
    fixture = TestBed.createComponent(CartComponent);
    component = fixture.componentInstance;

    fixture.detectChanges();
    fixture.detectChanges();
  };

  const renderWith = (orders: Order[], reservations$: Observable<Reservation[]>): void => {
    preOrdersServiceMock.getPreOrders.and.returnValue(of(orders));
    reservationServiceMock.getReservationsForUser$.and.returnValue(reservations$);
    createComponent();
  };

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

  const cta = (): HTMLButtonElement | null => host().querySelector<HTMLButtonElement>('.cart-cta');

  beforeEach(async () => {
    preOrdersServiceMock = jasmine.createSpyObj<PreOrdersService>('PreOrdersService', [
      'getPreOrders',
      'cancelPreOrderByReservationId',
      'unsubmitPreOrderByReservationId',
      'submitPreOrderByReservationId',
      'editPreOrderByReservationId',
    ]);

    routerMock = jasmine.createSpyObj<Router>('Router', ['navigate']);

    reservationServiceMock = jasmine.createSpyObj<ReservationService>('ReservationService', [
      'getReservationsForUser$',
    ]);

    toastMock = jasmine.createSpyObj<ToastService>('ToastService', ['success']);

    preOrdersServiceMock.getPreOrders.and.returnValue(of([]));
    reservationServiceMock.getReservationsForUser$.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [CartComponent],
      providers: [
        {
          provide: PreOrdersService,
          useValue: preOrdersServiceMock,
        },
        {
          provide: Router,
          useValue: routerMock,
        },
        {
          provide: ReservationService,
          useValue: reservationServiceMock,
        },
        {
          provide: ToastService,
          useValue: toastMock,
        },
      ],
    }).compileComponents();
  });

  describe('with an empty cart', () => {
    beforeEach(() => {
      createComponent();
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should emit closeCartEvent when closeCart is called', () => {
      spyOn(component.closeCartEvent, 'emit');

      component.closeCart();

      expect(component.closeCartEvent.emit).toHaveBeenCalled();
    });

    it('should return submitted orders with statuses 1 and 3', () => {
      component.orders.set([
        {
          orderId: 'order-1',
          status: 1,
        },
        {
          orderId: 'order-2',
          status: 2,
        },
        {
          orderId: 'order-3',
          status: 3,
        },
        {
          orderId: 'order-4',
          status: 0,
        },
      ] as Order[]);

      expect(component.submittedOrders().map((order) => order.status)).toEqual([1, 3]);
    });

    it('should return unsubmitted orders with status 0', () => {
      component.orders.set([
        {
          orderId: 'order-1',
          status: 0,
        },
        {
          orderId: 'order-2',
          status: 1,
        },
        {
          orderId: 'order-3',
          status: 3,
        },
      ] as Order[]);

      expect(component.unsubmittedOrders().map((order) => order.status)).toEqual([0]);
    });

    it('should navigate to reservations when goToReservations is called', () => {
      component.goToReservations();

      expect(routerMock.navigate).toHaveBeenCalledWith(['/reservations']);
    });
  });

  describe('order mode from the reservation guest count', () => {
    it('renders an enabled "Submit Pre-Order" button for a reservation with 1 guest', () => {
      renderWith([unsubmittedOrder], of([reservation('res-2', 4), reservation('res-1', 1)]));

      expect(cta()?.textContent?.trim()).toBe('Submit Pre-Order');
      expect(cta()?.disabled).toBeFalse();
    });

    it('renders "Save Pre-Order" for a reservation with 4 guests', () => {
      renderWith([unsubmittedOrder], of([reservation('res-2', 1), reservation('res-1', 4)]));

      expect(cta()?.textContent?.trim()).toBe('Save Pre-Order');
    });

    it('renders a disabled button and the unavailable message when the reservation is not in the list', () => {
      renderWith([unsubmittedOrder], of([reservation('res-2', 1)]));

      expect(cta()).not.toBeNull();
      expect(cta()?.disabled).toBeTrue();
      expect(host().querySelector('.cta-unavailable')).not.toBeNull();
    });

    it('still renders the order with a disabled button and the unavailable message when the reservations load fails', () => {
      renderWith(
        [unsubmittedOrder],
        throwError(() => new Error('reservations failed'))
      );

      expect(cta()).not.toBeNull();
      expect(cta()?.disabled).toBeTrue();
      expect(host().querySelector('.cta-unavailable')).not.toBeNull();
    });

    it('shows the "My Pre-Order" heading when there is an unsubmitted order', () => {
      renderWith([unsubmittedOrder], of([reservation('res-1', 1)]));

      const headings = Array.from(host().querySelectorAll('h4')).map((heading) =>
        heading.textContent?.trim()
      );

      expect(headings).toContain('My Pre-Order');
    });
  });

  describe('loading', () => {
    it('shows the spinner and no button while the reservations are pending', () => {
      renderWith([unsubmittedOrder], new Subject<Reservation[]>());

      expect(host().querySelector('app-loading-spinner')).not.toBeNull();
      expect(cta()).toBeNull();
    });

    it('hides the spinner and shows the button after the reservations emit and complete', () => {
      const reservations$ = new Subject<Reservation[]>();
      renderWith([unsubmittedOrder], reservations$);

      reservations$.next([reservation('res-1', 1)]);
      reservations$.complete();
      fixture.detectChanges();

      expect(host().querySelector('app-loading-spinner')).toBeNull();
      expect(cta()).not.toBeNull();
    });

    it('loads the reservations one time and the orders again after a quantity update', () => {
      preOrdersServiceMock.editPreOrderByReservationId.and.returnValue(of({}));
      renderWith([unsubmittedOrder], of([reservation('res-1', 1)]));

      component.updateDishQuantity('order-1', 'dish-1', 3);

      expect(preOrdersServiceMock.getPreOrders).toHaveBeenCalledTimes(2);
      expect(reservationServiceMock.getReservationsForUser$).toHaveBeenCalledTimes(1);
    });
  });

  describe('submit and save', () => {
    beforeEach(() => {
      preOrdersServiceMock.submitPreOrderByReservationId.and.returnValue(of({}));
    });

    it('submits by reservation ID when the Submit button is clicked for 1 guest', () => {
      renderWith([unsubmittedOrder], of([reservation('res-1', 1)]));

      cta()?.click();

      expect(preOrdersServiceMock.submitPreOrderByReservationId).toHaveBeenCalledOnceWith('res-1');
    });

    it('shows a toast, closes the cart and calls no order API when the Save button is clicked for 4 guests', () => {
      renderWith([unsubmittedOrder], of([reservation('res-1', 4)]));
      const closeSpy = spyOn(component.closeCartEvent, 'emit');

      cta()?.click();

      expect(toastMock.success).toHaveBeenCalledOnceWith('Your pre-order is saved.');
      expect(closeSpy).toHaveBeenCalledTimes(1);
      expect(preOrdersServiceMock.submitPreOrderByReservationId).not.toHaveBeenCalled();
      expect(preOrdersServiceMock.editPreOrderByReservationId).not.toHaveBeenCalled();
    });

    it('does not submit a group order when submitOrder is called directly', () => {
      renderWith([unsubmittedOrder], of([reservation('res-1', 4)]));

      component.submitOrder('order-1');

      expect(preOrdersServiceMock.submitPreOrderByReservationId).not.toHaveBeenCalled();
    });

    it('does not submit when the reservations load failed', () => {
      renderWith(
        [unsubmittedOrder],
        throwError(() => new Error('reservations failed'))
      );

      component.submitOrder('order-1');

      expect(preOrdersServiceMock.submitPreOrderByReservationId).not.toHaveBeenCalled();
    });

    it('does not submit when the reservation is not in the list', () => {
      renderWith([unsubmittedOrder], of([reservation('res-2', 1)]));

      component.submitOrder('order-1');

      expect(preOrdersServiceMock.submitPreOrderByReservationId).not.toHaveBeenCalled();
    });
  });
});
