import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { CartStateService } from '@app/services/cart/cart-state.service';
import { PreOrdersService } from '@app/services/pre-orders/pre-orders.service';
import { buildPreOrderQueryParams } from '@app/shared/helpers/pre-order-navigation';

import { GroupBonus } from '@app/models/group-orders/group-bonus.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupReservationStatus, GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { ReservationCustomer, ReservationStatusCustomer } from '@app/models/reservations/reservation.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
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
  let guestInvite: jasmine.SpyObj<GuestInviteService>;
  let preOrders: jasmine.SpyObj<PreOrdersService>;
  let cartState: jasmine.SpyObj<CartStateService>;

  async function render(
    ownerGroupId$: Observable<string>,
    statusResponse: GroupStatusResponse = status('Open'),
    sessions: GroupSessionsResponse = SESSIONS,
  ): Promise<HTMLElement> {
    groupOrders.getOwnerGroupId$.and.returnValue(ownerGroupId$);
    groupOrders.getSessions$.and.returnValue(of(sessions));
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

    guestInvite = jasmine.createSpyObj<GuestInviteService>('GuestInviteService', ['getInviteToken$']);

    preOrders = jasmine.createSpyObj<PreOrdersService>('PreOrdersService', ['editPreOrderByReservationId']);
    cartState = jasmine.createSpyObj<CartStateService>('CartStateService', ['refresh']);

    TestBed.configureTestingModule({
      imports: [GroupPreOrderModalComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: GroupOrdersService, useValue: groupOrders },
        { provide: GuestInviteService, useValue: guestInvite },
        { provide: PreOrdersService, useValue: preOrders },
        { provide: CartStateService, useValue: cartState },
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

  describe('actions', () => {
    function buttons(scope: ParentNode): HTMLButtonElement[] {
      return Array.from(scope.querySelectorAll('button'));
    }

    async function click(text: string, scope: ParentNode = fixture.nativeElement): Promise<void> {
      const button = buttons(scope).find((b) => b.textContent?.trim() === text);
      expect(button).withContext(`button "${text}"`).toBeDefined();
      button!.click();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    }

    function dialog(root: HTMLElement): HTMLElement | null {
      return root.querySelector('app-confirm-dialog');
    }

    async function openRemoveDialog(root: HTMLElement): Promise<void> {
      const anna = buttons(root).find((b) => b.getAttribute('aria-expanded') !== null && b.textContent?.includes('Anna'));
      anna!.click();
      fixture.detectChanges();
      await click('Remove Anna');
    }

    it('copies the invite link and shows "Link copied"', async () => {
      const writeText = spyOn(navigator.clipboard, 'writeText').and.resolveTo();
      guestInvite.getInviteToken$.and.returnValue(of({ token: 't1' }));
      const root = await render(of('g1'));

      await click('Copy pre-order link');

      expect(guestInvite.getInviteToken$).toHaveBeenCalledWith('r1');
      expect(writeText).toHaveBeenCalledWith(`${window.location.origin}/join/t1`);
      expect(root.textContent).toContain('Link copied');
    });

    it('removes the guest and reloads after the dialog is confirmed', async () => {
      groupOrders.removeGuest$.and.returnValue(of(SESSIONS));
      const root = await render(of('g1'));

      await openRemoveDialog(root);
      await click('Remove Anna', dialog(root)!);

      expect(groupOrders.removeGuest$).toHaveBeenCalledWith('g1', 'guest-anna');
      expect(groupOrders.getSessions$).toHaveBeenCalledTimes(2);
    });

    it('does not remove the guest when the owner chooses "Keep"', async () => {
      const root = await render(of('g1'));

      await openRemoveDialog(root);
      await click('Keep', dialog(root)!);

      expect(groupOrders.removeGuest$).not.toHaveBeenCalled();
      expect(dialog(root)).toBeNull();
    });

    it('submits at once when every seat has ordered', async () => {
      groupOrders.submit$.and.returnValue(of([]));
      const root = await render(of('g1'));

      await click('Submit Pre-Order');

      expect(dialog(root)).toBeNull();
      expect(groupOrders.submit$).toHaveBeenCalledWith('g1', 'r1');
    });

    it('asks before submitting when seats are empty and does not submit on "Wait for guests"', async () => {
      const root = await render(of('g1'), { ...status('Open'), maxCapacity: 5 });

      await click('Submit Pre-Order');

      expect(dialog(root)?.textContent).toContain("2 seats haven't pre-ordered yet");
      await click('Wait for guests', dialog(root)!);

      expect(groupOrders.submit$).not.toHaveBeenCalled();
      expect(dialog(root)).toBeNull();
    });

    it('submits after "Confirm anyway" when seats are empty', async () => {
      groupOrders.submit$.and.returnValue(of([]));
      const root = await render(of('g1'), { ...status('Open'), maxCapacity: 5 });

      await click('Submit Pre-Order');
      await click('Confirm anyway', dialog(root)!);

      expect(groupOrders.submit$).toHaveBeenCalledOnceWith('g1', 'r1');
    });

    it('disables the submit button when nobody has ordered', async () => {
      const root = await render(of('g1'), { ...status('Open'), orderedCount: 0 });

      const submit = buttons(root).find((b) => b.textContent?.trim() === 'Submit Pre-Order');
      expect(submit?.disabled).toBeTrue();
    });
  });

  describe('owner dishes', () => {
    const SALAD = { id: 'dishA', name: 'Caesar Salad', price: 10 };
    const SOUP = { id: 'dishB', name: 'Tom Yum', price: 20 };

    function ownerWithDishes(): GroupSessionsResponse {
      return {
        ...SESSIONS,
        owner: {
          ...SESSIONS.owner,
          items: [
            { dish: SALAD, quantity: 2 },
            { dish: SOUP, quantity: 1 },
          ],
          total: 40,
        },
      };
    }

    function button(root: HTMLElement, predicate: (b: HTMLButtonElement) => boolean): HTMLButtonElement | undefined {
      return Array.from(root.querySelectorAll('button')).find(predicate);
    }

    it('increases the quantity of one dish, keeps the others and reloads', async () => {
      preOrders.editPreOrderByReservationId.and.returnValue(of({}) as never);
      const root = await render(of('g1'), status('Open'), ownerWithDishes());

      button(root, (b) => b.textContent?.includes('My Pre-Order') ?? false)!.click();
      fixture.detectChanges();
      button(root, (b) => b.getAttribute('aria-label') === 'Increase Caesar Salad')!.click();
      fixture.detectChanges();
      await fixture.whenStable();

      expect(preOrders.editPreOrderByReservationId).toHaveBeenCalledOnceWith('r1', {
        orderItems: [
          jasmine.objectContaining({ dish: jasmine.objectContaining({ id: 'dishA' }), quantity: 3 }),
          jasmine.objectContaining({ dish: jasmine.objectContaining({ id: 'dishB' }), quantity: 1 }),
        ],
      });
      expect(groupOrders.getSessions$).toHaveBeenCalledTimes(2);
    });

    it('opens the menu in pre-order mode and closes the modal from "Add my dishes"', async () => {
      const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
      const emptyOwner: GroupSessionsResponse = {
        ...SESSIONS,
        owner: { ...SESSIONS.owner, items: [], total: 0, isSubmitted: false },
      };
      const root = await render(of('g1'), { ...status('Open'), orderedCount: 2 }, emptyOwner);
      const closed: { changed: boolean }[] = [];
      fixture.componentInstance.closed.subscribe((event) => closed.push(event));

      button(root, (b) => b.textContent?.includes('Add my dishes') ?? false)!.click();
      fixture.detectChanges();

      expect(navigate).toHaveBeenCalledWith(['/menu'], { queryParams: buildPreOrderQueryParams(RESERVATION) });
      expect(closed).toEqual([{ changed: false }]);
    });
  });
});
