# Implementer report: step 2, scaffold

## 1. Mode and step

Mode: scaffold. Step 2 (plan r4): "Order card: mode-driven footer button and restyle".

## 2. Log

1. Read plan r4 step 2 and the r3 → r4 delta. Read `not-submitted-order.component.ts` and `cart.component.html:35-55`. The file uses the signal API (`input.required`, `output`, `computed`) and `@app/...` imports. I copied that style.
2. Added these members to `NotSubmittedOrderComponent`, with the names and types from the contract:
   - `readonly orderMode = input.required<CartOrderMode>();`
   - `readonly saved = output<string>();`
   - `readonly ctaLabel = computed((): string => { throw new Error('Not implemented'); });`
   - `readonly ctaDisabled = computed((): boolean => { throw new Error('Not implemented'); });`
   - `onCtaClick(): void { throw new Error('Not implemented'); }`
   - an import of `CartOrderMode` from `@app/models/cart/cart-order-mode.model`.

   The coordinator asked for throwing bodies in the computed members too, so the scaffold contains no logic. A computed throws only when something reads it. The template does not read `ctaLabel` or `ctaDisabled` yet, so the existing behaviour does not change.
3. Added the interim binding `[orderMode]="'single'"` to `<app-not-submitted-order>` in `cart.component.html` (r4 fix for NG8008). I made no other change to the cart.
4. I did not change the template, the SCSS or the existing methods (no restyle in scaffold).
5. Ran the four gates. Compared the failing test names with the list from step 1 (after the step 1 implementation).

## 3. Files changed

- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/components/not-submitted-order/not-submitted-order.component.ts` (+15 lines)
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.html` (+1 line)

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, ends with "Bundle check passed: no configuration placeholders left.", no NG8008 |
| Initial bundle | 802.53 kB raw, 170.89 kB transfer (1 MB error budget) |
| Build warnings | 13 lines with `WARNING`. None of them is in a file that I changed. `not-submitted-order.component.scss` and `cart.component.scss` have no budget warning, so each is under 4 kB. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 266 of 266 (46 FAILED)`, `TOTAL: 46 FAILED, 220 SUCCESS`, exit 1 |

The failing test names are identical to the 46 from the step 1 implementation run (`diff` gives no output). None of them is a `NotSubmittedOrderComponent` test or a `CartComponent` test. There are no new failures.

Note: frontend `CLAUDE.md` §8 says the initial bundle is 915.96 kB. This build measured 802.53 kB. I report the measured number.

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

Nothing for the scaffold. The UI has no visual change yet.

## 8. Follow-ups (not done)

- The existing `not-submitted-order.component.spec.ts` does not set `orderMode`. It still passes because the template does not read `orderMode` yet. After the implement step, the template reads `orderMode`, so the spec must set `orderMode` with `setInput`. The plan already gives this to the test-writer (M3).
