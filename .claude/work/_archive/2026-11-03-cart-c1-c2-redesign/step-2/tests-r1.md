# Test-writer report: step 2, round 1

## Tests written

File: `frontend/restaurant-frontend-app/src/app/components/cart/components/not-submitted-order/not-submitted-order.component.spec.ts`. I replaced the 3 old tests with 17 tests. `beforeEach` sets `order` (quantity 2, price 10, dish name "Caesar Salad") and `orderMode` `'single'` with `setInput` (M3). Each mode test sets its mode with `setInput` again.

New behaviour (11 tests, red now):

- `single mode shows an enabled "Submit Pre-Order" button` — `.cart-cta` text and `disabled === false`.
- `single mode emits submitted with the order ID and not saved when the button is clicked` — DOM click on `.cart-cta`.
- `single mode does not show the unavailable message` — `.cart-cta` exists, `.cta-unavailable` does not.
- `group mode shows an enabled "Save Pre-Order" button`.
- `group mode emits saved with the order ID and not submitted when the button is clicked`.
- `group mode does not show the unavailable message`.
- `unknown mode disables the button`.
- `unknown mode shows the unavailable message as a status` — `role="status"` and the exact text from the plan.
- `unknown mode emits neither submitted nor saved from onCtaClick` — calls `onCtaClick()` directly, because a click on a disabled button does nothing.
- `grand total shows the "Grand Total" label`.
- `grand total shows the total for 2 x 10 as $20.00` — `.grand-total-value`.

Kept behaviour (6 tests, they pass now). The plan lists them as guards for the restyle:

- `disables "Decrease quantity" at quantity 1`, `enables "Decrease quantity" at quantity 2`.
- `emits updateQuantity with quantity 3 when "Increase quantity" is clicked at quantity 2`.
- `emits removeDish with the dish ID when "Remove" is clicked` — button `aria-label="Remove Caesar Salad"`.
- `emits addDish when the "Add dish" button is clicked`.
- `shows the reservation address in the caption` — text `Pre-order #order-1 (Test address`.

These 6 tests are not vacuous. If the restyle removes a button or changes its `aria-label`, the query returns `null`, `?.click()` does nothing, and the spy assertion fails. `?.disabled` then gives `undefined`, and `toBeTrue()` / `toBeFalse()` fail. A wrong quantity (2 or 1 instead of 3) or a wrong ID fails `toHaveBeenCalledOnceWith`.

I removed the 3 old tests. Every test now covers "should create". The single-mode CTA click covers the `submit()` emit. `.grand-total-value` covers `totalPrice() === 20`.

## Red run

1. `npx tsc -p tsconfig.spec.json --noEmit` → exit 0, 0 errors.
2. `npm test -- --watch=false --browsers=ChromeHeadless` → exit 1, **TOTAL: 57 FAILED, 223 SUCCESS** (280 tests).
3. I compared the failing test names with the step 1 test-writer run. The 11 new `NotSubmittedOrderComponent` tests above now fail. The 9 step 1 helper tests now pass, after the step 1 implementation. The other 46 failures are the same pre-existing set.

Failure reason for each new test:

| Test | Reason |
|---|---|
| single: Submit button | `Expected undefined to be 'Submit Pre-Order'` (no `.cart-cta`) |
| single: click emits submitted | spy not called (no `.cart-cta`) |
| single: no message | `Expected null not to be null` (no `.cart-cta`) |
| group: Save button | `Expected undefined to be 'Save Pre-Order'` |
| group: click emits saved | spy not called |
| group: no message | `Expected null not to be null` |
| unknown: disabled | `Expected null not to be null` |
| unknown: message | `Expected undefined to be 'status'` (no `.cta-unavailable`) |
| unknown: onCtaClick | `Error: Not implemented` (scaffold stub) |
| Grand Total label | the text contains "Total payment", not "Grand Total" |
| $20.00 value | `Expected undefined to be '$20.00'` (no `.grand-total-value`) |

## Existing tests

The 46 pre-existing failures (stub specs, missing providers) do not change. The cart spec is not changed in this step, and no `CartComponent` test fails.

## Not tested

- The Figma styling (sizes, colours, sticky footer, 650 px breakpoint, M1 flex layout). A unit test cannot check the visual result honestly. Check it in a browser.

## Notes for the implementer

- The CTA must be a `<button>` with class `cart-cta`. Its trimmed text must be exactly `Submit Pre-Order` or `Save Pre-Order`. The click calls `onCtaClick()`.
- `.cta-unavailable` must have `role="status"` and the trimmed text `We could not load your reservation details. Reopen the cart to try again.`
- `.grand-total-value` trimmed text must be `$20.00`. The text "Grand Total" must be in the component.
- In `'single'` and `'group'` mode, `onCtaClick()` emits once, with `'order-1'`.
- The "Add dish" button text after trim must be exactly `Add dish` (the icon is an `<img>`, so it adds no text).
- Keep `aria-label` values `Decrease quantity`, `Increase quantity`, `Remove <dish name>`.
