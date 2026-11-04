import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Params, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { CartStateService } from '@app/services/cart/cart-state.service';
import { DishesService } from '@app/services/dishes/dishes.service';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

import { MenuPageComponent } from './menu-page.component';

describe('MenuPageComponent', () => {
  let fixture: ComponentFixture<MenuPageComponent>;
  let toast: jasmine.SpyObj<ToastService>;
  let preOrders: jasmine.SpyObj<PreOrdersService>;
  let cartState: jasmine.SpyObj<CartStateService>;

  const dish = {
    id: 'dish-1',
    name: 'Green Salad',
    imageUrl: 'https://test.api/salad.jpg',
    price: 12,
    state: 'Available',
    weight: '250 g',
    dishType: 'Appetizer',
  } as DishListResponse;

  const render = async (queryParams: Params): Promise<HTMLElement> => {
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']);
    const dishesService = jasmine.createSpyObj<DishesService>('DishesService', ['getDishes']);
    dishesService.getDishes.and.returnValue(of([dish]));
    preOrders = jasmine.createSpyObj<PreOrdersService>('PreOrdersService', [
      'addDishToPreOrderByReservationId',
    ]);
    cartState = jasmine.createSpyObj<CartStateService>('CartStateService', ['refresh']);

    await TestBed.configureTestingModule({
      imports: [MenuPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: ActivatedRoute, useValue: { queryParams: of(queryParams) } },
        { provide: ToastService, useValue: toast },
        { provide: PreOrdersService, useValue: preOrders },
        { provide: CartStateService, useValue: cartState },
      ],
    })
      .overrideComponent(MenuPageComponent, {
        set: { providers: [{ provide: DishesService, useValue: dishesService }] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(MenuPageComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  const preOrderButtons = (host: HTMLElement): HTMLElement[] =>
    Array.from(host.querySelectorAll<HTMLElement>('app-custom-button')).filter(
      (button) => button.textContent?.trim() === 'Pre-Order'
    );

  it('shows the reservation banner and Pre-Order buttons in pre-order mode', async () => {
    const host = await render({
      preOrder: 'true',
      reservationId: 'res-1',
      reservationLabel: '9 Abashidze Street, Table 3, Sun Nov 08 2026, 16:45 - 18:15',
    });

    expect(host.querySelector('.pre-order-notice')?.textContent).toContain(
      '9 Abashidze Street, Table 3, Sun Nov 08 2026, 16:45 - 18:15'
    );
    expect(preOrderButtons(host).length).toBe(1);
  });

  it('adds the dish to the reservation pre-order and refreshes the header cart indicator', async () => {
    await render({ preOrder: 'true', reservationId: 'res-1', reservationLabel: 'label' });
    preOrders.addDishToPreOrderByReservationId.and.returnValue(of({}));

    fixture.componentInstance.addDishToCart(dish);

    expect(preOrders.addDishToPreOrderByReservationId).toHaveBeenCalledOnceWith('res-1', dish, 1);
    expect(toast.success).toHaveBeenCalledWith('Dish added to cart successfully!', 'Success');
    expect(cartState.refresh).toHaveBeenCalledTimes(1);
  });

  it('shows no banner and no Pre-Order buttons without the pre-order params', async () => {
    const host = await render({});

    expect(host.querySelector('.pre-order-notice')).toBeNull();
    expect(preOrderButtons(host).length).toBe(0);
  });

  it('turns pre-order mode off and shows an error when the reservation ID is missing', async () => {
    const host = await render({ preOrder: 'true' });

    expect(host.querySelector('.pre-order-notice')).toBeNull();
    expect(preOrderButtons(host).length).toBe(0);
    expect(toast.error).toHaveBeenCalledWith(
      'Pre-order is enabled but no reservation ID provided.',
      'Error'
    );
  });
});
