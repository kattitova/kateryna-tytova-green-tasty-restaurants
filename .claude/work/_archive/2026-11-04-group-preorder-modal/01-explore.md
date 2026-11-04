# 01 — Exploration report: Group Pre-Order modal (owner)

Base commit: `fa6b784` (main). Date: 2026-11-04. Explorer: read-only.

Paths: `FE` = `frontend/restaurant-frontend-app/src/app/`, `BE` = `backend/restaurant-backend-app/dnapp/lambdas/api-handler/`, `BEdocs` = `backend/restaurant-backend-app/docs/`.

## Task understanding

Add a "Group Pre-Order" button (`button--secondary`) to the reservation card on the customer Reservations page. The button opens a new owner modal (Figma M00–M11). The modal shows the table (seats, who ordered), the owner's own pre-order, each guest's pre-order (dishes, quantity, price, subtotal), a grand total, bonus progress, and a "Submit Pre-Order" action that sends the whole group order. The owner's own order must not be counted twice in the group submit. The card already has 3 owner buttons (Cancel, Edit, Pre-order), so the layout must handle a 4th button. The user asks for compact steps and few test cases.

## Relevant code

Frontend — card and page

- `FE/components/reservations/card-reservation/card-reservation.component.html:125-285` — `.actions` block: all buttons per status and role.
- `FE/components/reservations/card-reservation/card-reservation.component.html:127-172` — status `Reserved`, customer: Cancel (`button--tertiary button--large`, :128-134), Edit (`button--secondary button--large`, :137-143), Pre-order (`button--primary button--large`, :165-171).
- `FE/components/reservations/card-reservation/card-reservation.component.html:175-182` — status `InProgress`: Cancel only for the customer.
- `FE/components/reservations/card-reservation/card-reservation.component.html:199-231` — customer, status not Reserved and not Cancelled: "Leave Feedback" or "Update Feedback" (`button--secondary button--large`).
- `FE/components/reservations/card-reservation/card-reservation.component.ts:35-46` — standalone, `ChangeDetectionStrategy.OnPush`, signal `input()`/`output()`, `@app/...` imports.
- `FE/components/reservations/card-reservation/card-reservation.component.ts:73-95` — outputs: `cancelReservation`, `editReservation`, `postponeReservation`, `leaveFeedbackReservation`, `updateFeedbackReservation`, `sendAnonymousFeedback`, `statusChanged`, `preOrder`. No group output.
- `FE/components/reservations/card-reservation/card-reservation.component.ts:153-157` — `onPreOrder()` navigates to `/menu` with `buildPreOrderQueryParams`.
- `FE/components/reservations/card-reservation/card-reservation.component.scss:90-109` — `.actions`: `display:flex; flex-wrap:wrap; justify-content:flex-end; gap:1rem; margin-top:5rem`. At `max-width:837px`: `flex-direction:column; align-items:flex-start; margin-top:3rem`.
- `FE/pages/reservation-dashboard/reservation-dashboard.component.html:15-27` — renders `<app-card-reservation>` and binds `cancelReservation`, `editReservation`, `leaveFeedbackReservation`, `updateFeedbackReservation`.
- `FE/pages/reservation-dashboard/reservation-dashboard.component.html:47-94` — page-level modals opened by signals: `app-make-reservation` (edit), `app-reservation-confirmed`, `app-feedback-window`.
- `FE/pages/reservation-dashboard/reservation-dashboard.component.ts:100-102` — modal flags `showEditReservationModal`, `showEditReservationConfirmed`, `showFeedbackModal`; pattern = page signal + `selectedReservation` signal (`:94-95`).
- `FE/pages/reservation-dashboard/reservation-dashboard.component.ts:474-560` — `loadReservations()`: `GET /reservations` + `GET /orders`, joins `preOrder` per reservation, sets `hasSubmittedPreOrder` when order status is 1 or 3 (`:532-535`).
- `FE/pages/reservation-dashboard/reservation-dashboard.component.scss:17-39` — grid `minmax(17.5rem, 30%)`; at `max-width:1150px` one column `minmax(17.5rem, 100%)`.
- `FE/pages/waiter-dashboard/waiter-dashboard.component.html:76` — the waiter page uses the same card component.

Frontend — group pieces that exist

- `FE/models/group-orders/participant.model.ts:1-23` — `Participant {id, type: 'Owner'|'Guest', name, items: {dish: DishShort, quantity}[], total, isSubmitted}`.
- `FE/models/group-orders/group-sessions-response.model.ts:3-8` — `{participants, total}`; comment in Ukrainian.
- `FE/models/group-orders/group-status.model.ts:1-8` — `{currentParticipants, maxCapacity, status: 'Open'|'Submitted'|'Closed'}` (matches backend code).
- `FE/models/group-orders/owner-session.model.ts:1-4` — `{guestSessionId}`.
- `FE/models/group-orders/submit-group-pre-order.model.ts:1-10` — request `{reservationId}`, response item `{dishId, quantity}`. Comment says path `/group-reservations/{id}/submit` (wrong, see Contracts).
- No `group-orders.service.ts` exists (searched `FE/services/`: not found). No HTTP call to `/guest/groups/...` or `/guest-session-id` exists in app code (grep: only in mocks).
- `FE/shared/helpers/group-reservation.helpers.ts:3-15` — `MIN_GROUP_GUEST_COUNT = 2`, `isGroupReservation`, `toCartOrderMode`.
- `FE/shared/helpers/consolidate-pre-order.ts:19-33` — `consolidatePreOrder(participants)`: sums only `isSubmitted` participants by dish id. Own local `Participant`/`Dish` interfaces (duplicate of the model).
- `FE/shared/mocks/group-orders/group-orders-mock.interceptor.ts:39-87` — DEV-only mock (registered only in dev, `FE/app.config.ts:25`) for ids `demo-reservation`/`demo-group`: invite-token, invites, guest-session-id, sessions GET, status GET, submit POST at `/group-reservations/demo-group/submit` (:73), DELETE session.
- `FE/shared/mocks/group-orders/group-orders.mock-store.ts:163-173` — mock data includes an `Owner` participant in `participants`; `submit()` (:197-213) consolidates submitted participants including the owner.
- `FE/shared/mocks/auth/demo-auth.ts:11-60` — `?demo` URL param logs in a fake owner (dev only, `FE/app.config.ts:17-19`).
- `homework-frontend.md` (repo root) and `frontend/restaurant-frontend-app/homework-tests/` — marathon homework for a route page `/reservations/:id/group-order` with `GroupOrdersService` (`getGroupInvite$`, `getSessions$`, `getOwnerSessionId$`, `getStatus$`, `submit$`, `removeSession$`). This route is not in `FE/app.routes.ts` (grep `group-order`: not found). `package.json` has no `test:hw` script (only `"test": "ng test"`, :22).

Frontend — owner order, invite, reusable pieces

- `FE/services/pre-orders/pre-orders.service.ts:17-108` — `getPreOrders`, `getByReservationId` (`GET /orders/{id}`), `createPreOrder`, `editPreOrderByReservationId` (`PUT /orders/{id}`, empty list → `DELETE`), `submitPreOrderByReservationId`, `unsubmit...`.
- `FE/models/orders/order.model.ts:3-10` — `Order {orderId, reservationAddress, reservationTime, reservationId, orderItems, status: number}`.
- `FE/components/cart/components/not-submitted-order/not-submitted-order.component.ts:44-48,66-75` — group mode CTA is "Save Pre-Order" and emits `saved`; no submit for group owner.
- `FE/components/cart/cart.component.ts:161-162,182-183` — group owner cannot unsubmit or submit from the cart.
- `FE/components/cart/components/not-submitted-order/not-submitted-order.component.html:21-40` — quantity −/+ buttons (local markup, not a shared component).
- `FE/services/reservation/guest-invite.service.ts:14-22` — `getInviteToken$(reservationId)` → `GET /reservations/{id}/invite-token`; `getInvite$(token)` → `GET /guest/invites/{token}`.
- `FE/components/reservations/reservation-invite-modal/reservation-invite-modal.component.ts:51-54,89-101` — invite link format `${origin}/join/${token}`; copy via `navigator.clipboard.writeText` + toast "Link copied!".
- `FE/components/tables/tables/tables.component.html:178` — the only place that opens `app-reservation-invite-modal` (after booking). The Reservations page does not open it.
- `FE/shared/custom-components/modal/modal.component.ts:3-26` / `.html:1-24` — shared `<app-modal>`: `title`, `width` (default 520px), `closeOnBackdrop`, `closed`, Escape closes, `[modal-actions]` slot. Used by 4 modals (invite, review, edit-reservation, guest-name).
- `FE/components/cart/components/cancel-order-modal/cancel-order-modal.component.html:1-35` — a confirm dialog specific to the cart (hard-coded texts). No generic confirm-dialog component (searched `confirm` under `FE`: only `confirm-password`).
- `FE/services/toast/toast.service.ts:9-18` — `error`, `success`, `info`.
- Avatar / initials component: not found (grep `avatar|initials` finds only profile and feedback usages, no shared component).
- `FE/shared/styles/buttons.scss:43-68` — sizes: `--extra-large` 56px, `--large` 40px / 16px 600, `--small` 32px / 14px 500. `:119-144` `--secondary` (green outline). `:146-178` `--tertiary` (underlined text). `:184-254` `icon-button` (40px, `--small` 32px, `--large` 48px, `--outlined`).
- `FE/shared/styles/_modal.scss:1-18` — `.modal-backdrop`, `--bottom`.
- `frontend/restaurant-frontend-app/src/assets/svg/` — icons that exist: `trash.svg`, `copy.svg`, `check-circle.svg`, `clock*.svg`, `calendar*.svg`, `people*.svg`, `minus-green.svg`, `plus-green.svg`, `arrow-left/right/up/down.svg`.

Frontend — auth

- `FE/auth/token.interceptor.ts:20-68` — any URL that contains `/guest/` passes through **without** the Cognito `Authorization` header; errors get guest toasts (401 text: "Session expired. Please re-open the invite link.").
- `FE/auth/token.interceptor.ts:70-80` — other URLs get `Authorization: Bearer <idToken>`.
- `FE/services/guest-sessions/guest-sessions.service.ts:77-79` — guest calls set their own `Authorization: Bearer <sessionToken>` header.

Backend

- `BE/Routing/RouteRegistry.cs:188-201` — guest and group routes (list below).
- `BE/Handlers/GuestSessions/GetGroupSessionsHandler.cs:11-48` → `BE/Services/GuestSessions/GuestSessionService.cs:199-206` — owner check, then guests only.
- `BE/Services/GuestSessions/GuestSessionService.cs:208-244` — `BuildGuestSnapshotAsync`/`ToGuestParticipant`: type always `Guest`, `total` = sum of all items of the session (submitted or not); missing dishes skipped.
- `BE/Services/GuestSessions/GuestSessionService.cs:176-197` — `DeleteGuestSessionAsync`: owner check, group must be `Open`, owner session → 400, returns remaining guests. Does not change `Reservation.GuestCount`.
- `BE/Services/GuestSessions/GuestSessionService.cs:157-173` — `GetGroupReservationStatusAsync`: `currentParticipants` = guests + 1, `maxCapacity` = `Reservation.GuestCount`, `status`. No ordered count.
- `BE/Services/GuestSessions/GuestSessionService.cs:342-379` — `GetOrCreateOwnerSessionIdAsync`: id = `owner-{groupReservationId}`; owner session has empty `Dishes`, no token.
- `BE/Services/GroupReservations/GroupPreOrderService.cs:22-65` — group submit (details in Current behaviour).
- `BE/Services/GroupReservations/GroupReservationService.cs:30-45` — `GET /guest/invites/{token}`: 400/expired if group not `Open`; `GuestLimitExceeded` if `joined + 1 >= GuestCount`.
- `BE/Services/GroupReservations/GroupReservationService.cs:127-148` — invite token: needs `GuestCount >= 2`, reservation `Reserved`, group not `Submitted`/`Closed`. No owner check in this method.
- `BE/Handlers/Reservations/EditReservationHandler.cs:33-109` — `PUT /reservations` (the only way to change `guestCount`).
- `BE/Entities/GuestSession.cs:7-34` — fields: id, groupReservationId, guestName, dishes, tokenHash, isOwnerSession, ownerUserId, isSubmitted. No timestamps, no contact data.
- `BE/Entities/GroupReservation.cs:5-20` — id, reservationId, reservationToken, status. No deadline, no bonus fields.
- `BE/Entities/OrderStatus.cs:3-9` — `Created=0, Submitted=1, Cancelled=2, SubmittedByCustomer=3`.
- `BEdocs/decisions/0002-owner-session-and-group-submit.md` — ADR for owner session and group submit; line 98: "The owner's own dishes stay in the `Order`."

## Current behaviour

### 1. Reservation card buttons (customer = owner)

`GET /reservations` for a customer returns only reservations with `CustomerId = sub` (`BE/Handlers/Reservations/GetReservationsForUserHandler.cs:70,102-106`). So every card on the customer Reservations page is an owner card. There is no "guest" card: guests have no account (anonymous guest sessions).

| Status | Buttons today (customer) | Evidence |
|---|---|---|
| Reserved (0) | Cancel (tertiary), Edit (secondary), Pre-order (primary) | `card-reservation.component.html:127-172` |
| InProgress (2) | Cancel (tertiary), Leave/Update Feedback (secondary) | `:175-182`, `:199-231` |
| Finished (3) | Leave/Update Feedback (secondary) | `:199-231` |
| Cancelled (1) | none | `:199-231` excludes Cancelled |

- No "Invite Guests" button and no "Group Pre-Order" button exist on the card (grep `Invite` in the card: not found). Figma M00 shows a different card design: status header strip ("Upcoming"/"Completed"), trash icon next to the address (Cancel as an icon), "Invite Guests" button, "Pre-orders: 4/10", and "Group Pre-Order" with an orange dot. The current card has a status badge (`:28-34`) and text buttons.
- The card shows no group information (no participant count, no group status).

**Space measurement (estimate from CSS, not measured in a browser).** Button = text width + 2 × 16px padding + 2.8px border, Poppins 16px/600. Estimated widths: Cancel ≈ 90px, Edit ≈ 70px, Pre-order ≈ 115px, Group Pre-Order ≈ 170px. Gaps 16px each.

| Viewport | Card content width | 3 buttons (≈ 307px) | 4 buttons (≈ 493px) |
|---|---|---|---|
| 1440px (card = 30% of ≈ 1392px ≈ 418px, minus 48px padding) | ≈ 370px | 1 row | wraps to 2 rows (right-aligned) |
| 1200px (30% of ≈ 1152 ≈ 346px − 48) | ≈ 298px | already wraps | 2–3 rows |
| ≤ 1150px (1 column, full width) | ≥ 232px | 1 row on wide screens | 1 row from ≈ 540px content |
| ≤ 837px | column layout | buttons stack vertically, left aligned (`.scss:100-108`) | 4 stacked buttons, ≈ 4 × 40px + 3 × 16px = 208px of height |

The grid uses `grid-auto-rows: 1fr` (`reservation-dashboard.component.scss:23`), so one card that wraps makes all rows taller. To confirm the numbers, render the page in a browser.

### 2. Backend API for the owner group view (code wins)

| Route (code) | API GW auth (`deployment_resources.json`) | Owner/guest | Frontend call today |
|---|---|---|---|
| `GET /reservations/{reservationId}/invite-token` (`RouteRegistry.cs:197`) | cognito_authorizer | owner (no owner check in service) | `guest-invite.service.ts:14` |
| `GET /guest/invites/{token}` (`:196`) | NONE | public | `guest-invite.service.ts:20` |
| `GET /reservations/{reservationId}/guest-session-id` (`:198`) | cognito_authorizer | owner | none (mock only) |
| `GET /guest/groups/{groupReservationId}/sessions` (`:200`) | cognito_authorizer | owner | none (mock only) |
| `DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}` (`:199`) | cognito_authorizer | owner | none (mock only) |
| `GET /guest/groups/{groupReservationId}/status` (`:195`) | NONE | public | none (mock only) |
| `POST /guest/groups/{groupReservationId}/submit` (`:201`) | cognito_authorizer | owner | none (mock uses wrong path) |
| `PUT /reservations` (`:174`) | cognito_authorizer | owner/waiter | `reservation.service` `editReservation$` |

Auth evidence: Python dump of `backend/restaurant-backend-app/deployment_resources.json` (command output in this session): `/guest/groups/{groupReservationId}/sessions GET cognito_authorizer`, `.../sessions/{guestSessionId} DELETE cognito_authorizer`, `.../submit POST cognito_authorizer`, `.../status GET NONE`.

**Interceptor effect.** The three owner routes under `/guest/groups/...` (sessions GET, DELETE, submit) contain `/guest/`, so `token.interceptor.ts:23` sends them with no `Authorization` header. API Gateway's Cognito authorizer then returns 401 before the Lambda (`ApiRoutes.md` notes, e.g. line 1312 area). The 401 shows the guest toast "Session expired. Please re-open the invite link." (`token.interceptor.ts:42-44`). `GET /guest/groups/{id}/status` is public, so it works without the header. `/reservations/{id}/guest-session-id` and `/reservations/{id}/invite-token` do not contain the literal substring `/guest/` (`guest-session-id` has `/guest-`), so they get the Cognito header.

**Group sessions list** (`GuestSessionService.cs:199-244`):
- Guests only. The owner session is not in the list (`GuestsOnly()`), type is always `Guest`.
- Per guest: `id`, `name`, `items[{dish (full DishResponse: id, name, price, imageUrl, …), quantity}]`, `total` (sum of all items, submitted or not), `isSubmitted`.
- Top-level `total` = sum of guest totals, including guests with `isSubmitted: false`.
- A guest who joined but added nothing: `items: []`, `total: 0`, `isSubmitted: false`.
- Works in every group status.

**Owner's own order.** The owner's dishes live in the `Order` (`GET /orders/{reservationId}` or `GET /orders`), not in a guest session. The owner session (`owner-{groupId}`) always has empty `Dishes`; it has no token, so no endpoint can add dishes to it (`GuestSessionService.cs:366-379`; ADR 0002 line 98).

**Group status** (`GuestSessionService.cs:157-173`, `GroupReservationStatusResponse.cs:6-17`): `{currentParticipants: guests + 1, maxCapacity: Reservation.GuestCount, status: "Open"|"Submitted"|"Closed"}`. No ordered count, no deadline, no bonus, no submitted time. `ApiRoutes.md:1198-1206` documents `GET /group-reservations/{id}/status` with `totalGuests`/`submittedGuests` — this is wrong vs code.

**Group status transitions.** `Open` → `Submitted` only by group submit. `Closed` only when a reservation update leaves status `Reserved` through `ReservationService.UpdateReservationAsync` (`ReservationService.cs:115-125`, `GroupReservationService.cs:116-123`). No time-based close (no deadline logic; grep `deadline|bonus|discount|remind`: not found in backend C#).

**Changing seats.** No dedicated endpoint. `PUT /reservations` with `{reservationId, startTime, endTime, guestCount}` changes `guestCount` (`EditReservationHandler.cs:90-92`). Checks: start ≥ 15 min ahead (`:63-67`), end > start, `table.Capacity >= guestCount` (`:80-83`), no overlap (`:85-88`). No check against joined guests, no lower bound (1 or 0 is accepted), and the handler does not call `ReservationService.UpdateReservationAsync`, so it does not create or close the group reservation. The invite limit uses `Reservation.GuestCount` (`GroupReservationService.cs:44`, `GuestSessionService.cs:61`), so fewer seats block new joins.

**DELETE guest session** (`GuestSessionService.cs:176-197`): owner only, group must be `Open`, owner session → 400 `OwnerSessionNotDeletable`, returns remaining guests `{participants, total}`. It does not reduce `guestCount` (Figma M04 text "her seat removed" needs a separate `PUT /reservations`).

**Group submit** (`GroupPreOrderService.cs:22-65`), checks in order: reservation exists (404) → caller is owner (403) → group exists and belongs to the reservation (404) → group `Open` (400 Submitted/Expired) → at least one session with `isSubmitted` (400) → submitted sessions have dishes (400) → a non-cancelled `Order` exists (400 "Active pre-order for reservation … not found"). Then it adds the consolidated guest dishes to the owner's `Order` items (`MergeIntoOrderItems`, `:104-120`), sets order status `SubmittedByCustomer`, and sets group `Submitted` in one transaction. Response = consolidated guest dishes only.
- **Duplication of the owner's order:** the owner's own items are already in the `Order`; the submit adds only session dishes. The owner session has no dishes, so the owner's dishes are not added a second time. Same dish ordered by owner and guest → quantities are summed in one order item (`:115`).
- **Consequences for Figma states:** M06 (only the owner ordered, no submitted guest) → submit returns 400 "No submitted guest sessions…". M07 (owner has no order) → submit returns 400 "Active pre-order … not found". M09 "Confirm anyway" works only if at least one guest submitted.
- Guests with items but `isSubmitted: false` are not in the submit.

**Invite link for "Copy pre-order link".** `GET /reservations/{id}/invite-token` → `{token}`; link = `${origin}/join/${token}` (`reservation-invite-modal.component.ts:51-54`). Fails after submit (400 Submitted) or when the reservation is not `Reserved` (`GroupReservationService.cs:135-144`).

**Getting `groupReservationId` from a reservation.** The reservation DTO has no `groupReservationId` (grep in `BE/Models/Reservations/` and `ReservationMapper.cs`: not found; FE `reservation.model.ts:31-50` has none). Two paths exist: (a) `GET /reservations/{id}/guest-session-id` returns `owner-{groupReservationId}` (`ApiRoutes.md:570` documents the format; works in every status; creates the owner session on first call); (b) invite-token → `GET /guest/invites/{token}` → `GroupReservationId`, which fails when the group is not `Open` or is full (`GroupReservationService.cs:35-45`).

**Reminders.** No endpoint (grep `remind`: not found). `GuestSession` stores only `guestName` — no e-mail/phone (`GuestSession.cs:7-34`). `GET /notifications` is the waiter notification list (`GetWaiterNotificationsHandler.cs`).

**Bonus rules.** Not on the backend (grep `discount|bonus`: not found). The order has no discount field. `GuestSession` has no submit timestamp, so "last pre-order ≥ 24 h / ≥ 2 h before" cannot be computed by anyone today.

### 3. Frontend group features today

- No owner group UI on the Reservations page. The previous task made the header cart show "Save Pre-Order" for group owners (`not-submitted-order.component.ts:44-46`), so today a group owner has **no way to submit** a pre-order from the UI.
- Mock backend covers the homework flow only for `demo-reservation` in dev. Mock differs from the real API: mock `participants` include the `Owner` (`group-orders.mock-store.ts:165`), real API excludes it; mock submit path is `/group-reservations/{id}/submit` (`group-orders-mock.interceptor.ts:73`), real is `/guest/groups/{id}/submit`; mock submit counts owner items, real submit adds only guest-session dishes to the existing order.
- Several mock/model files have Ukrainian comments (`group-sessions-response.model.ts:6`, `group-orders.mock-store.ts:130-131,138`, `group-orders-mock.interceptor.ts:35-37,81`, `demo-auth.ts:6-9`, `app.config.ts:16,24`, `consolidate-pre-order.ts:25`) — root `CLAUDE.md` "Language" rule says English.

### 4. Figma behaviours vs backend

| Figma behaviour | Status | Evidence |
|---|---|---|
| Guest list with dishes, qty, price, subtotal (M01/M02) | supported | `GET /guest/groups/{id}/sessions`; but blocked by interceptor (401) until fixed |
| Owner's own order row (M01/M02/M07) | supported (via `/orders`) | owner dishes in `Order`; `PreOrdersService.getByReservationId` |
| Edit own dishes in the modal (M02: −/+, trash) | supported | `PUT /orders/{reservationId}` (`pre-orders.service.ts:73-84`); 30-minute rule `OrderService.cs:299` |
| "4 of 6 ordered" | partly | seats = `maxCapacity` (status) or `guestCount`; ordered count must be derived on the client from `isSubmitted` + owner order |
| "Waiting" for joined guests without order (AC3) | partly | `isSubmitted:false` / `items:[]`; empty seats = `guestCount − 1 − guests` derived on client |
| Copy pre-order link (M01/M03b) | supported | `GET /reservations/{id}/invite-token`; fails after submit |
| Seats stepper (M01/M03) | partly | only `PUT /reservations` with full body; no check vs joined guests; no group create/close |
| Remove guest (M04) | partly | `DELETE …/sessions/{guestSessionId}` deletes the guest; "seat removed" needs a separate `PUT /reservations`; blocked by interceptor (401) |
| Submit pre-order (M01/M09/M10) | partly | `POST /guest/groups/{id}/submit`; 400 when no submitted guest or no owner order; blocked by interceptor (401) |
| "Sent to Restaurant" state (M10) | supported | `GET /guest/groups/{id}/status` → `Submitted` (public) |
| Bonus 10% (M01/M05) | not supported | no backend discount; no submit timestamps |
| Free dessert tier (US 2 AC1) | not supported | none in backend or design |
| Deadline / "time left" (M01/M11) | not supported on backend | no field; client can only compute from `startTime` |
| Reminder to guest (US 2 AC2) | not supported | no endpoint, no contact data |
| Add to calendar (M10) | not supported on backend | client-only possible; no `.ics`/calendar code in FE (grep: not found) |
| Notification dot on the button (M00) | not supported | no "new guest order" signal for customers; would need polling sessions per card |
| Large group slider (M08) | client-only | n/a |

## Contracts touched

- API routes (code): `GET /reservations/{id}/guest-session-id`, `GET /guest/groups/{id}/sessions`, `DELETE /guest/groups/{id}/sessions/{guestSessionId}`, `GET /guest/groups/{id}/status`, `POST /guest/groups/{id}/submit`, `GET /reservations/{id}/invite-token`, `GET/PUT/DELETE /orders/{reservationId}`, possibly `PUT /reservations`.
- `ApiRoutes.md` drift vs code: status route path and fields (`:1198-1206` vs `RouteRegistry.cs:195` + `GroupReservationStatusResponse.cs`); `/guest-sessions/...` dish routes (`:1139,1154`) vs `/guest/sessions/...` (`RouteRegistry.cs:191-192`). Already in FE tech-debt 2.4.
- FE models: `FE/models/group-orders/*` (exist; `submit-group-pre-order.model.ts:1,6` comment path wrong). `Participant.dish` = `DishShort {id,name,price,imageUrl?,weight?}` — subset of backend `DishResponse`.
- `FE/auth/token.interceptor.ts:23` — `/guest/` exclusion (security-relevant).
- AWS resources / env variables: none needed for supported features. Any backend for bonus/reminder/seat endpoint would need a backend task and `syndicate deploy`.

## Existing tests

- `FE/components/reservations/card-reservation/card-reservation.component.spec.ts:36` — only "should create".
- `FE/pages/reservation-dashboard/reservation-dashboard.component.spec.ts:81-96` — cart refresh after cancel, spinner, empty state.
- `FE/components/reservations/reservation-invite-modal/reservation-invite-modal.component.spec.ts:60-154` — 10 tests: token load, copy link, toasts, navigation.
- `FE/shared/custom-components/modal/modal.component.spec.ts:33-66` — 6 tests of `<app-modal>`.
- `FE/auth/token.interceptor.spec.ts:40,50` — 2 tests (400 toast, SKIP_ERROR_TOAST). No test for the `/guest/` branch.
- `FE/shared/helpers/group-reservation.helpers.spec.ts`, `FE/shared/helpers/consolidate-pre-order.spec.ts`, `FE/shared/mocks/group-orders/group-orders.mock-store.spec.ts` (5 tests), `FE/shared/mocks/auth/demo-auth.spec.ts`.
- `frontend/restaurant-frontend-app/homework-tests/**` — 8 spec files for a page/service design (not in `src/`, not run by `npm test`).
- Backend: `Tests/Handlers/{DeleteGuestSession,GetGroupSessions,GetOwnerSessionId,SubmitGroupPreOrder}HandlerTests.cs`, `Tests/Services/{GroupPreOrder,GroupReservation,GuestSession}ServiceTests.cs`. Not run: `dotnet` is not installed in this environment (`timeout: failed to run command 'dotnet': No such file or directory`).

**Frontend baseline (run in this session, from `frontend/restaurant-frontend-app`, `node_modules` present so no `npm ci`):**

- `npx tsc -p tsconfig.app.json --noEmit` → exit 0.
- `npx tsc -p tsconfig.spec.json --noEmit` → exit 0.
- `npm test -- --watch=false --browsers=ChromeHeadless` → `Executed 364 of 364`, `TOTAL: 364 SUCCESS`, 0 failed. This differs from the 46 failed in `frontend/restaurant-frontend-app/docs/tech-debt.md` 2.1 and the orchestrator's note.

## Constraints and risks

- **Interceptor (security-relevant).** Owner calls under `/guest/groups/...` get no Cognito header → 401. Any change must not add the Cognito token to real guest-session calls (which set their own `Authorization`, `guest-sessions.service.ts:77-79`) and must not change `POST /guest/sessions` (comment `token.interceptor.ts:20-22`).
- **Current vs target conventions.** App `CLAUDE.md` §5 says Eager CD and relative imports; the card and page use `OnPush`, signal `input()/output()` and `@app/...` imports (`tsconfig.json` `paths` `@app/*`). FE tech-debt 2.3 records this. Follow the code.
- **Card is shared with the waiter page** (`waiter-dashboard.component.html:76`). A layout change to `.actions` affects waiter cards too.
- **`grid-auto-rows: 1fr`** makes every card as tall as the tallest one.
- **Bundle budget** ≈ 84 kB headroom (app `CLAUDE.md` §8). No new npm packages without approval (calendar/clipboard libs).
- **Business rules missing on backend** (bonus, deadline, reminders, free dessert). Any client-only bonus display would not change the price the restaurant gets.
- **Submit preconditions** (≥1 submitted guest with dishes; owner order exists) make some Figma states end in 400.
- **Owner order editing** is blocked within 30 min of start (`OrderService.cs:299`) and when the reservation is not `Reserved` (`:141-142`).
- **Seat change via `PUT /reservations`**: 15-min rule, capacity, overlap 409; accepts values below joined guests; does not sync group reservation.
- **Mock divergence** (owner in participants, submit path) can make dev-mode behaviour differ from the real API.
- **Deploy-only checks:** API Gateway Cognito authorizer on the `/guest/groups/...` owner routes, owner session creation, the submit transaction (ADR 0002 "Deploy checks").
- `GET /reservations/{id}/guest-session-id` writes an owner session on the first call (side effect of a GET).

## Open questions

Blocking:

- [blocking] Bonus scope: the backend has no discount, deadline or free-dessert logic, and no submit timestamps. Show the bonus block (progress, "10% off", deadline) as client-only display, hide it, or open a backend task? A client-only discount does not change the restaurant's order total.
- [blocking] Seats stepper and "seat removed" after Remove Guest: implement through `PUT /reservations` (full body, no check vs joined guests, no group sync), leave out, or ask for a backend endpoint?
- [blocking] Reminder (US 2 AC2) and "Add to calendar" (M10): out of scope? No backend support; calendar would be client-only (`.ics` download or a link) without a new package.
- [blocking] Interceptor fix approach: the change touches auth code. Confirm that the owner routes under `/guest/groups/` may get the Cognito header (only these routes, not `/guest/sessions/...` or `/guest/invites/...`).
- [blocking] Card layout for 4 buttons: follow M00 (Cancel as trash icon, new card design) or keep the current card and only re-arrange buttons? Which statuses show "Group Pre-Order" (Reserved only, or also InProgress/Finished read-only)?

Non-blocking (default in brackets):

- [non-blocking] "Ordered" meaning for a guest: `isSubmitted === true` or "has items"? [default: `isSubmitted === true`; guests with items but not submitted show as "Waiting", because the submit ignores them.]
- [non-blocking] Grand Total: own order + submitted guests only, or also unsubmitted carts (API `total`)? [default: own order + submitted guest subtotals, matching what the submit sends.]
- [non-blocking] Owner "ordered" state: owner `Order` exists and is not cancelled. [default: yes, `orderItems.length > 0`.]
- [non-blocking] Notification dot on the button (M00): no backend signal. [default: leave out.]
- [non-blocking] How to get `groupReservationId`: from `guest-session-id` (`owner-` prefix, works in every status) or from invite-token → invites (fails when full/submitted). [default: `guest-session-id`, strip the documented `owner-` prefix.]
- [non-blocking] Submit when no guest submitted, or owner has no order (backend 400): disable the button with a hint, or call and show the API toast? [default: disable with a hint.]
- [non-blocking] Update the dev mock (owner in participants, submit path) to match the real API? [default: yes, if the modal uses the mock in dev.]
- [non-blocking] Show "Group Pre-Order" only for `guestCount >= MIN_GROUP_GUEST_COUNT`. [default: yes.]
- [non-blocking] FE tech-debt 2.1 says 46 failing tests; today's run shows 0 failed. Update the baseline note. [default: orchestrator records 364/0 as the new baseline.]
