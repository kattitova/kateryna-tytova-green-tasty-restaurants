import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Table } from '@app/models/tables/table.model';

import { TimeSlotsModalComponent } from './time-slots-modal.component';

describe('TimeSlotsModalComponent', () => {
  let fixture: ComponentFixture<TimeSlotsModalComponent>;

  const table: Table = {
    id: '422f8285-b773-46b5-a5bc-167ca91cf6b2',
    tableNumber: 3,
    locationId: 'loc-1',
    capacity: 4,
    timeRanges: [
      { start: '2026-11-08T10:30:00Z', end: '2026-11-08T12:00:00Z' },
      { start: '2026-11-08T12:15:00Z', end: '2026-11-08T13:45:00Z' },
    ],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TimeSlotsModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TimeSlotsModalComponent);
    fixture.componentInstance.locationName = '9 Abashidze Street';
    fixture.componentInstance.table = table;
    fixture.componentInstance.date = '2026-11-08';
    fixture.detectChanges();
  });

  it('shows the slot count and the table number once, and never the table ID', () => {
    const subtitle = (fixture.nativeElement as HTMLElement).querySelector('.subtitle')?.textContent ?? '';

    expect(subtitle.match(/2 slots/g)?.length).toBe(1);
    expect(subtitle).toContain('9 Abashidze Street, Table 3');
    expect(subtitle).not.toContain(table.id);
  });

  it('renders one time slot for each time range', () => {
    expect((fixture.nativeElement as HTMLElement).querySelectorAll('app-time-slot').length).toBe(2);
  });
});
