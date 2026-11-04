import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';

import { ReservationConfirmedComponent } from './reservation-confirmed.component';

describe('ReservationConfirmedComponent', () => {
  let fixture: ComponentFixture<ReservationConfirmedComponent>;

  const reservation: Reservation = {
    id: 'res-1',
    tableId: '422f8285-b773-46b5-a5bc-167ca91cf6b2',
    customerId: 'customer-1',
    startTime: new Date(2026, 10, 8, 16, 45),
    endTime: new Date(2026, 10, 8, 18, 15),
    guestCount: 2,
    status: ReservationStatus.Reserved,
    locationAddress: '9 Abashidze Street',
    tableCapacity: 4,
    waiterId: 'waiter-1',
  };

  const render = (value: Reservation): string => {
    fixture.componentRef.setInput('reservation', value);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReservationConfirmedComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationConfirmedComponent);
  });

  it('shows the table number and not the table ID', () => {
    const text = render({ ...reservation, tableNumber: 3 });

    expect(text).toContain('Table 3');
    expect(text).not.toContain(reservation.tableId);
  });

  it('omits the table and never shows the table ID when the table number is unknown', () => {
    const text = render({ ...reservation, tableNumber: null });

    expect(text).not.toContain('Table');
    expect(text).not.toContain(reservation.tableId);
    expect(text).toContain('has been successfully made.');
  });
});
