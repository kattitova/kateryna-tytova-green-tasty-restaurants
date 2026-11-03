import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';
import { Order } from '@app/models/orders/order.model';

import { SubmittedOrderComponent } from './submitted-order.component';

describe('SubmittedOrderComponent', () => {
  let component: SubmittedOrderComponent;
  let fixture: ComponentFixture<SubmittedOrderComponent>;

  const order = {
    orderId: 'order-1',
    status: 1,
    reservationId: 'res-1',
    reservationAddress: 'Test Address',
    reservationTime: '2026-09-08T12:15:00Z',
    orderItems: [
      {
        quantity: 2,
        state: 1,
        dish: {
          id: 'dish-1',
          price: 10,
        },
      },
      {
        quantity: 1,
        state: 2,
        dish: {
          id: 'dish-2',
          price: 100,
        },
      },
    ],
  } as Order;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubmittedOrderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SubmittedOrderComponent);

    component = fixture.componentInstance;

    fixture.componentRef.setInput('order', order);
    fixture.componentRef.setInput('orderMode', 'single');
    fixture.detectChanges();
  });

  const setMode = (mode: CartOrderMode): void => {
    fixture.componentRef.setInput('orderMode', mode);
    fixture.detectChanges();
  };

  const buttonByText = (text: string): HTMLButtonElement | undefined =>
    Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button')
    ).find((button) => button.textContent?.trim() === text);

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit canceled when cancelOrder is called', () => {
    spyOn(component.canceled, 'emit');

    component.cancelOrder();

    expect(component.canceled.emit).toHaveBeenCalled();
  });

  it('should emit edited when editOrder is called', () => {
    spyOn(component.edited, 'emit');

    component.editOrder();

    expect(component.edited.emit).toHaveBeenCalled();
  });

  it('should exclude rejected items from total price', () => {
    expect(component.totalPrice()).toBe(20);
  });

  describe('edit and cancel by order mode', () => {
    it('shows "Edit Pre-order" in single mode and emits edited on click', () => {
      setMode('single');
      const spy = spyOn(component.edited, 'emit');

      const editButton = buttonByText('Edit Pre-order');
      editButton?.click();

      expect(editButton).toBeDefined();
      expect(spy).toHaveBeenCalledTimes(1);
    });

    it('does not show "Edit Pre-order" in group mode', () => {
      setMode('group');

      expect(buttonByText('Cancel Pre-order')).toBeDefined();
      expect(buttonByText('Edit Pre-order')).toBeUndefined();
    });

    it('does not show "Edit Pre-order" in unknown mode', () => {
      setMode('unknown');

      expect(buttonByText('Cancel Pre-order')).toBeDefined();
      expect(buttonByText('Edit Pre-order')).toBeUndefined();
    });

    it('does not emit edited from editOrder in group mode', () => {
      setMode('group');
      const spy = spyOn(component.edited, 'emit');

      component.editOrder();

      expect(spy).not.toHaveBeenCalled();
    });

    it('does not emit edited from editOrder in unknown mode', () => {
      setMode('unknown');
      const spy = spyOn(component.edited, 'emit');

      component.editOrder();

      expect(spy).not.toHaveBeenCalled();
    });

    (['single', 'group', 'unknown'] as const).forEach((mode) => {
      it(`shows "Cancel Pre-order" in ${mode} mode and emits canceled on click`, () => {
        setMode(mode);
        const spy = spyOn(component.canceled, 'emit');

        buttonByText('Cancel Pre-order')?.click();

        expect(spy).toHaveBeenCalledTimes(1);
      });
    });
  });
});
