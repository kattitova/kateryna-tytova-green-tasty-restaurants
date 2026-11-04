# Implementer report: step 5, implement, round 1

## 1. Mode and step

Mode: implement. Step 5 (plan r4): "Submitted orders: hide "Edit Pre-order" for group orders (Q5 b)".

## 2. Log

1. Read `step-5/tests-r1.md` (10 red, 6 already green, 310 tests, `56 FAILED, 254 SUCCESS`) and the submitted-order spec. The tests agree with the plan, so there is no test defect.
2. `SubmittedOrderComponent`:
   - `canEdit = computed(() => this.orderMode() === 'single')`. It fails closed: `'group'` and `'unknown'` give `false`.
   - `editOrder()` emits `edited` only when `canEdit()` is true.
   - The template wraps the "Edit Pre-order" button in `@if (canEdit()) { ... }`, so the button is not in the DOM in `'group'` or `'unknown'` mode. "Cancel Pre-order", `cancelOrder()` and `totalPrice` did not change.
3. `CartComponent.editOrder(orderId)`: the guard is now `if (!order || this.orderMode(order) !== 'single') return;` before `unsubmitPreOrderByReservationId`, the same guard as `submitOrder`. `editOrderModal()` calls `editOrder`, so it has the same guard (no code change there).
4. Ran all gates and ESLint. Then I ran a Playwright check with the same method as step 4: `ng serve` on port 4300 and a mocked API. I stopped the dev server after the check.

## 3. Files changed

- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/components/submitted-order/submitted-order.component.ts`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/components/submitted-order/submitted-order.component.html`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.ts`

Screenshots (new, in this folder): `group-unsubmitted-and-submitted.png`, `group-unsubmitted-and-submitted-scrolled.png`.

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left.", no NG8xxx |
| Initial bundle | 806.36 kB raw, 171.79 kB transfer |
| Build warnings | 13 `WARNING` lines, the same as before. None in a changed file. |
| ESLint `cart.component.ts`, `submitted-order/` | no output, 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 310 of 310 (46 FAILED)`, `TOTAL: 46 FAILED, 264 SUCCESS` |

The failing names are identical to the 46 pre-existing ones (`diff` gives no output). All 16 new step 5 tests pass, and so do all earlier cart, not-submitted-order and submitted-order tests.

### Visual check (Playwright, Chromium, 1280 × 1013, deviceScaleFactor 1)

Mock data: one status-0 order on `res-1` (5 guests) and one status-1 order on `res-2` (4 guests).

- `group-unsubmitted-and-submitted.png` (scroll top): "My Pre-Order" with the group order and a "Save Pre-Order" CTA. Below it is "Submitted pre-orders".
- `group-unsubmitted-and-submitted-scrolled.png` (scrolled to the end): the submitted group order shows only "Cancel Pre-order".
- The DOM check of the visible panel returned `{"cta":"Save Pre-Order","submittedButtons":["Cancel Pre-order"]}`.

Layout note: with a submitted section below, the unsubmitted order's sticky footer is in the normal flow and comes before "Submitted pre-orders". It stays at the panel bottom only while its own section fills the viewport. This is the expected result of the plan's per-order sticky footer (the plan defines the Figma layout for the single-order case only).

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

- Real API data and real auth (the check used mocked responses).
- The mobile breakpoint.
- The submit modal "Edit" path (`editOrderModal`) in the browser. It calls the guarded `editOrder`. The submit modal opens only after a successful single-mode submit.

## 8. Follow-ups (not done)

- When "Edit Pre-order" is hidden, "Cancel Pre-order" stands alone at the right of the `.actions` row. This is acceptable, but a designer can check it.
- The submitted section keeps its old styles (H3 title, list layout). Figma C1/C2 does not show this section.
