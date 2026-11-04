import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Params, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { DishSort } from '@app/models/enums/dish-sort.model';
import { DishType } from '@app/models/enums/dish-type.model';
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
  let dishesService: jasmine.SpyObj<DishesService>;
  let navigateSpy: jasmine.Spy;
  let queryParams$: BehaviorSubject<Params>;

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
    queryParams$ = new BehaviorSubject<Params>(queryParams);
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']);
    dishesService = jasmine.createSpyObj<DishesService>('DishesService', ['getDishes']);
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
        { provide: ActivatedRoute, useValue: { queryParams: queryParams$ } },
        { provide: ToastService, useValue: toast },
        { provide: PreOrdersService, useValue: preOrders },
        { provide: CartStateService, useValue: cartState },
      ],
    })
      .overrideComponent(MenuPageComponent, {
        set: { providers: [{ provide: DishesService, useValue: dishesService }] },
      })
      .compileComponents();

    // Fake router: a navigation re-emits the query params, as the real router does.
    navigateSpy = spyOn(TestBed.inject(Router), 'navigate').and.callFake((_commands, extras) => {
      const next: Params = { ...queryParams$.value };
      Object.entries(extras?.queryParams ?? {}).forEach(([key, value]) => {
        if (value === null || value === undefined) {
          delete next[key];
        } else {
          next[key] = value;
        }
      });
      queryParams$.next(next);
      return Promise.resolve(true);
    });

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
    expect(toast.success).toHaveBeenCalledWith('Green Salad added to your pre-order.', 'Success');
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

  describe('category filter', () => {
    const categoryButtons = (host: HTMLElement): HTMLButtonElement[] =>
      Array.from(host.querySelectorAll<HTMLButtonElement>('.category-buttons button'));

    const buttonByLabel = (host: HTMLElement, label: string): HTMLButtonElement =>
      categoryButtons(host).find((button) => button.textContent?.trim() === label)!;

    const click = (host: HTMLElement, label: string): void => {
      buttonByLabel(host, label).click();
      fixture.detectChanges();
    };

    const activeLabels = (host: HTMLElement): string[] =>
      categoryButtons(host)
        .filter((button) => button.classList.contains('active'))
        .map((button) => button.textContent?.trim() ?? '');

    // red before
    it('shows "All dishes" first, then the three categories, all as type=button', async () => {
      const host = await render({});

      expect(categoryButtons(host).map((button) => button.textContent?.trim())).toEqual([
        'All dishes',
        'Appetizers',
        'Main Courses',
        'Desserts',
      ]);
      expect(categoryButtons(host).every((button) => button.type === 'button')).toBeTrue();
    });

    // red before
    it('marks only "All dishes" active on first load', async () => {
      const host = await render({});

      expect(activeLabels(host)).toEqual(['All dishes']);
    });

    // pin
    it('loads all dishes with the default sort on first load', async () => {
      await render({});

      expect(dishesService.getDishes).toHaveBeenCalledOnceWith(
        undefined,
        DishSort.PopularityDescending
      );
    });

    // pin (passes before: no All dishes button exists yet; guards that it is not active too)
    it('marks the category from the URL active and not "All dishes"', async () => {
      const host = await render({ category: DishType.Dessert });

      expect(activeLabels(host)).toEqual(['Desserts']);
    });

    // red before
    it('shows all dishes again and marks "All dishes" active after a category was selected', async () => {
      const host = await render({});
      click(host, 'Main Courses');
      dishesService.getDishes.calls.reset();

      click(host, 'All dishes');

      expect(dishesService.getDishes).toHaveBeenCalledOnceWith(
        undefined,
        DishSort.PopularityDescending
      );
      expect(activeLabels(host)).toEqual(['All dishes']);
    });

    // red before
    it('clears only the category query param on "All dishes" and keeps the sort', async () => {
      const host = await render({ category: DishType.Dessert, sort: DishSort.PriceAscending });

      click(host, 'All dishes');

      const extras = navigateSpy.calls.mostRecent().args[1];
      expect(extras.queryParams).toEqual({ category: null, sort: DishSort.PriceAscending });
      expect(extras.queryParamsHandling).toBe('merge');
    });

    // pin
    it('selects the clicked category and requests only that category', async () => {
      const host = await render({});
      dishesService.getDishes.calls.reset();

      click(host, 'Main Courses');

      expect(navigateSpy.calls.mostRecent().args[1].queryParams.category).toBe(
        DishType.MainCourse
      );
      expect(dishesService.getDishes).toHaveBeenCalledOnceWith(
        DishType.MainCourse,
        DishSort.PopularityDescending
      );
      expect(activeLabels(host)).toEqual(['Main Courses']);
    });

    // red before
    it('keeps the filter when the active category is clicked again', async () => {
      const host = await render({ category: DishType.Dessert });

      click(host, 'Desserts');

      expect(navigateSpy.calls.mostRecent().args[1].queryParams.category).toBe(DishType.Dessert);
      expect(activeLabels(host)).toEqual(['Desserts']);
      expect(dishesService.getDishes.calls.mostRecent().args[0]).toBe(DishType.Dessert);
    });

    // red before
    it('shows "All dishes" in pre-order mode and keeps the pre-order params on click', async () => {
      const host = await render({
        preOrder: 'true',
        reservationId: 'res-1',
        reservationLabel: 'label',
        category: DishType.Appetizer,
      });

      expect(categoryButtons(host)[0].textContent?.trim()).toBe('All dishes');
      click(host, 'All dishes');

      expect(navigateSpy.calls.mostRecent().args[1].queryParamsHandling).toBe('merge');
      expect(queryParams$.value['reservationId']).toBe('res-1');
      expect(queryParams$.value['preOrder']).toBe('true');
      expect(queryParams$.value['category']).toBeUndefined();
    });
  });
});
