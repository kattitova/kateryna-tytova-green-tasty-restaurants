import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AuthenticationService } from '@app/auth/services/authentication.service';
import { ToastService } from '@app/services/toast/toast.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

import { PlaceOrderWaiterComponent } from './place-order-waiter.component';

describe('PlaceOrderWaiterComponent', () => {
  let auth: jasmine.SpyObj<AuthenticationService>;
  let toast: jasmine.SpyObj<ToastService>;

  const configure = async (): Promise<void> => {
    await TestBed.configureTestingModule({
      imports: [PlaceOrderWaiterComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: AuthenticationService, useValue: auth },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();
  };

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthenticationService>('AuthenticationService', [
      'getUser',
      'getUserNameById$',
    ]);
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']);
    auth.getUserNameById$.and.returnValue(of('Anna Beridze'));
  });

  it('shows the waiter and customer names for a signed-in waiter', async () => {
    auth.getUser.and.returnValue({
      id: 'waiter-1',
      firstName: 'Giorgi',
      lastName: 'Kapanadze',
    } as ReturnType<AuthenticationService['getUser']>);
    await configure();

    const fixture: ComponentFixture<PlaceOrderWaiterComponent> =
      TestBed.createComponent(PlaceOrderWaiterComponent);
    fixture.componentInstance.customerId = 'customer-1';
    fixture.detectChanges();

    expect(fixture.componentInstance.waiterName).toBe('Giorgi Kapanadze');
    expect(auth.getUserNameById$).toHaveBeenCalledOnceWith('customer-1');
    expect(fixture.componentInstance.customerName).toBe('Anna Beridze');
  });

  // Pins the CURRENT behaviour: the constructor throws without a user. This is tech debt, not the target.
  // When the component handles a missing user without throwing, change this test.
  it('current behaviour: shows an error toast and throws when nobody is signed in', async () => {
    auth.getUser.and.returnValue(null);
    await configure();

    expect(() => TestBed.createComponent(PlaceOrderWaiterComponent)).toThrowError(
      'User is not authenticated'
    );
    expect(toast.error).toHaveBeenCalledWith(
      'You must be logged in to place an order.',
      'Authentication Error'
    );
  });
});
