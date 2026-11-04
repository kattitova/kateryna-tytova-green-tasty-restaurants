# FE-7 implement r1

Mode: implement, step 11 (FE-7).

Log:
- Read tests-r1.md (2 red) and M02, M07.
- Expanded "My Pre-Order" (Open only) shows each own dish with image (when `imageUrl` exists), - qty +, price and a trash button; aria-labels "Decrease {dish}", "Increase {dish}", "Remove {dish}"; "-" disabled at quantity 1 (and while a save runs). In a Submitted or Closed group the list stays read-only.
- Each action builds the new item list from `sessions.owner.items` (`{ dish: { id }, quantity }`) and calls `PreOrdersService.editPreOrderByReservationId(reservation.id, { orderItems })` (an empty list cancels the order, as the service does today). Success -> `changed = true`, `cartState.refresh()`, `reload()`. Error -> the buttons are enabled again (interceptor toast). A second click during a save is ignored.
- Owner not ordered: the row is no longer a disabled button (a nested button is invalid HTML). It is a static row with "+ Add my dishes" (Open only). Click -> `closed.emit({ changed })` and `router.navigate(['/menu'], { queryParams: buildPreOrderQueryParams(reservation) })`.
- Spec and docs not touched.

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 397 of 397 SUCCESS, 0 failed.
Build: "Bundle check passed"; initial total 846.44 kB raw / 179.41 kB transfer (842.87 kB before). Style budget: `group-pre-order-modal.component.scss` 5.52 kB (1.52 kB over the 4 kB warning; was 4.91 kB).
Not checked: layout of the edit rows in a browser; the real edit endpoint (needs the deployed API); a stale `sessions.owner.items` after a submit.
Follow-up: the modal scss is 5.52 kB. Split the rows into child components to meet the 4 kB budget.
