import { CurrencyPipe, DatePipe, formatDate } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, switchMap, tap } from 'rxjs';

import { GroupPreOrderView } from '@app/models/group-orders/group-pre-order-view.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { ReservationCustomer } from '@app/models/reservations/reservation.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { LoadingSpinnerComponent } from '@app/shared/custom-components/loading-spinner/loading-spinner.component';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { buildGroupPreOrderView, formatTimeLeft } from '@app/shared/helpers/group-pre-order.helpers';

type LoadState = 'loading' | 'error' | 'content';
type BonusTone = 'progress' | 'success' | 'muted';

interface BonusBox {
  tone: BonusTone;
  heading: string;
  subLine: string;
  /** 0 to 100. Set only while the bonus is in progress. */
  progress: number | null;
}

const MINUS = '−';
const SEAT_SCROLL_STEP_PX = 240;
/** The seats row shows about 7 seats in 800 px. More seats need the arrows. */
const SEATS_WITHOUT_SCROLL = 7;

@Component({
  selector: 'app-group-pre-order-modal',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, LoadingSpinnerComponent, ModalComponent],
  templateUrl: './group-pre-order-modal.component.html',
  styleUrl: './group-pre-order-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupPreOrderModalComponent implements OnInit {
  private readonly groupOrders = inject(GroupOrdersService);
  private readonly destroyRef = inject(DestroyRef);

  readonly reservation = input.required<ReservationCustomer>();
  readonly ownerName = input('');
  readonly closed = output<{ changed: boolean }>();

  protected readonly state = signal<LoadState>('loading');
  protected readonly view = signal<GroupPreOrderView | null>(null);
  protected readonly sessions = signal<GroupSessionsResponse | null>(null);
  protected readonly ownerExpanded = signal(false);
  protected readonly expandedGuestIds = signal<ReadonlySet<string>>(new Set());
  /** Set to true by the actions of later steps (remove, submit, edit). */
  protected readonly changed = signal(false);

  private readonly seatsTrack = viewChild<ElementRef<HTMLElement>>('seatsTrack');
  private groupId: string | null = null;

  protected readonly guests = computed<Participant[]>(() => this.sessions()?.participants ?? []);
  protected readonly owner = computed<Participant | null>(() => this.sessions()?.owner ?? null);
  protected readonly allGuestsExpanded = computed(
    () => this.guests().length > 0 && this.guests().every((g) => this.expandedGuestIds().has(g.id)),
  );
  protected readonly ownerInitials = computed(() => {
    const seat = this.view()?.seats[0];
    return seat?.kind === 'owner' ? seat.initials : '';
  });
  protected readonly seatsScroll = computed(() => (this.view()?.seats.length ?? 0) > SEATS_WITHOUT_SCROLL);

  protected readonly bonusBox = computed<BonusBox | null>(() => {
    const view = this.view();
    return view ? this.buildBonusBox(view) : null;
  });

  protected readonly footerBonusLine = computed<string>(() => {
    const view = this.view();
    return view ? this.buildFooterBonusLine(view) : '';
  });

  ngOnInit(): void {
    this.load();
  }

  /** Repeats the sessions and status calls for the loaded group. */
  reload(): void {
    if (this.groupId === null) {
      this.load();
      return;
    }

    this.fetch(this.groupId);
  }

  protected onClose(): void {
    this.closed.emit({ changed: this.changed() });
  }

  protected toggleOwner(): void {
    this.ownerExpanded.update((expanded) => !expanded);
  }

  protected toggleGuest(id: string): void {
    this.expandedGuestIds.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(id)) {
        next.add(id);
      }
      return next;
    });
  }

  protected toggleAllGuests(): void {
    this.expandedGuestIds.set(this.allGuestsExpanded() ? new Set() : new Set(this.guests().map((g) => g.id)));
  }

  protected scrollSeats(direction: -1 | 1): void {
    this.seatsTrack()?.nativeElement.scrollBy({ left: direction * SEAT_SCROLL_STEP_PX, behavior: 'smooth' });
  }

  protected itemsSummary(participant: Participant): string {
    return participant.items.map((item) => item.dish.name).join(', ');
  }

  protected pluralPreOrders(count: number): string {
    return count === 1 ? 'pre-order' : 'pre-orders';
  }

  private load(): void {
    this.state.set('loading');
    this.groupOrders
      .getOwnerGroupId$(this.reservation().id)
      .pipe(
        tap((id) => (this.groupId = id)),
        switchMap((id) => this.groupData$(id)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ([sessions, status]) => this.show(sessions, status),
        error: () => this.state.set('error'),
      });
  }

  private fetch(groupId: string): void {
    this.groupData$(groupId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ([sessions, status]) => this.show(sessions, status),
        error: () => this.state.set('error'),
      });
  }

  private groupData$(groupId: string) {
    return forkJoin([this.groupOrders.getSessions$(groupId), this.groupOrders.getStatus$(groupId)]);
  }

  private show(
    sessions: GroupSessionsResponse,
    status: Parameters<typeof buildGroupPreOrderView>[1],
  ): void {
    this.sessions.set(sessions);
    this.view.set(buildGroupPreOrderView(sessions, status, this.ownerName()));
    this.state.set('content');
  }

  private formatDeadline(deadline: string | null): string {
    return deadline ? formatDate(deadline, 'MMM d, HH:mm', 'en-US') : '';
  }

  private buildBonusBox(view: GroupPreOrderView): BonusBox {
    const { bonus } = view;

    if (view.isSubmitted) {
      const guestsLine = 'guests can no longer change their orders';
      const bonusText =
        bonus.state === 'Achieved'
          ? bonus.tier === 'FreeDessert'
            ? 'Free dessert for the table'
            : '10% off applied'
          : null;
      return {
        tone: 'success',
        heading: 'Pre-order sent to the restaurant',
        subLine: bonusText ? `${bonusText} \u00b7 ${guestsLine}` : 'Guests can no longer change their orders',
        progress: null,
      };
    }

    const isDiscount = bonus.tier === 'TenPercentDiscount';

    if (bonus.state === 'Achieved') {
      return {
        tone: 'success',
        heading: isDiscount
          ? 'Everyone has ordered — 10% off applied'
          : 'Everyone has ordered — free dessert for the table',
        subLine: isDiscount
          ? 'The discount is included in the group total'
          : 'Your reservation received a free dessert bonus for the table',
        progress: null,
      };
    }

    if (bonus.state === 'InProgress') {
      const n = view.remainingCount;
      const reward = isDiscount ? '10% off for the whole table' : 'free dessert for the table';
      const left = bonus.deadline ? formatTimeLeft(new Date(bonus.deadline), new Date()) : null;
      const until = `Until ${this.formatDeadline(bonus.deadline)}`;
      return {
        tone: 'progress',
        heading: `${n} more ${this.pluralPreOrders(n)} — ${reward}`,
        subLine: left ? `${until} · ${left}` : until,
        progress: view.seatCount > 0 ? Math.min(100, (view.orderedCount / view.seatCount) * 100) : 0,
      };
    }

    return {
      tone: 'muted',
      heading: 'The group bonus is no longer available',
      subLine:
        view.orderedCount < view.seatCount
          ? `Not all seats ordered before ${this.formatDeadline(bonus.deadline)}`
          : 'The last pre-order came less than 2 h before the reservation',
      progress: null,
    };
  }

  private buildFooterBonusLine(view: GroupPreOrderView): string {
    const { bonus } = view;
    const money = (value: number) => `$${value.toFixed(2)}`;

    if (bonus.state === 'InProgress') {
      return bonus.tier === 'FreeDessert'
        ? `Free dessert unlocks at ${view.seatCount} of ${view.seatCount}`
        : `Group discount ${MINUS}10% unlocks at ${view.seatCount} of ${view.seatCount} (${MINUS}${money(view.unlockDiscountAmount)})`;
    }

    if (bonus.state === 'Achieved') {
      return bonus.tier === 'FreeDessert'
        ? 'Free dessert for the table'
        : `Total before discount ${money(view.grandTotal)} · Group discount ${MINUS}10% applied (${MINUS}${money(view.discountAmount)})`;
    }

    return `Group discount — deadline passed`;
  }
}
