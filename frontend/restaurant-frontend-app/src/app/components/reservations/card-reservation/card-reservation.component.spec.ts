import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CardReservationComponent } from './card-reservation.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
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
});
