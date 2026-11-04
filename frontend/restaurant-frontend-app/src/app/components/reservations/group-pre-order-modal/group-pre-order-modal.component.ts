import { CurrencyPipe, DatePipe, formatCurrency, formatDate } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  LOCALE_ID,
  NgZone,
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
import { forkJoin, from, switchMap, tap, timer } from 'rxjs';

import { GroupPreOrderView } from '@app/models/group-orders/group-pre-order-view.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { ReservationCustomer } from '@app/models/reservations/reservation.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { ToastService } from '@app/services/toast/toast.service';
import { LoadingSpinnerComponent } from '@app/shared/custom-components/loading-spinner/loading-spinner.component';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { buildGroupPreOrderView, formatTimeLeft } from '@app/shared/helpers/group-pre-order.helpers';
import { buildInviteLink } from '@app/shared/helpers/invite-link';
import { ConfirmDialogComponent } from '@app/shared/custom-components/confirm-dialog/confirm-dialog.component';

type LoadState = 'loading' | 'error' | 'content';
type BonusTone = 'progress' | 'success' | 'muted';

type DialogState = { kind: 'remove'; participantId: string; name: string } | { kind: 'submit' };

interface DialogView {
  title: string;
  message: string;
  cancelLabel: string;
  confirmLabel: string;
  variant: 'danger' | 'primary';
}

interface BonusBox {
  tone: BonusTone;
  heading: string;
  subLine: string;
  /** 0 to 100. Set only while the bonus is in progress. */
  progress: number | null;
}

const MINUS = '−';
const LINK_COPIED_MS = 3000;
const SEAT_SCROLL_STEP_PX = 240;
/** The seats row shows about 7 seats in 800 px. More seats need the arrows. */
const SEATS_WITHOUT_SCROLL = 7;

@Component({
  selector: 'app-group-pre-order-modal',
  standalone: true,
  imports: [ConfirmDialogComponent, CurrencyPipe, DatePipe, LoadingSpinnerComponent, ModalComponent],
  templateUrl: './group-pre-order-modal.component.html',
  styleUrl: './group-pre-order-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupPreOrderModalComponent implements OnInit {
  private readonly groupOrders = inject(GroupOrdersService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly locale = inject(LOCALE_ID);
  private readonly ngZone = inject(NgZone);
  private readonly guestInvite = inject(GuestInviteService);
  private readonly toast = inject(ToastService);

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

  protected readonly dialog = signal<DialogState | null>(null);
  protected readonly linkCopied = signal(false);
  protected readonly submitting = signal(false);

  protected readonly dialogView = computed<DialogView | null>(() => {
    const dialog = this.dialog();
    const view = this.view();
    if (!dialog || !view) {
      return null;
    }

    if (dialog.kind === 'remove') {
      return {
        title: `Remove ${dialog.name} from the table?`,
        message: `${dialog.name}'s pre-order will be deleted and the seat becomes free.`,
        cancelLabel: 'Keep',
        confirmLabel: `Remove ${dialog.name}`,
        variant: 'danger',
      };
    }

    return this.buildSubmitDialog(view);
  });

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

  protected copyLink(): void {
    this.guestInvite
      .getInviteToken$(this.reservation().id)
      .pipe(
        switchMap(({ token }) => from(navigator.clipboard.writeText(buildInviteLink(token)))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.showLinkCopied(),
        error: () => this.toast.error('Could not copy the link. Please try again.'),
      });
  }

  protected askRemoveGuest(participantId: string): void {
    const guest = this.guests().find((item) => item.id === participantId);
    if (guest) {
      this.dialog.set({ kind: 'remove', participantId, name: guest.name });
    }
  }

  protected confirmRemove(): void {
    const dialog = this.dialog();
    if (dialog?.kind !== 'remove' || this.groupId === null) {
      return;
    }

    this.dialog.set(null);
    this.groupOrders
      .removeGuest$(this.groupId, dialog.participantId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.changed.set(true);
        this.reload();
      });
  }

  protected keep(): void {
    this.dialog.set(null);
  }

  protected submit(): void {
    const view = this.view();
    if (!view?.canSubmit || this.submitting()) {
      return;
    }

    if (view.remainingCount === 0) {
      this.sendSubmit();
      return;
    }

    this.dialog.set({ kind: 'submit' });
  }

  protected confirmSubmit(): void {
    this.dialog.set(null);
    this.sendSubmit();
  }

  protected waitForGuests(): void {
    this.dialog.set(null);
  }

  /** Escape and the close button close only the open dialog first. */
  protected onClose(): void {
    if (this.dialog()) {
      this.dialog.set(null);
      return;
    }

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

  private showLinkCopied(): void {
    this.linkCopied.set(true);
    // Outside the Angular zone: a 3 s timer in the zone would keep the app unstable for 3 s.
    this.ngZone.runOutsideAngular(() => {
      timer(LINK_COPIED_MS)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.linkCopied.set(false));
    });
  }

  private sendSubmit(): void {
    if (this.groupId === null) {
      return;
    }

    this.submitting.set(true);
    this.groupOrders
      .submit$(this.groupId, this.reservation().id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.changed.set(true);
          this.reload();
        },
        error: () => this.submitting.set(false),
      });
  }

  private buildSubmitDialog(view: GroupPreOrderView): DialogView {
    const remaining = view.remainingCount;
    const { bonus } = view;
    let clause = '';
    if (bonus.state === 'InProgress') {
      clause =
        bonus.tier === 'FreeDessert'
          ? " and the free dessert bonus won't apply"
          : " and the 10% group discount won't apply";
    }

    return {
      title: remaining === 1 ? "1 seat hasn't pre-ordered yet" : `${remaining} seats haven't pre-ordered yet`,
      message:
        `If you confirm now, the restaurant will receive ${view.orderedCount} ${this.pluralPreOrders(view.orderedCount)}` +
        `${clause}. After confirmation, guests can't change their orders.`,
      cancelLabel: 'Wait for guests',
      confirmLabel: 'Confirm anyway',
      variant: 'primary',
    };
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
      const until = bonus.deadline ? `Until ${this.formatDeadline(bonus.deadline)}` : '';
      return {
        tone: 'progress',
        heading: `${n} more ${this.pluralPreOrders(n)} — ${reward}`,
        subLine: left && until ? `${until} · ${left}` : until,
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
    const money = (value: number) => formatCurrency(value, this.locale, '$');

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

    return 'Group discount — deadline passed';
  }
}
