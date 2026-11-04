import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';

import { AuthenticationService } from '@app/auth/services/authentication.service';
import { Reservation } from '@app/models/reservations/reservation.model';
import { CartStateService } from '@app/services/cart/cart-state.service';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { ReservationService } from '@app/services/reservation/reservation.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { Roles } from '@app/shared/constants/roles';
import { HeaderComponent } from '@app/shared/custom-components/header/header.component';

import { ReservationDashboardComponent } from './reservation-dashboard.component';

@Component({ selector: 'app-header', template: '' })
class HeaderStubComponent {}

describe('ReservationDashboardComponent', () => {
  let fixture: ComponentFixture<ReservationDashboardComponent>;
  let reservations$: Subject<Reservation[]>;
  let reservationService: jasmine.SpyObj<ReservationService>;
  let cartState: jasmine.SpyObj<CartStateService>;

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

  beforeEach(async () => {
    reservations$ = new Subject<Reservation[]>();

    const authenticationService = jasmine.createSpyObj<AuthenticationService>(
      'AuthenticationService',
      ['getToken', 'getUser']
    );
    authenticationService.getToken.and.returnValue('token');
    authenticationService.getUser.and.returnValue({
      id: 'customer-1',
      firstName: 'Anna',
      lastName: 'Beridze',
      role: Roles.CUSTOMER,
    } as ReturnType<AuthenticationService['getUser']>);

    reservationService = jasmine.createSpyObj<ReservationService>('ReservationService', [
      'getReservationsForUser$',
      'cancelReservation$',
    ]);
    reservationService.getReservationsForUser$.and.returnValue(reservations$);
    cartState = jasmine.createSpyObj<CartStateService>('CartStateService', ['refresh']);

    const preOrdersService = jasmine.createSpyObj<PreOrdersService>('PreOrdersService', [
      'getPreOrders',
    ]);
    preOrdersService.getPreOrders.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [ReservationDashboardComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: AuthenticationService, useValue: authenticationService },
        { provide: ReservationService, useValue: reservationService },
        { provide: PreOrdersService, useValue: preOrdersService },
        { provide: CartStateService, useValue: cartState },
      ],
    })
      // The header is not under test. It needs more of AuthenticationService than this mock has.
      .overrideComponent(ReservationDashboardComponent, {
        remove: { imports: [HeaderComponent] },
        add: { imports: [HeaderStubComponent] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ReservationDashboardComponent);
    fixture.detectChanges();
  });

  it('refreshes the header cart indicator after a reservation is cancelled', () => {
    reservationService.cancelReservation$.and.returnValue(
      of({}) as ReturnType<ReservationService['cancelReservation$']>
    );

    fixture.componentInstance.cancelReservation('res-1');

    expect(cartState.refresh).toHaveBeenCalledTimes(1);
  });

  it('shows the spinner while the reservations load', () => {
    expect(host().querySelector('.reservations-loading app-loading-spinner')).not.toBeNull();
    expect(host().querySelector('.reservation-list')).toBeNull();
  });

  it('hides the spinner and shows the empty state after the reservations load', () => {
    reservations$.next([]);
    reservations$.complete();
    fixture.detectChanges();

    expect(host().querySelector('app-loading-spinner')).toBeNull();
    expect(host().textContent).toContain("It looks like you don't have any reservations.");
  });
});
