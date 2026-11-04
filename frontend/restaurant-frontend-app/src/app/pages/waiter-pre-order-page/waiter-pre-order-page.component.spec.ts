import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { OrderItem } from '@app/models/dishes/order-item.model';
import { GroupBonusTier } from '@app/models/group-orders/group-bonus.model';
import { Order } from '@app/models/orders/order.model';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { WaiterPreOrderPageComponent } from './waiter-pre-order-page.component';

function item(id: string, price: number, quantity: number, state = 0): OrderItem {
  return {
    dish: { id, name: `Dish ${id}`, price, imageUrl: '', state: 'Available' } as unknown as DishListResponse,
    quantity,
    state,
  };
}

function order(
  items: OrderItem[],
  groupBonusTier: GroupBonusTier | null = null,
  discountPercent = 0,
): Order {
  return {
    orderId: 'order-1',
    reservationId: 'res-1',
    reservationAddress: 'Address',
    reservationTime: 'Nov 10, 2026, 6:00 PM - 8:00 PM',
    status: 1,
    orderItems: items,
    groupBonusTier,
    discountPercent,
  };
}

describe('WaiterPreOrderPageComponent', () => {
  let component: WaiterPreOrderPageComponent;
  let fixture: ComponentFixture<WaiterPreOrderPageComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
      ],
      imports: [WaiterPreOrderPageComponent],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  async function createWith(loaded: Order): Promise<HTMLElement> {
    sessionStorage.setItem(
      'waiterPreOrderData',
      JSON.stringify({
        reservationId: 'res-1',
        createdByName: 'Ann',
        createdByRole: 'Customer',
        tableName: '3',
      }),
    );
    fixture = TestBed.createComponent(WaiterPreOrderPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    httpMock.expectOne('https://test.api/orders/res-1').flush(loaded);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const textOf = (host: HTMLElement, testId: string): string | null => {
    const element = host.querySelector(`[data-testid="${testId}"]`);
    return element ? (element.textContent ?? '').replace(/\s+/g, ' ').trim() : null;
  };

  it('should create', () => {
    fixture = TestBed.createComponent(WaiterPreOrderPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  describe('bill', () => {
    it('shows total before discount, discount line and final total for a 10% group bonus', async () => {
      const host = await createWith(order([item('d1', 34, 4)], 'TenPercentDiscount', 10));

      expect(textOf(host, 'wpo-subtotal')).toBe('Total before discount: $136.00');
      expect(textOf(host, 'wpo-discount')).toBe('Group discount −10%: −$13.60');
      expect(textOf(host, 'wpo-total')).toBe('Total: $122.40');
      expect(textOf(host, 'wpo-free-dessert')).toBeNull();
    });

    it('shows only the total when the order has no group bonus', async () => {
      const host = await createWith(order([item('d1', 34, 4)]));

      expect(textOf(host, 'wpo-total')).toBe('Total: $136.00');
      expect(textOf(host, 'wpo-subtotal')).toBeNull();
      expect(textOf(host, 'wpo-discount')).toBeNull();
      expect(textOf(host, 'wpo-free-dessert')).toBeNull();
    });

    it('shows the free dessert line and no discount for a free dessert bonus', async () => {
      const host = await createWith(order([item('d1', 34, 4)], 'FreeDessert', 0));

      expect(textOf(host, 'wpo-free-dessert')).toBe('Group bonus: free dessert');
      expect(textOf(host, 'wpo-total')).toBe('Total: $136.00');
      expect(textOf(host, 'wpo-subtotal')).toBeNull();
      expect(textOf(host, 'wpo-discount')).toBeNull();
    });

    it('excludes canceled items from the total', async () => {
      const host = await createWith(order([item('d1', 10, 2, 0), item('d2', 100, 1, 2)]));

      expect(textOf(host, 'wpo-total')).toBe('Total: $20.00');
    });

    it('updates the total after the waiter cancels a dish', async () => {
      const host = await createWith(
        order([item('d1', 34, 4), item('d2', 10, 1)], 'TenPercentDiscount', 10),
      );
      expect(textOf(host, 'wpo-total')).toBe('Total: $131.40');
      spyOn(TestBed.inject(PreOrdersService), 'rejectDishInOrder').and.returnValue(of({}));

      component.rejectDish('d1');
      fixture.detectChanges();

      expect(textOf(host, 'wpo-subtotal')).toBe('Total before discount: $10.00');
      expect(textOf(host, 'wpo-discount')).toBe('Group discount −10%: −$1.00');
      expect(textOf(host, 'wpo-total')).toBe('Total: $9.00');
    });

    it('keeps the total after the waiter confirms a dish', async () => {
      const host = await createWith(
        order([item('d1', 34, 4), item('d2', 10, 1)], 'TenPercentDiscount', 10),
      );
      spyOn(TestBed.inject(PreOrdersService), 'confirmDishInOrder').and.returnValue(of({}));

      component.confirmDish('d2');
      fixture.detectChanges();

      expect(textOf(host, 'wpo-total')).toBe('Total: $131.40');
    });

    it('hides the bill when the order has no items', async () => {
      const host = await createWith(order([]));

      expect(host.querySelector('[data-testid="wpo-bill"]')).toBeNull();
    });
  });

  describe('Confirm and Cancel buttons by item state (pinning tests)', () => {
    const buttonOf = (card: Element, selector: string): HTMLButtonElement | null =>
      card.querySelector<HTMLButtonElement>(`${selector} button`);

    it('disables Confirm for a confirmed item and Cancel for a canceled item after load', async () => {
      const host = await createWith(order([item('d1', 10, 1, 1), item('d2', 10, 1, 2)]));

      const cards = host.querySelectorAll('.pre-order-dish');
      expect(buttonOf(cards[0], 'app-custom-button.btn-confirm')?.disabled).toBeTrue();
      expect(buttonOf(cards[0], 'app-custom-button.btn-cancel')?.disabled).toBeFalse();
      expect(buttonOf(cards[1], 'app-custom-button.btn-cancel')?.disabled).toBeTrue();
      expect(buttonOf(cards[1], 'app-custom-button.btn-confirm')?.disabled).toBeFalse();
    });

    it('enables both buttons for a pending item after load', async () => {
      const host = await createWith(order([item('d1', 10, 1, 0)]));

      const card = host.querySelector('.pre-order-dish') as Element;
      expect(buttonOf(card, 'app-custom-button.btn-confirm')?.disabled).toBeFalse();
      expect(buttonOf(card, 'app-custom-button.btn-cancel')?.disabled).toBeFalse();
    });
  });
});
