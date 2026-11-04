import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { GroupBonus } from '@app/models/group-orders/group-bonus.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupReservationStatus, GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { ReservationCustomer, ReservationStatusCustomer } from '@app/models/reservations/reservation.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GroupPreOrderModalComponent } from './group-pre-order-modal.component';

const RESERVATION: ReservationCustomer = {
  id: 'r1',
  tableId: 'table-1',
  startTime: new Date('2030-01-10T16:00:00Z'),
  endTime: new Date('2030-01-10T17:30:00Z'),
  guestCount: 3,
  status: ReservationStatusCustomer.Reserved,
  restaurantName: 'Green & Tasty',
  locationAddress: 'Rustaveli Avenue 48',
  waiterId: 'waiter-1',
};

const ACHIEVED: GroupBonus = { state: 'Achieved', tier: 'TenPercentDiscount', discountPercent: 10, deadline: null };

function participant(id: string, name: string, total: number, type: 'Owner' | 'Guest'): Participant {
  return { id, type, name, items: [], total, isSubmitted: true };
}

const SESSIONS: GroupSessionsResponse = {
  owner: participant('owner-g1', 'Owner', 27, 'Owner'),
  participants: [participant('guest-anna', 'Anna', 36, 'Guest'), participant('guest-ben', 'Ben', 73, 'Guest')],
  total: 109,
};

function status(state: GroupReservationStatus): GroupStatusResponse {
  return { currentParticipants: 3, maxCapacity: 3, status: state, orderedCount: 3, bonus: ACHIEVED };
}

describe('GroupPreOrderModalComponent', () => {
  let fixture: ComponentFixture<GroupPreOrderModalComponent>;
  let groupOrders: jasmine.SpyObj<GroupOrdersService>;

  async function render(
    ownerGroupId$: Observable<string>,
    statusResponse: GroupStatusResponse = status('Open'),
  ): Promise<HTMLElement> {
    groupOrders.getOwnerGroupId$.and.returnValue(ownerGroupId$);
    groupOrders.getSessions$.and.returnValue(of(SESSIONS));
    groupOrders.getStatus$.and.returnValue(of(statusResponse));

    fixture = TestBed.createComponent(GroupPreOrderModalComponent);
    fixture.componentRef.setInput('reservation', RESERVATION);
    fixture.componentRef.setInput('ownerName', 'Olga Petrenko');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    groupOrders = jasmine.createSpyObj<GroupOrdersService>('GroupOrdersService', [
      'getOwnerGroupId$',
      'getSessions$',
      'getStatus$',
      'removeGuest$',
      'submit$',
    ]);

    TestBed.configureTestingModule({
      imports: [GroupPreOrderModalComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: GroupOrdersService, useValue: groupOrders },
      ],
    });
  });

  it('shows the ordered count, the discounted grand total and the total before discount', async () => {
    const root = await render(of('g1'));

    expect(groupOrders.getSessions$).toHaveBeenCalledWith('g1');
    expect(groupOrders.getStatus$).toHaveBeenCalledWith('g1');
    expect(root.querySelector('[data-testid="gpo-ordered-summary"]')?.textContent).toContain('3 of 3 ordered');
    expect(root.querySelector('[data-testid="gpo-grand-total"]')?.textContent).toContain('$122.40');
    expect(root.textContent).toContain('$136.00');
  });

  it('shows an inline error when the group id cannot be loaded', async () => {
    const root = await render(throwError(() => new Error('Unexpected owner session id')));

    expect(root.querySelector('[data-testid="gpo-error"]')?.textContent).toContain(
      'Could not load the group pre-order.',
    );
  });

  it('shows "Sent to Restaurant" and no submit button when the group is submitted', async () => {
    const root = await render(of('g1'), status('Submitted'));

    expect(root.textContent).toContain('Sent to Restaurant');
    const buttonTexts = Array.from(root.querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(buttonTexts).not.toContain('Submit Pre-Order');
  });
});
