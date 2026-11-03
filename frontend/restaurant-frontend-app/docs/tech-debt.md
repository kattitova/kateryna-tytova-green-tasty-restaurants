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

### 2.1 46 unit tests fail (P2)

- **Where:** the Karma suite, outside the cart. Examples are stub specs and specs with missing providers.
- **Problem:** before the redesign, `npm test -- --watch=false --browsers=ChromeHeadless` gave 46 failed tests. The number did not change after the redesign. A new failure is easy to miss among them.
- **Fix:** fix or remove the failing specs. Then make 0 failed tests the baseline.

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
- **Problem:** the group status path and fields near line 1191 do not agree with `RouteRegistry.cs:195` and `GroupReservationStatusResponse.cs`. Lines 1132 and 1147 use `/guest-sessions/...`, but the code uses `/guest/sessions/...`. The document shows the order `status` as a string, but the frontend `Order.status` is a number.
- **Fix:** copy the paths, fields and types from the code into `ApiRoutes.md`.
