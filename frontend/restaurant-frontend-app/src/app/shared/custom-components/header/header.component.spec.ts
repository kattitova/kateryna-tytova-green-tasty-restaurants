import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject, isObservable, map, Observable, of } from 'rxjs';

import { UserModel } from '@app/auth/models/user.model';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { CartStateService } from '@app/services/cart/cart-state.service';
import { NotificationsService } from '@app/services/notifications/notifications.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { Roles } from '@app/shared/constants/roles';

import { HeaderComponent } from './header.component';

describe('HeaderComponent', () => {
  let fixture: ComponentFixture<HeaderComponent>;
  let notificationsMock: jasmine.SpyObj<NotificationsService>;
  let cartState: { hasDishes: ReturnType<typeof signal<boolean>>; ensureLoaded: jasmine.Spy; clear: jasmine.Spy };

  const render = async (user: UserModel | null | Observable<UserModel | null>): Promise<HTMLElement> => {
    const user$ = isObservable(user) ? user : of(user);
    const authMock = jasmine.createSpyObj<AuthenticationService>('AuthenticationService', ['signOut'], {
      user$,
      isAuthorised$: user$.pipe(map((value) => value !== null)),
    });

    notificationsMock = jasmine.createSpyObj<NotificationsService>('NotificationsService', [
      'loadNotifications',
      'markAsRead',
    ]);
    notificationsMock.loadNotifications.and.returnValue(of([]));
    notificationsMock.markAsRead.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      providers: [
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: AuthenticationService, useValue: authMock },
        { provide: NotificationsService, useValue: notificationsMock },
        { provide: CartStateService, useValue: cartState },
        provideRouter([]),
      ],
      imports: [HeaderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  const customer = { id: 'customer-1', firstName: 'Anna', role: Roles.CUSTOMER } as UserModel;

  beforeEach(() => {
    cartState = {
      hasDishes: signal(false),
      ensureLoaded: jasmine.createSpy('ensureLoaded'),
      clear: jasmine.createSpy('clear'),
    };
  });

  it('should create for an anonymous visitor and clears the cart indicator', async () => {
    const host = await render(null);

    expect(host).toBeTruthy();
    expect(cartState.clear).toHaveBeenCalled();
    expect(cartState.ensureLoaded).not.toHaveBeenCalled();
  });

  it('loads the cart state for a customer', async () => {
    await render(customer);

    expect(cartState.ensureLoaded).toHaveBeenCalledOnceWith('customer-1');
    // A clear() here would reset "loaded once per user" and reload the cart on every page.
    expect(cartState.clear).not.toHaveBeenCalled();
  });

  it('shows no cart indicator when the cart has no dishes', async () => {
    const host = await render(customer);

    expect(host.querySelector('.cart-indicator')).toBeNull();
  });

  it('shows the cart indicator on the cart icons when the cart has dishes', async () => {
    cartState.hasDishes.set(true);
    const host = await render(customer);

    // Two cart buttons: the mobile menu one and the desktop one.
    expect(host.querySelectorAll('.cart-indicator').length).toBe(2);
    expect(host.querySelectorAll('[aria-label="Cart, has dishes"]').length).toBe(2);
  });

  it('clears the cart indicator when the customer signs out', async () => {
    const user$ = new BehaviorSubject<UserModel | null>(customer);
    await render(user$);
    expect(cartState.ensureLoaded).toHaveBeenCalledTimes(1);

    user$.next(null);
    fixture.detectChanges();

    expect(cartState.clear).toHaveBeenCalled();
  });
});
