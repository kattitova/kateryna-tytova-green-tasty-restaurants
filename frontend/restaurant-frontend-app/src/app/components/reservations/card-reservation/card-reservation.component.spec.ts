import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CardReservationComponent } from './card-reservation.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { Roles } from '@app/shared/constants/roles';
import { GroupBonusTier } from '@app/models/group-orders/group-bonus.model';
import { Order } from '@app/models/orders/order.model';
import { ReservationCustomer, ReservationStatusCustomer } from '@app/models/reservations/reservation.model';

const RESERVATION: ReservationCustomer = {
  id: 'res-1',
  tableId: 'table-1',
  startTime: new Date('2026-10-21T16:00:00Z'),
  endTime: new Date('2026-10-21T17:30:00Z'),
  guestCount: 2,
  status: ReservationStatusCustomer.Reserved,
  restaurantName: 'Green & Tasty',
  locationAddress: 'Rustaveli Avenue 48',
  waiterId: 'waiter-1',
};

describe('CardReservationComponent', () => {
  let component: CardReservationComponent;
  let fixture: ComponentFixture<CardReservationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }, provideRouter([])],
      imports: [CardReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CardReservationComponent);
    component = fixture.componentInstance;
    // r — обов'язковий input: без нього компонент падає з NG0950
    fixture.componentRef.setInput('r', RESERVATION);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  function setReservation(overrides: Partial<ReservationCustomer>): HTMLElement {
    const reservation = { ...RESERVATION, ...overrides };
    fixture.componentRef.setInput('r', reservation);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function findButton(root: HTMLElement, text: string): HTMLButtonElement | undefined {
    return Array.from(root.querySelectorAll('button')).find((b) => b.textContent?.trim() === text);
  }

  it('shows "Group Pre-Order" on a reserved group card and emits groupPreOrder on click', () => {
    const root = setReservation({ guestCount: 4 });
    const emitted: ReservationCustomer[] = [];
    component.groupPreOrder.subscribe((r) => emitted.push(r));

    const button = findButton(root, 'Group Pre-Order');
    expect(button).toBeDefined();
    button!.click();

    expect(emitted).toEqual([{ ...RESERVATION, guestCount: 4 }]);
  });

  it('shows no "Group Pre-Order" button for a single guest', () => {
    const root = setReservation({ guestCount: 1 });

    expect(findButton(root, 'Group Pre-Order')).toBeUndefined();
  });

  it('shows no "Group Pre-Order" button when the reservation is in progress', () => {
    const root = setReservation({ guestCount: 4, status: ReservationStatusCustomer.InProgress });

    expect(findButton(root, 'Group Pre-Order')).toBeUndefined();
  });

  it('emits cancelReservation with the id from the "Cancel reservation" icon button', () => {
    const root = setReservation({ guestCount: 4 });
    const emitted: string[] = [];
    component.cancelReservation.subscribe((id) => emitted.push(id));

    const button = root.querySelector<HTMLButtonElement>('button[aria-label="Cancel reservation"]');
    expect(button).not.toBeNull();
    button!.click();

    expect(emitted).toEqual(['res-1']);
  });

  it('renders reservation time in 24-hour format on the card', () => {
    const root = setReservation({});
    const timeLine = root
      .querySelector('img[src="assets/svg/clock-green.svg"]')
      ?.parentElement?.textContent;

    expect(timeLine).toBeTruthy();
    expect(timeLine).toMatch(/\b\d{2}:\d{2}\s*–\s*\d{2}:\d{2}\b/);
    expect(timeLine).not.toMatch(/\bAM\b|\bPM\b/i);
  });

  describe('group bonus badge', () => {
    function preOrderWith(
      groupBonusTier: GroupBonusTier | null | undefined,
      discountPercent: number | undefined,
    ): Order {
      return {
        orderId: 'order-1',
        reservationId: 'res-1',
        reservationAddress: 'Address',
        reservationTime: 'Time',
        status: 1,
        orderItems: [],
        groupBonusTier,
        discountPercent,
      };
    }

    function badgeText(preOrder: Order, role: string = Roles.WAITER): string | null {
      fixture.componentRef.setInput('role', role);
      const root = setReservation({ guestCount: 4, preOrder });
      const badge = root.querySelector('[data-testid="group-bonus-badge"]');
      return badge ? (badge.textContent ?? '').replace(/\s+/g, ' ').trim() : null;
    }

    it('shows "Group bonus: −10%" on the waiter card when the pre-order has a 10% bonus', () => {
      expect(badgeText(preOrderWith('TenPercentDiscount', 10))).toBe('Group bonus: −10%');
    });

    it('shows "Free dessert" on the waiter card when the pre-order has a free dessert bonus', () => {
      expect(badgeText(preOrderWith('FreeDessert', 0))).toBe('Free dessert');
    });

    it('shows no group bonus badge when the pre-order has no bonus', () => {
      expect(badgeText(preOrderWith(null, 0))).toBeNull();
      expect(badgeText(preOrderWith(undefined, undefined))).toBeNull();
    });

    it('shows no group bonus badge when the tier is a discount but discountPercent is 0', () => {
      expect(badgeText(preOrderWith('TenPercentDiscount', 0))).toBeNull();
    });

    it('shows no group bonus badge on the customer card', () => {
      expect(badgeText(preOrderWith('TenPercentDiscount', 10), Roles.CUSTOMER)).toBeNull();
    });
  });
});
