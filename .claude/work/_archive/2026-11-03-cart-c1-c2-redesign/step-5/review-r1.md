# Code review: step 5, round 1 (last step)

Step 5 (plan r4): "Submitted orders: hide "Edit Pre-order" for group orders (Q5 b)".
Commits reviewed: `a3498f6` (scaffold), `efff1ab` (tests, red), `3cbe56a` (implementation).
Inputs: `00-task.md`, `plan.md` step 5, `step-5/scaffold.md`, `step-5/tests-r1.md`, `step-5/impl-r1.md`, two screenshots in `step-5/`.

## Scope and maker/checker split

| Commit | Files | Role | Result |
|---|---|---|---|
| `a3498f6` | `cart.component.ts`, `cart.component.html`, `submitted-order.component.ts` | implementer (scaffold) | production only, OK |
| `efff1ab` | `cart.component.spec.ts`, `submitted-order.component.spec.ts` | test-writer | tests only, OK |
| `3cbe56a` | `cart.component.ts`, `submitted-order.component.ts`, `.html` | implementer | production only, OK |

No change outside the step. No `infra/` or `*.tf` change. No API or route change, so no `ApiRoutes.md` update is necessary. `git status --porcelain` is clean.

## Contract check (plan step 5)

| Contract item | Where | Result |
|---|---|---|
| `orderMode = input.required<CartOrderMode>()` (M3, no default) | `submitted-order.component.ts:23` | OK |
| `canEdit = computed(() => orderMode() === 'single')`, fail closed | `submitted-order.component.ts:39` | OK. `'group'` and `'unknown'` give `false`. |
| Template renders "Edit Pre-order" only when `canEdit()` (absent from DOM, not disabled) | `submitted-order.component.html:61-69` | OK |
| "Cancel Pre-order" in every mode, unchanged | `submitted-order.component.html:53-59` | OK |
| `editOrder()` emits only when `canEdit()` | `submitted-order.component.ts:45-49` | OK |
| `submittedOrderViews` uses the same mapping as `unsubmittedOrderViews` | `cart.component.ts:69-71, 271-277` | OK. Both go through one private `toOrderViews` → `orderMode`. One decision lives in one place. Status 1 and 3 use the same rule. |
| Template `@for (view of submittedOrderViews(); track view.order.orderId)` with `[orderMode]="view.mode"` | `cart.component.html:68-75` | OK |
| `editOrder(orderId)` returns with no API call when the mode is not `'single'` | `cart.component.ts:142-148` | OK. Same guard as `submitOrder`. |
| `editOrderModal()` gets the guard through `editOrder` | `cart.component.ts:104-114` | OK. No code change necessary. |
| Submit modal, cancel modal, `cancelOrder` unchanged | — | OK |

Correctness notes:
- `orderMode(order)` reads the `reservationGuestCounts` signal in a plain method. In `editOrder` this runs outside a reactive context, so it reads the current value. No stale state.
- A single owner's submit → submit modal → "Edit" path still works: the order is in `orders()` after the reload, its mode is `'single'`, and `editOrder` calls `unsubmitPreOrderByReservationId`.
- The submitted order in `'unknown'` mode loses Edit until the cart is reopened. This is the documented fail-closed result (plan "Contracts and risks").

## Tests

`submitted-order.component.spec.ts`: `beforeEach` sets `orderMode` `'single'` (plan r4 minor). The existing test "should emit edited when editOrder is called" is unchanged and relies on it. 8 new tests cover all the cases in the plan.

`cart.component.spec.ts`: 8 new tests cover all the cases in the plan, including the two r4 `'unknown'` variants of `editOrder`.

Mutation check (which one-line change would still pass):
- `canEdit = orderMode() !== 'group'` → killed by the "unknown mode" DOM and `editOrder()` tests.
- Template guarded but `editOrder()` not guarded → killed by the two direct `editOrder()` tests.
- Cart guard `=== 'group'` instead of `!== 'single'` → killed by the two r4 `'unknown'` cart tests.
- `submittedOrderViews` gives `'unknown'` to everything, or looks up the first reservation → killed by "shows Edit for 1 guest" (the list puts `res-2`, 4 guests, first).
- Guard that blocks every mode → killed by "unsubmits a single order", which also checks the `'res-1'` argument.
- Mode guard that also hides or blocks Cancel → killed by the 3 Cancel tests.

The tests check behaviour through the DOM and the service mock, not the implementation. I found no gap that the plan asked to close.

## Screenshots

`group-unsubmitted-and-submitted.png` and `-scrolled.png`: the group order in the unsubmitted section shows "Save Pre-Order". The submitted group order (`res-2`, 4 guests) shows only "Cancel Pre-order". This agrees with Q5 (b). The lone Cancel button at the right of `.actions` looks acceptable. A designer can check it (the implementer's §8 says the same).

## Findings

None of severity blocker, major or minor in step 5.

## Overall branch check (`git diff origin/main...HEAD -- frontend`, 15 files, +1107 / -345)

I read the whole branch diff for consistency across steps 1-5.

Consistent:
- One mode type (`CartOrderMode`), one pure helper (`toCartOrderMode`), one private mapping in the cart (`orderMode`), used by both views and by both guards (`submitOrder`, `editOrder`).
- Both child components have `orderMode` as `input.required` (M3). The step 2 interim `'single'` binding is gone: both bindings in `cart.component.html` are `view.mode`.
- Fail closed everywhere: `'unknown'` disables the CTA (child), blocks `onCtaClick` (child), blocks `submitOrder` and `editOrder` (cart), and hides Edit (child).
- Save makes no API call (Q1). `getReservationsForUser$` runs once per cart open (`forkJoin` on the first load only).
- No route change, so no `ApiRoutes.md` change. Q2/Q3 respected: no "Your guests" line, no interceptor change.
- ESLint on all changed cart files and the helper: 0 problems.

Notes for the orchestrator. They need no fix in this step, and I do not count them as findings:
1. **`'unknown'` inline message (step 2 text, plan-approved).** `not-submitted-order.component.html:83-85` says "We could not load your reservation details. Reopen the cart to try again." `'unknown'` has two causes: the load failed, or the order's `reservationId` is not in `GET /reservations`. In the second case, reopening the cart does not help. That case can occur with real data, for example when the list endpoint filters out a reservation. Then the owner sees a disabled CTA with a wrong hint. Candidate for `docs/tech-debt.md`. A check with real API data is necessary.
2. **No hint on a submitted order in `'unknown'` mode.** Edit disappears with no explanation. Accepted by the plan (fail closed). Same tech-debt entry as note 1.
3. **Comment in `cart.component.ts:145`** says "group order" only. The guard also blocks `'unknown'`. The comment at `:166` in `submitOrder` names both cases. Cosmetic, optional.
4. **Step 2 review minor 2** (the "-" button looks the same enabled and disabled) went to `docs/tech-debt.md` as a deferred item. That file does not exist at the repo root (only `backend/restaurant-backend-app/docs/tech-debt.md` is tracked). The orchestrator must create it in the Finish step or record the item elsewhere.
5. `.list` in `cart.component.scss:108` has no user. It exists on `origin/main` (`:76`), so it is debt from before the task, not in a changed line. Step 5 did not remove it. Optional cleanup.
6. Layout with a submitted section below (impl §4): the unsubmitted footer is in the normal flow above "Submitted pre-orders" and scrolls away with its order. The step 4 review predicted this. Figma has no frame for this case. Acceptable.

## Check results (run by the reviewer in `frontend/restaurant-frontend-app`)

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left.", no NG8xxx |
| Initial bundle | 806.36 kB raw, 171.79 kB transfer (under the 1 MB limit) |
| Build warnings | 13 `[WARNING]` lines. None in a cart file. No style-budget warning for `cart.component.scss` or `not-submitted-order.component.scss`. |
| ESLint on the cart files and `group-reservation.helpers.ts` | 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 310 of 310 (46 FAILED)`, `TOTAL: 46 FAILED, 264 SUCCESS` |

The 46 failures are all outside the changed area: `GeneralInformationComponent` 32, and 1 each in `AppComponent`, `DishModalComponent`, `DishSelectCardComponent`, `MakeReservationComponent`, `MenuPageComponent`, `PlaceOrderWaiterComponent`, `RegisterComponent`, `ReportingDashboardComponent`, `ReservationConfirmedComponent`, `ReservationDashboardComponent`, `TimeSlotComponent`, `TimeSlotsModalComponent`, `WaiterDashboardComponent`, `WaiterPreOrderPageComponent`. No `CartComponent`, `NotSubmittedOrderComponent`, `SubmittedOrderComponent` or helper test fails. The count equals the pre-existing baseline of 46 and the implementer's run.

Not checked: real API data and auth, the mobile breakpoint in a browser, the submit-modal "Edit" path in a browser.

## Verdict

**approve**
