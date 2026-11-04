# Frontend tech debt: cart

Status: open. Recorded on 2026-11-03 during the cart redesign for the Figma screens C1 and C2 (task folder `.claude/work/2026-11-03-cart-c1-c2-redesign/`). The redesign did not fix these items.

Paths below are relative to `frontend/restaurant-frontend-app/`.

Priority:

- **P1**: a wrong status code, a 500 that a client can trigger, or a security issue.
- **P2**: wrong or missing documentation, or a code smell.

## 1. Cart

### 1.1 The "-" quantity button looks disabled when it works (P2)

- **Where:** `src/app/components/cart/components/not-submitted-order/not-submitted-order.component.scss` (decrease button).
- **Problem:** the "-" button always uses the grey border and text from Figma. At a quantity of 2 or more the button works, but it looks the same as the disabled state at quantity 1.
- **Fix:** get a design decision. Then give the enabled "-" button its own style, for example the green style of the "+" button.

### 1.2 The "unknown" order message is not always true (P2)

- **Where:** `src/app/components/cart/components/not-submitted-order/not-submitted-order.component.html` (`.cta-unavailable`).
- **Problem:** the message says "Reopen the cart to try again". This text is true when `GET /reservations` fails. It is false when the response does not contain the reservation of the order. A new try gives the same result.
- **Fix:** show a different message for each case, or use a neutral message.

### 1.3 Errors only go to the console (P2)

- **Where:** `src/app/components/cart/cart.component.ts` (`cancelOrder`, `editOrder`, `submitOrder`, `updateDishQuantity`, `removeDishFromOrder`).
- **Problem:** these methods only call `console.error` when a request fails. The user sees no message from the cart. The global interceptor shows a toast for some status codes only.
- **Fix:** show an error toast or an inline message in the cart.

### 1.4 Unused `.list` style (P2)

- **Where:** `src/app/components/cart/cart.component.scss` (`.list`).
- **Problem:** no template uses the `.list` class. The class existed before the redesign.
- **Fix:** remove the rule.

### 1.5 Two constants for the group size (P2)

- **Where:** `src/app/shared/helpers/group-reservation.helpers.ts` (`MIN_GROUP_GUEST_COUNT = 2`) and `reservation-invite-modal.component.ts:20` (`MIN_GUESTS_FOR_INVITE = 2`).
- **Problem:** the same rule has two constants. A change to one constant does not change the other.
- **Fix:** use `MIN_GROUP_GUEST_COUNT` in the invite modal.

## 2. Tests and documentation

### 2.1 Unit test baseline is 0 failed (fixed)

- **Where:** the Karma suite.
- **Fact:** `npm test -- --watch=false --browsers=ChromeHeadless` gave 0 failed of 364 tests on 2026-11-04. The earlier 46 failures no longer occur.
- **Fix:** none needed. Keep 0 failed as the baseline.

### 2.2 `npm test` cannot start Chrome with no screen (P2)

- **Where:** `karma.conf.js` or the test command in `CLAUDE.md` §6.
- **Problem:** in a container with no screen, `npm test -- --watch=false` runs no tests. The tests run only with `--browsers=ChromeHeadless`.
- **Fix:** add the headless browser to the documented test command, or add a CI launcher to the Karma configuration.

### 2.3 `CLAUDE.md` §4 and §5 do not agree with the code (P2)

- **Where:** `CLAUDE.md` §4 and §5.
- **Problem:** the file describes eager loading, `@Input`, `app/...` imports and an old `cart/` location. The cart code uses OnPush, signal `input()` and `output()`, and `@app/...` imports.
- **Fix:** update §4 and §5 to agree with the code.

### 2.4 `docs/ApiRoutes.md` does not agree with the backend code (P2)

- **Where:** `backend/restaurant-backend-app/docs/ApiRoutes.md`.
- **Problem:** the group status section now agrees with the code. Lines 1132 and 1147 use `/guest-sessions/...`, but the code uses `/guest/sessions/...`. The document shows the order `status` as a string, but the frontend `Order.status` is a number.
- **Fix:** copy the paths, fields and types from the code into `ApiRoutes.md`.

## 3. Group Pre-Order modal

Recorded on 2026-11-04 during the Group Pre-Order modal task (task folder `.claude/work/2026-11-04-group-preorder-modal/`).

### 3.1 The confirm dialog has no focus control (P2)

- **Where:** `src/app/shared/custom-components/confirm-dialog/`.
- **Problem:** the dialog does not move the focus into the dialog and does not keep the focus there. It has no `aria-describedby` for the message.
- **Fix:** move the focus to the first button when the dialog opens, trap the focus, and set `aria-describedby` on the dialog.

### 3.2 "Copy pre-order link" has one error path for two errors (P2)

- **Where:** `group-pre-order-modal.component.ts` (`copyLink`).
- **Problem:** the same handler catches a failed token request and a failed clipboard write. Two fast clicks reset the "Link copied" label too early.
- **Fix:** give each error its own message. Cancel the old timer before a new copy starts.

### 3.3 The modal stylesheet is over the style budget (P2)

- **Where:** `group-pre-order-modal.component.scss` (6.28 kB on 2026-11-04 after the responsive layout, warning at 4 kB, error at 10 kB).
- **Problem:** the build prints a style-budget warning for this file.
- **Fix:** move the seats row and the participant row into child components with their own stylesheets.

### 3.4 The modal has few tests for error paths (P2)

- **Where:** `group-pre-order-modal.component.spec.ts`.
- **Problem:** some paths have no tests: a clipboard failure, a submit error, Escape while a dialog is open, "−" at quantity 1. Also: the last own dish removed, and most bonus texts. The user asked for lean tests in this task.
- **Fix:** add tests when one of these paths changes.

### 3.5 Small logic in the modal template (P2)

- **Where:** `group-pre-order-modal.component.html` (bonus icon choice, line price, guest initials).
- **Problem:** the template calculates these values. `CLAUDE.md` §7 asks for no logic in templates.
- **Fix:** compute them in the view helper (`group-pre-order.helpers.ts`).

### 3.6 Owner edit drops dishes that no longer exist (P2)

- **Where:** `group-pre-order-modal.component.ts` (owner edit), backend sessions snapshot.
- **Problem:** the snapshot skips dishes that the dish table does not have. An owner edit sends the list without them, so the order loses them.
- **Fix:** keep unknown dishes in the snapshot with a flag, or keep them in the edit request.

## 4. Group bonus on the waiter pages

Recorded on 2026-11-04 (task folder `.claude/work/2026-11-04-group-bonus-and-edit-group/`).

### 4.1 The waiter page computes the bill in three places (P2)

- **Where:** `pages/waiter-pre-order-page/waiter-pre-order-page.component.ts`.
- **Problem:** the component computes the bill again after load, confirm and reject.
- **Fix:** use one `computed()` signal or one `refreshBill()` method.

### 4.2 Item states are magic numbers (P2)

- **Where:** `shared/helpers/order-bill.helpers.ts`, `submitted-order` component, waiter pre-order page.
- **Problem:** the code uses `2` and other numbers for the item state.
- **Fix:** add an `OrderItemState` enum that agrees with the backend `Entities/OrderItemState.cs`.

### 4.3 The "Group Pre-Order" button does not read `guestInviteLinkAvailable` (P2)

- **Where:** `components/reservations/card-reservation/`.
- **Problem:** the button shows from the guest count only. It can show for a reservation that has no group.
- **Fix:** show the button only when `guestInviteLinkAvailable` is true.

### 4.4 `console.log` lines on the waiter page (P2)

- **Where:** `pages/waiter-pre-order-page/waiter-pre-order-page.component.ts`.
- **Problem:** debug output stays in production code.
- **Fix:** remove the lines.

### 4.5 One Karma run disconnected (P2)

- **Where:** `npm test -- --watch=false --browsers=ChromeHeadless`.
- **Problem:** one run printed "Executed 430 of 430" and then "DISCONNECTED (no message in 30000 ms)", with an HTTP request stack trace. The next runs passed (435 of 435).
- **Fix:** if it occurs again, find the spec that leaves an open request, or increase `browserNoActivityTimeout`.

## 5. Guest pre-order flow

Recorded on 2026-11-04 (task folder `.claude/work/2026-11-04-guest-resubmit-seat/`).

### 5.1 A reload after submit takes a new seat (P2)

- **Where:** `pages/guest-pre-order-page/state/guest-preorder.store.ts` (`clearStorage`, `join`), backend `GuestSessionService.CreateGuestSessionAsync`.
- **Problem:** after a submit, the store removes the guest name but keeps the session id and token. A reload shows the name modal again, and `join()` makes a second session with the same name. Duplicate submitted sessions also increase the ordered count, so the group bonus can show as achieved.
- **Fix:** on load, read the stored session with `GET /guest/sessions/{id}` and show the submitted screen if `isSubmitted` is true. As a second guard, the backend can reject a second session with the same name in the group.

### 5.2 The store does not reset `status` for a new invite (P2)

- **Where:** `guest-preorder.store.ts` (`initContext`).
- **Problem:** Angular provides the store in root. If a guest opens a second invite in the same tab after a submit, the old submitted screen shows.
- **Fix:** reset `status` and the cart in `initContext` when the invite token changes.

### 5.3 A joined guest of a full group gets a general error on reload (P2)

- **Where:** `guest-preorder.store.ts` (invite load error handling).
- **Problem:** `GET /guest/invites/{token}` returns 400 `GuestLimitExceeded` when the group is full. The store maps only 404 and 410 to `expired`, so the guest sees "We couldn't load the invitation".
- **Fix:** show a clear message for 400 `GuestLimitExceeded`, or let a guest with a stored session skip the invite request.

### 5.4 The guest store writes the guest name to the console (P2)

- **Where:** `guest-preorder.store.ts` (`console.log` of the join payload and of errors).
- **Problem:** personal data goes to the browser console.
- **Fix:** remove the `console.log` lines.

### 5.5 `ReservationsPageComponent` specs fail late in the day (P2)

- **Where:** `components/tables/tables/tables.component.spec.ts` (line 66 and the "loading state" specs).
- **Problem:** the specs use the real clock. At 21:16 local time, 7 specs failed in 2 runs. The error was "Expected 0 to be greater than 0" for the time options of the first available date. Earlier in the day they pass.
- **Fix:** set a fixed clock in the specs (`jasmine.clock().mockDate`). Also make sure that the component moves to the next day when today has no free time slots.

### 5.6 `CLAUDE.md` §5 has old wording (P2)

- **Where:** `frontend/restaurant-frontend-app/CLAUDE.md` §5.
- **Problem:** the text about `@app/` paths and `Eager` does not agree with the code.
- **Fix:** update the text to agree with the code.

## 6. Group Pre-Order modal on narrow screens

Recorded on 2026-11-04 (task folder `.claude/work/2026-11-04-group-modal-ui/`).

### 6.1 The Submit button scrolls with the content on phones (P2)

- **Where:** `group-pre-order-modal.component.scss` (below 560 px).
- **Problem:** the whole modal card scrolls, so the owner must scroll to the end to see Grand Total and Submit.
- **Fix:** make the footer sticky at the bottom of the card on narrow screens.

### 6.2 The seats row has no arrows on phones (P2)

- **Where:** `group-pre-order-modal.component.ts` (`SEATS_WITHOUT_SCROLL = 7`).
- **Problem:** the value assumes an 800 px modal. On a phone the seats row overflows with no arrows. Touch scroll still works.
- **Fix:** show the arrows when the row overflows, from a measured width.

### 6.3 Interfaces in the modal component file (P2)

- **Where:** `group-pre-order-modal.component.ts` (`DialogView`, `BonusBox`, `OwnItem`).
- **Problem:** `CLAUDE.md` asks for no interfaces in a component file. Only `BreakdownRow` is in `models.ts`.
- **Fix:** move the three interfaces to `models.ts`.

## 7. Dish cards and the menu filter

Recorded on 2026-11-04 (task folder `.claude/work/2026-11-04-dishes-menu/`).

### 7.1 The dish modal does not manage focus (P2)

- **Where:** `app-dish-modal`, used by the main page (popular dishes) and the menu page.
- **Problem:** when the modal opens, focus stays on the card behind it. When the modal closes, focus does not go back to the card. A keyboard user cannot easily reach the modal.
- **Fix:** in the shared modal, move focus into the dialog on open and keep it there. On close, give focus back to the element that opened the modal. Add a test.

### 7.2 The main page stylesheet is over the style budget (P2)

- **Where:** `pages/main-page/main-page.component.scss` (5.36 kB on 2026-11-04, warning at 4 kB, error at 10 kB).
- **Problem:** the build prints a style-budget warning for this file.
- **Fix:** move the popular dishes section into a child component with its own stylesheet.
