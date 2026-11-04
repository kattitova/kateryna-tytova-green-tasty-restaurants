# Implementer report: step 2, implement, round 1

## 1. Mode and step

Mode: implement. Step 2 (plan r4): "Order card: mode-driven footer button and restyle".

## 2. Log

1. Read `step-2/tests-r1.md`, the new spec, `00-task.md` (Figma facts, M1-M4), plan r4 steps 2 and 4, the current component files, `_colors.scss`, `_typography.scss`, the global `.icon-button` styles, and `cart.component.scss`. The spec agrees with the plan, so there is no test defect.
2. Red state: `tests-r1.md` records 11 new tests failing (full run 57 FAILED, 223 SUCCESS). I did not run the suite again before the change, because the scaffold has had no change since that run.
3. TypeScript (`not-submitted-order.component.ts`):
   - `ctaLabel = computed(() => this.orderMode() === 'group' ? 'Save Pre-Order' : 'Submit Pre-Order')`.
   - `ctaDisabled = computed(() => this.orderMode() === 'unknown')`.
   - `onCtaClick()` is an exhaustive `switch`: `'single'` calls the existing `submit()`, so the `submitted` emit stays in one place; `'group'` emits `saved` with `order().orderId`; `'unknown'` does nothing.
   - `submit()`, `addDishToOrder()`, `removeDishFromOrder()`, increment, decrement and `totalPrice` did not change.
4. Template (`.html`), rewritten to the Figma structure:
   - The caption "Pre-order #{{ orderId }} ({{ address }}, {{ time }})" text did not change (Q4). Its class is now `order-caption`.
   - There is one card per dish (`.order-item-card`): a 96 px image, the name, the caption "Expected ready time - 10 minutes" (a hyphen as in Figma, not the old en dash), text `-` / `+` buttons with the old `aria-label`s and the `disabled` rule (quantity ≤ 1), `.quantity-value`, `.item-price` (`currency` pipe, M4), and the remove button. The remove button keeps `aria-label="Remove <name>"`, uses the global `.icon-button` (40 px, padding 8, 24 px icon) and is placed absolute at top 8, right 8.
   - The "Add dish" button did not change (Q4). It is between the cards and the footer.
   - `.order-footer` contains the `.grand-total` row (`<h4>Grand Total</h4>` and `.grand-total-value`), then `.cta-unavailable` (`role="status"`, the exact text from the plan; it shows when `ctaDisabled()`, so the rule is in one place), then the CTA `<button type="button" class="cart-cta" [disabled]="ctaDisabled()" (click)="onCtaClick()">{{ ctaLabel() }}</button>`.
   - The old `Total payment` / `<h2>` block and the `Submit Pre-order` button are removed.
5. SCSS (`.scss`), rewritten from the Figma values in `00-task.md` with the colour tokens (`color.$neutral-*`, `color.$green-600/800`):
   - Card: white, radius 8, padding 16, gap 16, shadow `0 0 5px rgba(218,218,218,0.7)`, `position: relative`.
   - Image: 96 px, `border-radius: 50%`, `object-fit: cover`, shadow `0 0 10px 4px rgba(218,218,218,0.7)`.
   - Name 14/24 500. Caption 12/16 400. The name block has `padding-right: 40px`, so a long name does not go under the absolute remove button.
   - Quantity buttons: white, border 1.4 px, radius 8, padding 4px 12px, 14/24 500. `-` has a `$neutral-200` border and `$neutral-400` text. `+` uses `$green-600`. The number is 16 px wide and centred. The price is 18/32 500 `$green-600`.
   - Footer: `position: sticky; bottom: 0; z-index: 1; margin-top: auto; margin-left/right: -40px; padding: 24px 40px; border-top: 1px solid $neutral-100; background: $neutral-0`, a column with gap 16. At ≤ 650 px the horizontal margins are -16 px and the padding is `24px 16px`.
   - Grand Total: H4 `$neutral-900` (margin 0) and the value 26/40 500 `$green-800`, space-between.
   - CTA: full width, height 56, radius 10, the gradient `linear-gradient(-83.73deg, #0f7a37 0%, #0b5b29 98.29%)`, 600 16/24 white, `font-family: inherit` (Poppins from `body`). When disabled: `opacity: 0.5; cursor: not-allowed`.
   - `.cta-unavailable`: caption 12/16, `$neutral-600`, centred. This style is not in Figma; I chose it to match the Figma caption style.
   - M1: `:host { display: flex; flex-direction: column; flex: 1 0 auto; }` and the root `.not-submitted-order` uses `display: flex; flex-direction: column; flex: 1 0 auto`. With `margin-top: auto` on the footer, the footer goes to the bottom when the content is short. The cart side of M1 (`.cart-body`, `.orders-list` with `flex: 1 0 auto`, no bottom padding) is step 4. I did not touch `cart.component.*` in this step.
6. Ran all gates and ESLint on the changed folder (results below).

## 3. Files changed

- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/components/not-submitted-order/not-submitted-order.component.ts`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/components/not-submitted-order/not-submitted-order.component.html`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/components/not-submitted-order/not-submitted-order.component.scss`

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left.", no NG8xxx |
| Initial bundle | 804.28 kB raw, 171.35 kB transfer (was 802.53 kB after the step 2 scaffold; 1 MB error budget) |
| Style budget lines | none for `not-submitted-order.component.scss` or `cart.component.scss`, so both are under 4 kB. The build has 13 `WARNING` lines, the same as after the scaffold, and all are in other files. |
| Style size, measured | `npx sass --load-path=src --style=compressed` on `not-submitted-order.component.scss` gives 2380 bytes (about 2.3 kB). Angular adds encapsulation attributes, so the real size is somewhat larger. The build prints no budget warning for this file. |
| `npx eslint` on the `not-submitted-order/` folder | no output, 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 280 of 280 (46 FAILED)`, `TOTAL: 46 FAILED, 234 SUCCESS`, exit 1 |

The failing test names are identical to the 46 pre-existing ones (`diff` against the step 1 list gives no output). All 17 `NotSubmittedOrderComponent` tests pass: 11 new and 6 kept. No `CartComponent` test fails.

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

- The visual result against `figma/c2-cart.png`: sizes, colours, the sticky footer, the 650 px breakpoint, and the M1 short-content case. This needs `npm start` and a browser. The M1 short-content result also depends on step 4: today `.cart` still has `padding: 40px` and `align-items: center`, and it has no `.cart-body` flex fill. So the footer reaches the panel bottom only after step 4.
- Real data in the cart needs the deployed API.

## 8. Follow-ups (not done)

- Step 4 must give `.orders-list` (unsubmitted) `flex: 1 0 auto; display: flex; flex-direction: column` and a body without bottom padding. Without these, M1 is not complete. The child footer's -40 px / -16 px margins assume the step 4 body padding of 40 px / 16 px.
- When more than one not-submitted order is in the cart, each order has its own sticky footer. The plan defines the layout only for a single order. This matches the plan, but the code-reviewer can note it.
- `.cta-unavailable` has no Figma style. I chose a caption style.
