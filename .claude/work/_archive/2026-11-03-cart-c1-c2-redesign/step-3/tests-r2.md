# Test-writer report: step 3, round 2 (minor fix from review-r1)

## Tests written

- `cart.component.spec.ts::submit and save shows no toast and does not close the cart when saveOrder gets an unknown order ID`. The cart has `order-1` (reservation `res-1`, 4 guests). The test calls `saveOrder('missing-id')`. It expects no `toast.success` call and no `closeCartEvent` emit.

I made no other change.

## Red run

This test pins behaviour that already exists, so it passes on the first run.

1. `npx tsc -p tsconfig.spec.json --noEmit` → exit 0, 0 errors.
2. `npm test -- --watch=false --browsers=ChromeHeadless --include='src/app/components/cart/cart.component.spec.ts'` → **TOTAL: 19 SUCCESS**.
3. `npm test -- --watch=false --browsers=ChromeHeadless` (full suite) → **TOTAL: 46 FAILED, 248 SUCCESS**. The failing names are identical to the 46 in the baseline from before this task. No cart test and no not-submitted-order test fails.

The test is not vacuous. The cart holds a valid group order, so only the order ID lookup can stop the save. The test catches these wrong implementations:

- `saveOrder` with no order lookup: it calls the toast and closes the cart for any ID.
- A lookup that falls back to the first unsubmitted order: `order-1` exists, so the toast and the close happen.
- A lookup that does not stop the toast, or that closes the cart before the lookup: one of the two spies is called.

## Existing tests

The other 18 cart tests still pass.

## Not tested

Nothing new.

## Notes for the implementer

- `saveOrder` with an ID that is not in `orders()` must do nothing that the user can see: no toast and no close.
