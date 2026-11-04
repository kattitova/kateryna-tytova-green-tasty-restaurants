# FE-5 scaffold report

Mode: scaffold, step 9 (FE-5).
- New `components/reservations/group-pre-order-modal/` (ts, html, scss): standalone, OnPush, `reservation = input.required<ReservationCustomer>()`, `ownerName = input('')`, `closed = output<{ changed: boolean }>()`, `reload()` throws `Error('Not implemented')`. Template is `<app-modal width="800px" title="Group Pre-Order">` with the close handler; it renders without services.
- `modal.component.html`: additive `<ng-content select="[modal-title-addon]" />` after the title.
- `reservation-dashboard`: signal `groupPreOrderReservation`, computed `ownerName` (first + last name), `openGroupPreOrder`, `closeGroupPreOrder({changed})` (changed -> `loadReservations()` and `cartState.refresh()`), `(groupPreOrder)` on the card and the modal in the template.
- Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 385 of 385 SUCCESS; build "Bundle check passed", initial 814.64 kB raw / 173.23 kB transfer.
