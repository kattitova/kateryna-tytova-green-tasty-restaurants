import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TimeSlotComponent } from './time-slot.component';

describe('TimeSlotComponent', () => {
  let fixture: ComponentFixture<TimeSlotComponent>;

  // Local times, so the expected "10:30" does not depend on the time zone of the test machine.
  const start = new Date(2026, 10, 8, 10, 30).toISOString();
  const end = new Date(2026, 10, 8, 12, 0).toISOString();

  const render = (label?: string): string => {
    fixture.componentInstance.timeRange = { start, end };
    fixture.componentInstance.label = label;
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TimeSlotComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TimeSlotComponent);
  });

  it('shows the label when one is given', () => {
    expect(render('Lunch slot')).toContain('Lunch slot');
  });

  it('shows the start time as HH:mm when no label is given', () => {
    expect(render()).toContain('10:30');
  });
});
