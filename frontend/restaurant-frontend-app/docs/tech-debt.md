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

- **Where:** `group-pre-order-modal.component.scss` (5.52 kB, warning at 4 kB, error at 10 kB).
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
