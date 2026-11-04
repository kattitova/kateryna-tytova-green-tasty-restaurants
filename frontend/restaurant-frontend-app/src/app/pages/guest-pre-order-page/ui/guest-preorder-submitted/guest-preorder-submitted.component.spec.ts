import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuestInvite } from '@app/models/reservations/guest-invite.model';

import { GuestCartItem } from '../../state/guest-preorder.store';
import { GuestPreorderSubmittedComponent } from './guest-preorder-submitted.component';

describe('GuestPreorderSubmittedComponent', () => {
  let fixture: ComponentFixture<GuestPreorderSubmittedComponent>;

  const NOTE = 'Your pre-order is final. To change it, contact the reservation owner.';

  const invite: GuestInvite = {
    GroupReservationId: 'group-1',
    LocationId: 'loc-1',
    RestaurantName: 'Green Table',
    LocationAddress: '1 Main Street',
    StartTime: '2026-11-10T18:00:00',
    EndTime: '2026-11-10T20:00:00',
    MaxGuestCount: 4,
    TableNumber: 3,
    InviterName: 'Anna',
  };

  const items: GuestCartItem[] = [
    { dishId: 'dish-1', title: 'Green Salad', price: 12, qty: 2 },
    { dishId: 'dish-2', title: 'Tomato Soup', price: 6, qty: 2 },
  ];

  const root = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const card = (): HTMLElement => root().querySelector('.card') as HTMLElement;
  const text = (el: Element | null | undefined): string => (el?.textContent ?? '').replace(/\s+/g, ' ').trim();

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuestPreorderSubmittedComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(GuestPreorderSubmittedComponent);
  });

  const render = (inputs: Record<string, unknown>): void => {
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
  };

  it('shows the final-order note in .submitted-note', () => {
    render({ invite, items, total: 36, guestName: 'Bob' });

    const notes = root().querySelectorAll('.submitted-note');
    expect(notes.length).toBe(1);
    expect(text(notes[0])).toBe(NOTE);
  });

  it('shows the note after the total block', () => {
    render({ invite, items, total: 36, guestName: 'Bob' });

    const note = root().querySelector('.submitted-note');
    expect(note).not.toBeNull();
    expect(note?.previousElementSibling?.classList.contains('total')).toBeTrue();
  });

  it('shows the note when invite is null and items are empty', () => {
    render({ invite: null, items: [], total: 0 });

    expect(text(root().querySelector('.submitted-note'))).toBe(NOTE);
  });

  it('renders no button and no app-custom-button', () => {
    render({ invite, items, total: 36, guestName: 'Bob' });

    expect(root().querySelector('button')).toBeNull();
    expect(root().querySelector('app-custom-button')).toBeNull();
  });

  it('does not show the Create New Pre-Order text', () => {
    render({ invite, items, total: 36, guestName: 'Bob' });

    expect(text(card()).toLowerCase()).not.toContain('create new pre-order');
  });

  it('renders one item row per item with title, quantity and line price', () => {
    render({ invite, items, total: 36, guestName: 'Bob' });

    const rows = root().querySelectorAll('.item-row');
    expect(rows.length).toBe(2);
    expect(text(rows[0].querySelector('.item-title'))).toBe('Green Salad × 2');
    expect(text(rows[0].querySelector('.item-price'))).toBe('$24.00');
    expect(text(rows[1].querySelector('.item-title'))).toBe('Tomato Soup × 2');
    expect(text(rows[1].querySelector('.item-price'))).toBe('$12.00');
  });

  it('renders the total from the total input', () => {
    render({ invite, items, total: 36, guestName: 'Bob' });

    expect(text(root().querySelector('.total-value'))).toBe('$36.00');
  });

  it('renders the restaurant name from the invite', () => {
    render({ invite, items, total: 36, guestName: 'Bob' });

    const firstRow = root().querySelector('.details .row');
    expect(text(firstRow)).toContain('Green Table');
  });

  it('renders a dash in the restaurant row when invite is null', () => {
    render({ invite: null, items, total: 36, guestName: 'Bob' });

    const firstRow = root().querySelector('.details .row');
    expect(text(firstRow?.querySelector('.value'))).toBe('-');
  });
});
