import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { ReservationDetails } from '@app/models/reservations/reservation-details.model';

/**
 * One row with the reservation address, date, time range and guests (Figma "Reservation Details").
 * Used in the cart and in the group pre-order modal. Missing values are not shown.
 */
@Component({
  selector: 'app-reservation-details',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './reservation-details.component.html',
  styleUrl: './reservation-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReservationDetailsComponent {
  readonly details = input.required<ReservationDetails>();

  readonly hasTimeRange = computed(() => !!this.details().startTime && !!this.details().endTime);

  readonly guestsLabel = computed(() => {
    const count = this.details().guestCount;

    if (count == null) {
      return null;
    }

    return `${count} ${count === 1 ? 'Guest' : 'Guests'}`;
  });
}
