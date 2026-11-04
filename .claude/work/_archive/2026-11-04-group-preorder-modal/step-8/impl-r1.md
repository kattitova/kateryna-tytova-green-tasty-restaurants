# FE-4 implement r1

Mode: implement, step 8 (FE-4).

Log:
- Read tests-r1.md (2 red) and M00. Customer Cancel is now an icon button (`assets/svg/trash.svg`, `aria-label="Cancel reservation"`, classes `icon-button icon-button--small cancel-icon`) after the status badge in the address line. It shows for Reserved and InProgress (`canCancelAsCustomer`) and emits `cancelReservation` with the id.
- The text "Cancel" button stays for the waiter only (`@if (role() === Roles.WAITER)`). Waiter markup otherwise unchanged.
- Reserved customer row: Edit (secondary), Pre-order (primary), "Group Pre-Order" (secondary) shown only when `isGroup()` (`isGroupReservation(guestCount)`); it emits `groupPreOrder` with the reservation. All customer buttons are `button--small` (Edit, Pre-order, Group Pre-Order, Leave Feedback, Update Feedback).
- SCSS: `.cancel-icon` margin only. Page not wired (FE-5). Spec unchanged.

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 385 of 385 SUCCESS, 0 failed.
Build: "Bundle check passed"; initial total 812.50 kB raw / 172.95 kB transfer (was 810.98 kB at FE-2). No style budget line for card-reservation.component.scss.
Not checked: layout in a browser (three small buttons in one row at 1440 px; the plan estimates wrapping on narrow cards).
