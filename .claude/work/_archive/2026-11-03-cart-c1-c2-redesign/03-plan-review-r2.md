# Plan review r2: Cart redesign, screens C1 and C2

Reviewed: `02-plan-r2.md` against `00-task.md` (including "Decisions"), `figma/c2-cart.png` (opened), `03-plan-review-r1.md`, and the frontend and backend code.

## Verdict
approve with minor

No blocker and no major finding. The plan respects every user decision. The C1/C2 rule and the data join use real fields. The `'unknown'` mode is a state of its own and cannot fall back to Submit. Four minor findings below. The implementer and the code-reviewer can handle them in the steps. No new plan round is needed.

## Findings

### [minor] Step 2 / Step 4 — with short content the footer does not sit at the panel bottom
- Problem: The Figma panel (`figma/c2-cart.png`) has the footer at the bottom of the panel, with empty space between the last card and "Grand Total", even with only two dishes. The plan uses `position: sticky; bottom: 0` on a footer inside the order block in `.cart-body`. Sticky only pins the footer when the body content overflows. With 1-3 dishes (the normal case) the footer renders directly under the "Add dish" button, not at the panel bottom. The plan says "single-order case matches Figma", and the step 4 manual check only covers the scroll case ("footer stays at the bottom while the cards scroll").
- Evidence: `figma/c2-cart.png` (footer at y ~815 of 1013, cards end at ~433); `02-plan-r2.md:98` (footer CSS), `:138` (body CSS), `:139` (manual check).
- Required change: The single-order layout must also fill the body height when the content is short. For example: `.cart-body` is a flex column, the unsubmitted section and the `app-not-submitted-order` host grow (`flex: 1`, host `display: flex`/`block` with `min-height`), and the order footer gets `margin-top: auto` in addition to sticky. Add "short content (2 dishes): footer at the panel bottom" to the step 4 manual check.

### [minor] Step 3 — the submit guard test does not cover the `'unknown'` mode
- Problem: The contract says `submitOrder` returns with no API call when the mode "is not `'single'`". The only test calls `submitOrder` for a group order (guestCount 4). A guard written as `mode === 'group'` passes all listed tests but lets an order with unknown size submit (for example when `editOrderModal` or a later caller reaches `submitOrder`). The disabled CTA test covers only the button, not the method.
- Evidence: `02-plan-r2.md:113` vs `:126`.
- Required change: Add one test: `getReservationsForUser$` errors (map `null`), `submitOrder(orderId)` called directly → `submitPreOrderByReservationId` is not called. Optionally the same for a `reservationId` that is not in the list.

### [minor] Step 2 — child input default `'single'` fails open
- Problem: `orderMode = input<CartOrderMode>('single')` means a missing binding gives a working Submit button. The parent decides the mode (one decision in one place), and the parent guard blocks the API call, so the risk is low. But the child is the only place where the default lives, and `'unknown'` would fail closed. `app-not-submitted-order` has one user only (`cart.component.html:44`).
- Evidence: `02-plan-r2.md:89`. The step 2 test "default mode: Submit, enabled".
- Required change: Either make the input `input.required<CartOrderMode>()` (tests then set the mode explicitly), or keep `'single'` and state why. No other change.

### [minor] Step 2 — price format differs from Figma
- Problem: Figma shows whole-dollar prices ("$17", "Grand Total $27"). The plan keeps `| currency` and its test expects `$20.00`. This is the current behaviour and the task does not ask for a format change, so keeping it is acceptable, but the plan does not say it is a known deviation.
- Evidence: `figma/c2-cart.png`; `not-submitted-order.component.html:80,101`; `02-plan-r2.md:97`.
- Required change: Note it as an accepted deviation (or under Out of scope). No code change.

## Earlier findings (r1)
- [blocker] Step 5 owner token on `/guest/groups/...` — not applicable (step 5 dropped by user decision Q2/Q3). The interceptor fact is kept in F4 for the future task. Correct.
- [major] Q1 + Q3 leave the group owner with no submit path — fixed: stated in "Contracts and risks" as accepted by the user (`00-task.md` Decisions Q1). The plan also enforces the rule in `submitOrder`.
- [major] Step 5b toast on error — not applicable for step 5. For `GET /reservations` the global toast is stated and accepted (F5, step 3). Checked: `getReservationsForUser$` sets no `SKIP_ERROR_TOAST` (`reservation.service.ts:81-84`), and the URL has no `orders`, so the toast shows (`token.interceptor.ts:87-93`). Correct.
- [minor] Dead `addDishToOrder` — not applicable: user decision Q4 keeps "Add dish". Correct.
- [minor] Loading state test — fixed (step 3, `Subject` test).
- [minor] Sticky per-order footer — partly fixed: the full-width border and padding are now specified, but the short-content case is still open (first finding above).
- [minor] Latency of `GET /reservations` — fixed ("Extra request" risk line).
- [minor] Chevron colour — not applicable (no link in this task). Correct.

## User decisions check
- Save makes no API call: step 3 `saveOrder` calls no `PreOrdersService` method. The test asserts neither `submitPreOrderByReservationId` nor `editPreOrderByReservationId` is called, plus toast and `closeCartEvent`. Correct.
- Group owner cannot submit alone: no Submit button for `'group'`, and `submitOrder` refuses non-`'single'` modes. The submit modal opens only after a successful `submitOrder` (`cart.component.ts:147-166`, `cart.component.html:110-119`), so it cannot open for a group order. Correct.
- No guests line, no link, no interceptor change, no tech-debt entry: none in the steps. Correct.
- Caption and "Add dish" unchanged: step 2 keeps the caption text, `addDish`, `addDishToOrder` and the button, with a test for each. Correct.

## Confirmed
- Join fields exist: `Order.reservationId` (`models/orders/order.model.ts`), `Reservation.id` and `Reservation.guestCount: number` (`models/reservations/reservation.model.ts`), backend JSON `id` and `guestCount` (`ReservationResponse.cs:9,21-22`).
- `ReservationService.getReservationsForUser$()` calls `GET /reservations` (`services/reservation/reservation.service.ts:81-84`. The plan's short path `reservation.service.ts:81` points to this file). For a customer the backend skips reservations that fail to convert (`GetReservationsForUserHandler.cs:108-117`). The plan maps a missing reservation to `'unknown'`, which is safe.
- Group threshold 2 matches the backend and `reservation-invite-modal.component.ts:20`.
- The cart is shown only to customers on the owner routes (`header.component.html:51,146,249`), and `app-not-submitted-order` has one user (`cart.component.html:44`).
- `'unknown'` handling: map `null` or reservation missing → `'unknown'` → CTA disabled, inline message with `role="status"`, `onCtaClick` emits nothing, parent guard blocks submit. Tests are concrete (selectors `.cart-cta`, `.cta-unavailable`, `.grand-total-value`, exact texts and emitted values) and can fail.
- The forkJoin error case is covered by the test "`getReservationsForUser$` errors → the order still renders", so a plain `forkJoin` without `catchError` on the reservations stream fails that test.
- `ToastService.success(message, title?)` exists (`services/toast/toast.service.ts:13`).
- Colour tokens `$neutral-0/100/200/400/900`, `$green-600`, `$green-800` exist (`shared/styles/abstracts/_colors.scss`). Styling values in step 2 and step 4 match the Figma facts in `00-task.md` (panel 640 px and shadow, header/body/footer padding, card radius/padding/shadow, 96 px image, quantity buttons, price, trash position, Grand Total, CTA gradient, height, radius and font).
- `models/cart/` already exists (`cart.model.ts`, `create-order-request.model.ts`). The new file fits there. `shared/helpers/` exists with spec files next to helpers.
- Current SCSS sizes: `not-submitted-order.component.scss` 1520 B, `cart.component.scss` 1713 B. The 4 kB warning budget is realistic.
- No API change: no `RouteRegistry.cs`, `ApiRoutes.md` or Terraform/Syndicate change needed.
- Existing tests: 5 in `cart.component.spec.ts`, 3 in `not-submitted-order.component.spec.ts`, as the plan states.
- Step order: data shape (1) → child (2) → parent (3) → layout (4). Each step leaves the build green. Scaffold markers are set for steps 1-3.
