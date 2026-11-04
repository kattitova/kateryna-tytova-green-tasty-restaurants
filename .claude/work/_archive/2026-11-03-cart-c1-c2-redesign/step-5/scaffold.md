# Implementer report: step 5, scaffold

## 1. Mode and step

Mode: scaffold. Step 5 (plan r4): "Submitted orders: hide "Edit Pre-order" for group orders (Q5 b)".

## 2. Log

1. Read plan r4 step 5 and `submitted-order.component.ts` / `.html`. The file uses the signal API and `@app/...` imports, the same as `not-submitted-order`.
2. `SubmittedOrderComponent`:
   - Added `readonly orderMode = input.required<CartOrderMode>();` (contract, M3).
   - Added `readonly canEdit = computed((): boolean => { throw new Error('Not implemented'); });`. The plan allows this stub.
   - The template and `editOrder()` did not change. Both are new behaviour for the implement step. A stub in `editOrder()` would break the existing test "should emit edited when editOrder is called". The template does not read `canEdit` yet, so the stub does not run.
3. `CartComponent`, in the same run, so that the template compiles (no NG8008):
   - Added `readonly submittedOrderViews = computed(() => this.toOrderViews(this.submittedOrders()));`. I wrote real code here, not a stub, for two reasons. The template renders it, so a throwing stub would break every cart with a submitted order. And the contract defines it as "the same private order → mode mapping as `unsubmittedOrderViews`", which is code that exists already (step 3).
   - To keep that mapping in one place, I moved the step 3 lambda into `private toOrderViews(orders: Order[]): CartOrderView[]` (it uses `orderMode`). `unsubmittedOrderViews` now calls it too. The types and results did not change.
   - `editOrder` did not change. The mode guard is new behaviour for the implement step.
4. `cart.component.html`: the submitted loop is now `@for (view of submittedOrderViews(); track view.order.orderId)` with `[order]="view.order"`, `[orderMode]="view.mode"`, `(canceled)="openCancelModal(view.order.orderId)"` and `(edited)="editOrder(view.order.orderId)"`.
5. Ran the gates and ESLint.

## 3. Files changed

- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/components/submitted-order/submitted-order.component.ts`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.ts`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.html`

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left.", no NG8008 |
| Initial bundle | 806.15 kB raw, 171.82 kB transfer |
| Build warnings | 13 `WARNING` lines, the same as before. None in a changed file. |
| ESLint on the 3 changed files | no output, 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 294 of 294 (46 FAILED)`, `TOTAL: 46 FAILED, 248 SUCCESS` |

The failing names are identical to the 46 pre-existing ones. All existing `SubmittedOrderComponent` and `CartComponent` tests pass. The existing submitted-order spec does not set `orderMode`. It still passes, because nothing reads the input yet.

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

Nothing for the scaffold.

## 8. Follow-ups (not done)

- Test-writer: the submitted-order spec `beforeEach` must set `orderMode` `'single'` (plan step 5). After the implementation, the template reads `canEdit()`, so a missing `orderMode` throws NG0950.
