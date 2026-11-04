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

  describe('layout', () => {
    function byText(root: HTMLElement, selector: string, text: string): HTMLElement {
      const found = Array.from(root.querySelectorAll<HTMLElement>(selector)).find((el) => el.textContent?.trim() === text);
      expect(found).withContext(`${selector} "${text}"`).toBeDefined();
      return found!;
    }

    function leafByText(root: HTMLElement, text: string): HTMLElement {
      const found = Array.from(root.querySelectorAll<HTMLElement>('*')).find(
        (el) => el.children.length === 0 && el.textContent?.trim() === text,
      );
      expect(found).withContext(`leaf "${text}"`).toBeDefined();
      return found!;
    }

    it('puts the "Sent to Restaurant" badge in the title row next to the heading', async () => {
      const root = await render(of('g1'), status('Submitted'));

      const badge = leafByText(root, 'Sent to Restaurant');
      const title = byText(root, 'h2', 'Group Pre-Order');

      expect(badge.parentElement).toBe(title.parentElement);
    });

    it('sizes the badge to its text plus horizontal padding', async () => {
      const root = await render(of('g1'), status('Submitted'));
      const badge = leafByText(root, 'Sent to Restaurant');

      const range = document.createRange();
      range.selectNodeContents(badge);
      const textWidth = range.getBoundingClientRect().width;
      const style = getComputedStyle(badge);
      const expected = textWidth + parseFloat(style.paddingLeft) + parseFloat(style.paddingRight);

      expect(textWidth).toBeGreaterThan(0);
      expect(Math.abs(badge.getBoundingClientRect().width - expected)).toBeLessThanOrEqual(2);
    });

    it('aligns the guests title with the card content (left edge of "Table")', async () => {
      const root = await render(of('g1'));

      const guests = byText(root, 'h3', "Guests' Pre-Orders").getBoundingClientRect();
      const table = byText(root, 'h3', 'Table').getBoundingClientRect();

      expect(Math.abs(guests.left - table.left)).toBeLessThanOrEqual(1);
    });

    it('aligns "Expand all" with the right edge of "Copy pre-order link"', async () => {
      const root = await render(of('g1'));

      const expand = byText(root, 'button', 'Expand all').getBoundingClientRect();
      const copy = byText(root, 'button', 'Copy pre-order link').getBoundingClientRect();

      expect(Math.abs(expand.right - copy.right)).toBeLessThanOrEqual(1);
    });

    it('keeps the "Table" header where it is: no own padding, 16 px from the card edge', async () => {
      const root = await render(of('g1'));
      const tableTitle = byText(root, 'h3', 'Table');
      const header = tableTitle.parentElement!;
      const card = header.closest('section')!;
      const copy = byText(root, 'button', 'Copy pre-order link');

      expect(parseFloat(getComputedStyle(header).paddingLeft)).toBe(0);
      expect(parseFloat(getComputedStyle(header).paddingRight)).toBe(0);
      expect(tableTitle.getBoundingClientRect().left - card.getBoundingClientRect().left).toBeCloseTo(16, 0);
      expect(card.getBoundingClientRect().right - copy.getBoundingClientRect().right).toBeCloseTo(16, 0);
    });
  });

  describe('grand total breakdown', () => {
    const MINUS = '−';
    type Row = [string, string | null];

    function rows(root: HTMLElement): Row[] {
      return Array.from(root.querySelectorAll('[data-testid="gpo-breakdown-row"]')).map((row) => [
        row.querySelector('[data-testid="gpo-breakdown-label"]')?.textContent?.trim() ?? '',
        row.querySelector('[data-testid="gpo-breakdown-amount"]')?.textContent?.trim() ?? null,
      ]);
    }

    function withBonus(bonus: GroupBonus): GroupStatusResponse {
      return { ...status('Open'), bonus };
    }

    it('lists one row per item with the amount apart from the label (10% discount achieved)', async () => {
      const root = await render(of('g1'));

      expect(rows(root)).toEqual([
        ['My pre-order', '$27.00'],
        ["Guests' pre-orders (2)", '$109.00'],
        ['Total before discount', '$136.00'],
        [`Group discount ${MINUS}10% applied`, `${MINUS}$13.60`],
      ]);
    });

    it('renders every row as a list item and every amount as a no-wrap element', async () => {
      const root = await render(of('g1'));

      const items = Array.from(root.querySelectorAll('[data-testid="gpo-breakdown-row"]'));
      expect(items.length).toBe(4);
      items.forEach((item) => expect(item.tagName).toBe('LI'));
      const amounts = Array.from(root.querySelectorAll<HTMLElement>('[data-testid="gpo-breakdown-amount"]'));
      expect(amounts.length).toBe(4);
      amounts.forEach((amount) => expect(getComputedStyle(amount).whiteSpace).toBe('nowrap'));
    });

    it('shows the unlock amount of a 10% discount that is still in progress', async () => {
      const root = await render(
        of('g1'),
        withBonus({ state: 'InProgress', tier: 'TenPercentDiscount', discountPercent: 10, deadline: null }),
      );

      expect(rows(root)).toEqual([
        ['My pre-order', '$27.00'],
        ["Guests' pre-orders (2)", '$109.00'],
        [`Group discount ${MINUS}10% unlocks at 3 of 3`, `${MINUS}$13.60`],
      ]);
    });

    it('shows a free dessert in progress as a row without an amount', async () => {
      const root = await render(
        of('g1'),
        withBonus({ state: 'InProgress', tier: 'FreeDessert', discountPercent: 0, deadline: null }),
      );

      expect(rows(root).at(-1)).toEqual(['Free dessert unlocks at 3 of 3', null]);
    });

    it('shows an achieved free dessert as a row without an amount element', async () => {
      const root = await render(
        of('g1'),
        withBonus({ state: 'Achieved', tier: 'FreeDessert', discountPercent: 0, deadline: null }),
      );

      expect(rows(root)).toEqual([
        ['My pre-order', '$27.00'],
        ["Guests' pre-orders (2)", '$109.00'],
        ['Free dessert for the table', null],
      ]);
    });

    it('shows a missed bonus as a row without an amount', async () => {
      const root = await render(
        of('g1'),
        withBonus({ state: 'Missed', tier: 'TenPercentDiscount', discountPercent: 10, deadline: null }),
      );

      expect(rows(root).at(-1)).toEqual(['Group discount — deadline passed', null]);
    });

    it('shows a dash for the owner row when the owner has not ordered', async () => {
      const emptyOwner: GroupSessionsResponse = {
        ...SESSIONS,
        owner: { ...SESSIONS.owner, items: [], total: 0, isSubmitted: false },
      };
      const root = await render(of('g1'), status('Open'), emptyOwner);

      expect(rows(root)[0]).toEqual(['My pre-order', '—']);
    });
  });
});
