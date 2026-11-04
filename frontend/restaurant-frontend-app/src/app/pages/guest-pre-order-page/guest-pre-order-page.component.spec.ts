import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

import { GuestPreOrderPageComponent } from './guest-pre-order-page.component';
import { GuestCartItem, GuestPreorderStore } from './state/guest-preorder.store';

describe('GuestPreOrderPageComponent', () => {
  let fixture: ComponentFixture<GuestPreOrderPageComponent>;
  let store: GuestPreorderStore;

  const salad: GuestCartItem = { dishId: 'dish-1', title: 'Green Salad', price: 12, qty: 1 };

  const cartButton = (): HTMLButtonElement | null =>
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.cart-btn');

  const render = (items: GuestCartItem[]): void => {
    store.cartItems.set(items);
    fixture.detectChanges();
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuestPreOrderPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        // No token: the store does not load the invite, so the test sends no HTTP requests.
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({})) } },
      ],
    }).compileComponents();

    store = TestBed.inject(GuestPreorderStore);
    fixture = TestBed.createComponent(GuestPreOrderPageComponent);
  });

  it('shows no cart indicator when the guest cart is empty', () => {
    render([]);

    expect(cartButton()?.querySelector('.cart-indicator')).toBeNull();
    expect(cartButton()?.getAttribute('aria-label')).toBe('Open cart');
  });

  it('shows the cart indicator when the guest adds a dish', () => {
    render([salad]);

    expect(cartButton()?.querySelector('.cart-indicator')).not.toBeNull();
    expect(cartButton()?.getAttribute('aria-label')).toBe('Open cart, has dishes');
  });

  it('hides the cart indicator again when the last dish has quantity 0', () => {
    render([{ ...salad, qty: 0 }]);

    expect(cartButton()?.querySelector('.cart-indicator')).toBeNull();
    expect(cartButton()?.getAttribute('aria-label')).toBe('Open cart');
  });
});
