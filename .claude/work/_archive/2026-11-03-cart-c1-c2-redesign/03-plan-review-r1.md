# Plan review r1: Cart redesign, screens C1 and C2

Reviewed: `02-plan-r1.md` against `00-task.md`, `figma/c2-cart.png` (not opened; the task text describes it), the frontend and backend code, `ApiRoutes.md` and `RouteRegistry.cs`.

## Verdict
changes required

One blocker (step 5, conditional step) and two major findings. Steps 1-4 are sound and close to ready.

## Findings

### [blocker] Step 5a/5b — `GET /guest/groups/{id}/sessions` is sent without the Cognito token
- Problem: `tokenInterceptor` skips every URL that contains `/guest/`. It does not add the `Authorization` header to those requests, and it shows guest-flow toasts on errors. The owner endpoint `GET /guest/groups/{groupReservationId}/sessions` needs the owner's Cognito ID token (API Gateway Cognito authorizer). In production the request gets 401 from API Gateway, and the user sees the toast "Session expired. Please re-open the invite link." on every cart open with a group order. The planned specs use `HttpTestingController` without the interceptor, so they pass and do not show the defect. The dev mock interceptor runs after `tokenInterceptor` and serves only `demo-reservation`, so a manual dev check also hides it.
- Evidence: `frontend/restaurant-frontend-app/src/app/auth/token.interceptor.ts:23-63` (`if (req.url.includes('/guest/')) { return next(req)... }`, 401 toast text); `backend/restaurant-backend-app/docs/ApiRoutes.md:1261-1262` ("Auth: Cognito ID token. The caller must be the owner"), `:1300`; `app.config.ts` interceptor order `[tokenInterceptor, ...mock]`.
- Required change: step 5 must say how the owner token reaches `/guest/groups/...` requests. For example, narrow the interceptor bypass to the guest-session endpoints (`/guest/sessions`, `POST /guest/...` join), or add an explicit `HttpContext` flag that the owner calls set. That is a change in `auth/token.interceptor.ts`: add it to the step files, name its tests (owner `/guest/groups/...` request gets `Authorization: Bearer <idToken>`; a guest-session request is still not changed), and list it under risks (the guest flow must not break, see the comment at `token.interceptor.ts:20-22`). If the user picks Q3 (b) (skip step 5), this finding is not applicable, but the plan must still record it next to Q2/Q3 so that the follow-up does not repeat it.

### [major] Q1 + Q3 defaults together leave a group owner with no way to submit
- Problem: The recommended defaults are Q1 (b) "Save = toast + close, no API call" and Q3 (a) "no View group pre-order link". The plan itself states that no frontend calls `POST /guest/groups/{id}/submit` and that no owner group page exists (F5). With both defaults, an owner of a reservation with `guestCount >= 2` sees only "Save Pre-Order", which does nothing on the server. The order stays status 0 forever, and the owner has no UI path to submit it. Today the same owner can submit with "Submit Pre-order". This is a functional regression, and the plan does not say it. The risk line "Q1 (b) risk" talks only about user expectation.
- Evidence: `02-plan-r1.md` F5, F6, Q1, Q3; `cart.component.ts` `submitOrder` is the only submit path; no caller of `/guest/groups/{id}/submit` in `src` (grep).
- Required change: State the combined effect in Q1 and in "Contracts and risks", so that the user decides with it in view. Give the user a concrete option, for example: Q1 (a) (Save calls the same submit as C1) until the group page exists, or (b) only together with Q3 (c) / a follow-up task. Record the follow-up in the plan's tech-debt items. The recommended default must not remove the only submit path without the user's explicit approval.

### [major] Step 5b — "any error → no toast" is not possible with the current interceptor
- Problem: The contract says an error for one reservation gives "no entry, no toast". `GET /reservations/{id}/guest-session-id` is not a `/guest/` URL and not an `orders` URL, so `tokenInterceptor` shows a global error toast for 403/404/500 (for example 404 "Group reservation was not found" for a reservation with `guestCount >= 2` that has no group item). The only opt-out is `SKIP_ERROR_TOAST` in the request `HttpContext`. The plan's service contract does not set it. The same applies to the step 3 `GET /reservations` fallback: a failure shows a global toast, which the plan does not mention (acceptable, but say it).
- Evidence: `token.interceptor.ts:83-121` (only `orders` 404 and `SKIP_ERROR_TOAST` skip the toast); `auth/skip-error-toast.context.ts`; `reservation.service.ts:55` (existing use of the flag); `ApiRoutes.md:579-580` (404 cases).
- Required change: In 5a, the `GroupOrdersService` methods send `SKIP_ERROR_TOAST = true` (or take a parameter, like `makeReservation$`), and the service spec asserts `req.request.context.get(SKIP_ERROR_TOAST)` is true. In step 3, state whether the `GET /reservations` failure shows the global toast (today's `getReservationsForUser$` has no flag) and accept it or change it explicitly.

### [minor] Step 3 — dead code after "Add dish" removal
- Problem: Step 3 removes the `(addDish)` binding but does not remove `CartComponent.addDishToOrder` (and its `console.log` and `/menu` navigation). With Q4 default it has no caller. Note: an `(addDish)` binding to a removed output does not break the build (Angular treats it as a DOM event), so between step 2 and step 3 the build stays green, but the binding is silent dead code until step 3.
- Evidence: `cart.component.ts` `addDishToOrder`; `cart.component.html:46`.
- Required change: Step 3 removes `addDishToOrder` from `CartComponent` when Q4 removes the button (subtract before add).

### [minor] Step 3 — loading state has no test
- Problem: The contract says `isLoadingOrders` stays true until both requests complete. No test asserts it. A `forkJoin` written without it, or a `finalize` on only one stream, passes all listed tests.
- Evidence: `02-plan-r1.md` step 3 contract vs tests; frontend `CLAUDE.md` §6 "empty/loading/error states".
- Required change: Add one test: with `getPreOrders` emitted and `getReservationsForUser$` still pending (a `Subject`), the spinner shows and no `.cart-cta` renders; after the reservations emit, the CTA renders.

### [minor] Step 2/4 — sticky per-order footer vs the Figma panel footer
- Problem: Figma has one footer at the panel bottom, full width, with border-top `#F7F7F7` and padding 24px 40px. The plan puts a sticky footer inside each order, inside the body that has padding 24px 40px. The border-top then does not reach the panel edges, and the sticky bottom offset depends on the body padding. With two unsubmitted orders, two footers exist. This is a reasonable decision for multiple orders, but the plan does not say how it matches the design for the single-order case.
- Evidence: `00-task.md:42` (footer padding and border); step 2 "Footer: position: sticky; bottom: 0"; step 4 body padding.
- Required change: State the intended layout for one order (for example negative horizontal margin / padding on the sticky footer so the border spans the panel, and `bottom` that cancels the body bottom padding), and keep the manual check in step 4 against `figma/c2-cart.png` for that case.

### [minor] Step 3 — extra latency of `GET /reservations` on every cart open
- Problem: For a customer, the backend reads each reservation again and then its location, order and group (`GetReservationsForUserHandler.cs:98-148`). Cart load now waits for this. The plan lists "one extra request" but not that the cart spinner waits for it.
- Evidence: `GetReservationsForUserHandler.cs:104-110,129-140`.
- Required change: Add it to the "Extra request" risk line. No design change needed: waiting is safer than showing C1 and then switching to C2.

### [minor] F7 — chevron colour
- Problem: `arrow-right.svg` has stroke `#232323`; the link next to it is `#0F7A37`. If the Figma chevron is green, reuse needs a CSS filter or a different asset. Moot while Q3 (a) hides the link.
- Evidence: `src/assets/svg/arrow-right.svg`.
- Required change: Note it under Q3 for the future link. No change for this task if Q3 (a).

## Open questions check
- Q1: real (no "save" endpoint; checked `RouteRegistry.cs:177-185`). Default needs the change in the major finding above.
- Q2: real. Backend evidence supports the default Y = `guestCount - 1`: `GroupReservationService.cs:44` closes the group when `joinedGuestsCount + 1 >= reservation.GuestCount` (the owner counts as one). Add this evidence to Q2.
- Q3: real (no route in `app.routes.ts`, no frontend caller). Default (a) is sound only with the Q1 caveat.
- Q4: real (cart can hold several status-0 orders; `cart.component.html:42`). Default is sound.

## Confirmed
- Header cart is the target: `header.component.html:63,250` render `<app-cart>`; guest drawer is under `join/:token` (`app.routes.ts:98-103`).
- `Order` has `reservationId` and no guest count (`models/orders/order.model.ts`). `Reservation.guestCount: number` and `id` exist (`models/reservations/reservation.model.ts:4-10`). `ReservationService.getReservationsForUser$()` calls `GET /reservations` (`reservation.service.ts:81-84`). Backend `ReservationResponse.GuestCount` exists (`Models/Reservations/ReservationResponse.cs:22`); `ApiRoutes.md:469-486` documents `guestCount`. The join by `reservationId` uses existing fields only. No invented API contract in steps 1-5.
- Group threshold 2: `ReservationService.cs:72,119`, `GroupReservationService.cs:132`, `reservation-invite-modal.component.ts:20`.
- Routes `RouteRegistry.cs:195,198,200`; `ApiRoutes.md:557-583` (`owner-` prefix at `:566`, side effect note), `:1302` (guest sessions only); `ApiRoutes.md:1191` doc drift is real.
- Models `models/group-orders/*` exist (`OwnerSessionResponse`, `GroupSessionsResponse`, `Participant.type`); the mock lists an `Owner` participant (`group-orders.mock-store.ts:43`). Filter on `type === 'Guest'` is correct.
- `GroupPreOrderService.cs:53-57` sets `SubmittedByCustomer` on the order.
- `not-submitted-order` current inputs/outputs, `cart.component.ts` flows, 5 tests in `cart.component.spec.ts` and 3 in `not-submitted-order.component.spec.ts` match the plan.
- `ToastService.success(message, title?)` exists. Colour tokens `$neutral-0/100/200/400/600`, `$green-600` exist in `shared/styles/abstracts/_colors.scss`; both SCSS files already `@use` it.
- Style budget 4 kB / 10 kB (`angular.json:52-54`). The four gates match frontend `CLAUDE.md` §6. `npm ci` requirement is correct.
- Deviation from `CLAUDE.md` §5 (OnPush, signal API, `@app/*` paths) is correctly identified: `tsconfig.json:7-9` has the path, the cart files use them. Following the code is correct (precedence 5).
- Test cases of steps 1, 2, 3 and 5 are concrete (inputs, selectors, expected text and emitted values). Step 4 has no tests with a stated reason.
