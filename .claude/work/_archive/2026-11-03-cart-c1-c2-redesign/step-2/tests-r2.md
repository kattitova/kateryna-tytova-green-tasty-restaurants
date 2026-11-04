# Test-writer report: step 2, round 2 (minor fix from review-r1)

## Tests written

- `not-submitted-order.component.spec.ts::unknown mode disables the button`: added one assertion, `expect(cta()?.textContent?.trim()).toBe('Submit Pre-Order');`. The test now checks that `'unknown'` mode shows the "Submit Pre-Order" label on the disabled button.

I made no other change. I did not touch the cart spec.

## Red run

This assertion pins behaviour that already exists, so it passes on the first run.

1. `npx tsc -p tsconfig.spec.json --noEmit` → exit 0, 0 errors.
2. `npm test -- --watch=false --browsers=ChromeHeadless --include='src/app/components/cart/components/not-submitted-order/not-submitted-order.component.spec.ts'` → **TOTAL: 17 SUCCESS**.
3. `npm test -- --watch=false --browsers=ChromeHeadless` (full suite) → **TOTAL: 56 FAILED, 237 SUCCESS**. No `NotSubmittedOrderComponent` test fails. The 56 failures are the 46 pre-existing ones plus the 10 red step 3 cart tests from `step-3/tests-r1.md`.

The assertion is not vacuous. It catches these wrong implementations:

- The reviewer's mutation `orderMode() === 'single' ? 'Submit Pre-Order' : 'Save Pre-Order'`. The label in `'unknown'` mode is then "Save Pre-Order".
- An empty label for the disabled state.
- A different label for the disabled state, for example "Unavailable".

## Existing tests

The other 16 tests in the spec still pass.

## Not tested

Nothing new.

## Notes for the implementer

- In `'unknown'` mode, the trimmed text of `.cart-cta` must be exactly `Submit Pre-Order`.
