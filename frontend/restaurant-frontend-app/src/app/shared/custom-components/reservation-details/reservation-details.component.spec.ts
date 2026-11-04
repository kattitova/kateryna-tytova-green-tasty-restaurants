import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReservationDetails } from '@app/models/reservations/reservation-details.model';

import { ReservationDetailsComponent } from './reservation-details.component';

describe('ReservationDetailsComponent', () => {
  let fixture: ComponentFixture<ReservationDetailsComponent>;

  const fullDetails: ReservationDetails = {
    address: '48 Rustaveli Avenue',
    startTime: new Date(2026, 9, 17, 12, 0),
    endTime: new Date(2026, 9, 17, 14, 0),
    guestCount: 6,
  };

  const render = (details: ReservationDetails): void => {
    fixture.componentRef.setInput('details', details);
    fixture.detectChanges();
  };

  const items = (): string[] =>
    Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('.detail')).map(
      (item) => item.textContent?.trim() ?? ''
    );

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReservationDetailsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationDetailsComponent);
  });

  it('renders address, date, time range and guests in the design order', () => {
    render(fullDetails);

    expect(items()).toEqual(['48 Rustaveli Avenue', 'Oct 17, 2026', '12:00 - 14:00', '6 Guests']);
  });

  it('uses the singular for one guest', () => {
    render({ ...fullDetails, guestCount: 1 });

    expect(items()).toContain('1 Guest');
  });

  it('accepts ISO strings for the times', () => {
    render({
      ...fullDetails,
      startTime: new Date(2026, 9, 17, 9, 5).toISOString(),
      endTime: new Date(2026, 9, 17, 10, 35).toISOString(),
    });

    expect(items()).toContain('09:05 - 10:35');
  });

  it('shows only the address when the other details are unknown', () => {
    render({ address: '48 Rustaveli Avenue' });

    expect(items()).toEqual(['48 Rustaveli Avenue']);
  });

  it('shows an icon next to each detail', () => {
    render(fullDetails);

    const icons = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLImageElement>('.detail img')
    ).map((icon) => icon.getAttribute('src'));

    expect(icons).toEqual([
      'assets/svg/reservation-details/location.svg',
      'assets/svg/reservation-details/calendar.svg',
      'assets/svg/reservation-details/clock.svg',
      'assets/svg/reservation-details/people.svg',
    ]);
  });
});
