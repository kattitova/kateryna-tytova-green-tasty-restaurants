import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ToastService } from '@app/services/toast/toast.service';

import { MakeReservationComponent } from './make-reservation.component';

describe('MakeReservationComponent', () => {
  let fixture: ComponentFixture<MakeReservationComponent>;
  let component: MakeReservationComponent;
  let toast: jasmine.SpyObj<ToastService>;

  const timeRange = {
    start: new Date(2026, 10, 8, 10, 30).toISOString(),
    end: new Date(2026, 10, 8, 12, 0).toISOString(),
  };

  beforeEach(async () => {
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']);

    await TestBed.configureTestingModule({
      imports: [MakeReservationComponent],
      providers: [{ provide: ToastService, useValue: toast }],
    }).compileComponents();

    fixture = TestBed.createComponent(MakeReservationComponent);
    component = fixture.componentInstance;
    component.locationName = '9 Abashidze Street';
    component.table = {
      id: 'table-uuid',
      tableNumber: 3,
      locationId: 'loc-1',
      capacity: 4,
      timeRanges: [timeRange],
    };
    component.selectedTimeRange = timeRange;
    component.guests = 2;
    fixture.detectChanges();
  });

  it('shows the location, the table number and the capacity', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('9 Abashidze Street, Table 3');
    expect(text).toContain('Table seating capacity: 4 people');
    expect(text).not.toContain('table-uuid');
  });

  it('emits the table ID, time range and guests on Make a Reservation', () => {
    const spy = spyOn(component.makeReservation, 'emit');

    component.onMakeReservation();

    expect(spy).toHaveBeenCalledOnceWith({ tableId: 'table-uuid', timeRange, guests: 2 });
  });
});
