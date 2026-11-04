import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';

import { CartOrderMode } from '@app/models/cart/cart-order-mode.model';
import { Order } from '@app/models/orders/order.model';

import { NotSubmittedOrderComponent } from './not-submitted-order.component';

describe('NotSubmittedOrderComponent', () => {
  let component: NotSubmittedOrderComponent;
  let fixture: ComponentFixture<NotSubmittedOrderComponent>;

  const orderWithQuantity = (quantity: number): Order =>
    ({
      orderId: 'order-1',
      status: 0,
      reservationId: 'res-1',
      reservationAddress: 'Test address',
      reservationTime: '2026-09-07T12:15:00Z',
      orderItems: [
        {
          quantity,
          dish: {
            id: 'dish-1',
            name: 'Caesar Salad',
            price: 10,
          },
        },
      ],
    }) as Order;

  const setMode = (mode: CartOrderMode): void => {
    fixture.componentRef.setInput('orderMode', mode);
    fixture.detectChanges();
  };

  const setOrder = (order: Order): void => {
    fixture.componentRef.setInput('order', order);
    fixture.detectChanges();
  };

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

  const cta = (): HTMLButtonElement | null =>
    host().querySelector<HTMLButtonElement>('.cart-cta');

  const buttonByLabel = (label: string): HTMLButtonElement | null =>
    host().querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotSubmittedOrderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(NotSubmittedOrderComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('order', orderWithQuantity(2));
    fixture.componentRef.setInput('orderMode', 'single');
    fixture.componentRef.setInput('details', { address: 'Test address' });
    fixture.detectChanges();
  });

  describe('single mode', () => {
    it('shows an enabled "Submit Pre-Order" button', () => {
      setMode('single');

      expect(cta()?.textContent?.trim()).toBe('Submit Pre-Order');
      expect(cta()?.disabled).toBeFalse();
    });

    it('emits submitted with the order ID and not saved when the button is clicked', () => {
      setMode('single');
      const submittedSpy = spyOn(component.submitted, 'emit');
      const savedSpy = spyOn(component.saved, 'emit');

      cta()?.click();

      expect(submittedSpy).toHaveBeenCalledOnceWith('order-1');
      expect(savedSpy).not.toHaveBeenCalled();
    });

    it('does not show the unavailable message', () => {
      setMode('single');

      expect(cta()).not.toBeNull();
      expect(host().querySelector('.cta-unavailable')).toBeNull();
    });
  });

  describe('group mode', () => {
    it('shows an enabled "Save Pre-Order" button', () => {
      setMode('group');

      expect(cta()?.textContent?.trim()).toBe('Save Pre-Order');
      expect(cta()?.disabled).toBeFalse();
    });

    it('emits saved with the order ID and not submitted when the button is clicked', () => {
      setMode('group');
      const submittedSpy = spyOn(component.submitted, 'emit');
      const savedSpy = spyOn(component.saved, 'emit');

      cta()?.click();

      expect(savedSpy).toHaveBeenCalledOnceWith('order-1');
      expect(submittedSpy).not.toHaveBeenCalled();
    });

    it('does not show the unavailable message', () => {
      setMode('group');

      expect(cta()).not.toBeNull();
      expect(host().querySelector('.cta-unavailable')).toBeNull();
    });
  });

  describe('unknown mode', () => {
    it('disables the button', () => {
      setMode('unknown');

      expect(cta()).not.toBeNull();
      expect(cta()?.disabled).toBeTrue();
      expect(cta()?.textContent?.trim()).toBe('Submit Pre-Order');
    });

    it('shows the unavailable message as a status', () => {
      setMode('unknown');

      const message = host().querySelector('.cta-unavailable');

      expect(message?.getAttribute('role')).toBe('status');
      expect(message?.textContent?.trim()).toBe(
        'We could not load your reservation details. Reopen the cart to try again.',
      );
    });

    it('emits neither submitted nor saved from onCtaClick', () => {
      setMode('unknown');
      const submittedSpy = spyOn(component.submitted, 'emit');
      const savedSpy = spyOn(component.saved, 'emit');

      component.onCtaClick();

      expect(submittedSpy).not.toHaveBeenCalled();
      expect(savedSpy).not.toHaveBeenCalled();
    });
  });

  describe('grand total', () => {
    it('shows the "Grand Total" label', () => {
      expect(host().textContent).toContain('Grand Total');
    });

    it('shows the total for 2 x 10 as $20.00', () => {
      const value = host().querySelector('.grand-total-value');

      expect(value?.textContent?.trim()).toBe('$20.00');
    });
  });

  describe('dish controls', () => {
    it('disables "Decrease quantity" at quantity 1', () => {
      setOrder(orderWithQuantity(1));

      expect(buttonByLabel('Decrease quantity')?.disabled).toBeTrue();
    });

    it('enables "Decrease quantity" at quantity 2', () => {
      expect(buttonByLabel('Decrease quantity')?.disabled).toBeFalse();
    });

    it('emits updateQuantity with quantity 3 when "Increase quantity" is clicked at quantity 2', () => {
      const spy = spyOn(component.updateQuantity, 'emit');

      buttonByLabel('Increase quantity')?.click();

      expect(spy).toHaveBeenCalledOnceWith({ itemId: 'dish-1', quantity: 3 });
    });

    it('emits removeDish with the dish ID when "Remove" is clicked', () => {
      const spy = spyOn(component.removeDish, 'emit');

      buttonByLabel('Remove Caesar Salad')?.click();

      expect(spy).toHaveBeenCalledOnceWith('dish-1');
    });

    it('emits addDish when the "Add dish" button is clicked', () => {
      const spy = spyOn(component.addDish, 'emit');
      const addButton = Array.from(host().querySelectorAll('button')).find(
        (button) => button.textContent?.trim() === 'Add dish',
      );

      addButton?.click();

      expect(spy).toHaveBeenCalledTimes(1);
    });
  });

  it('shows the reservation details block and never the order ID', () => {
    fixture.componentRef.setInput('details', { address: 'Test address', guestCount: 2 });
    fixture.detectChanges();

    const details = host().querySelector('app-reservation-details');

    expect(details?.textContent).toContain('Test address');
    expect(details?.textContent).toContain('2 Guests');
    expect(host().textContent).not.toContain('order-1');
  });
});
