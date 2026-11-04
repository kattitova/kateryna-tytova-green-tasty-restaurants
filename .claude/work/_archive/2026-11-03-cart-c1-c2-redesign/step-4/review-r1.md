# Code review: step 4, round 1

Step 4 (plan r4): "Cart panel layout and styles". Also the carried-over item from step 3 (review-r1 minor: `saveOrder` with an unknown ID).

## Scope

Commits:
- `f5d2f20` implementation (implementer): `cart.component.html`, `cart.component.scss`
- `45ae08b` carried-over fix (test-writer): `cart.component.spec.ts` (+10 lines)

The working tree is clean (`git status --porcelain` gives no output). There are no `infra/` or `*.tf` changes.

**Maker/checker split:** OK. `f5d2f20` changes only production files (HTML and SCSS). `45ae08b` changes only `cart.component.spec.ts`.

**Plan coverage:** complete. Each item of the step 4 "Change" is in the diff:
- Panel: `width: min(640px, 100vw)`, `height: 100dvh`, column flex, no padding, `overflow: hidden`, shadow `0 0 5px rgba(195,193,173,0.39)`.
- Header: `padding: 24px 40px`, border-bottom `$neutral-100`, centred H3 with `$neutral-900` and margin 0. The close button is `position: absolute; top: 16px; right: 16px`. It changed from `icon-button--small` (32 px, padding 4) to `icon-button` (40 px, padding 8, `buttons.scss:184-192`), so "padding 8px, 24px icon" matches Figma. The `<img>` has `width="24" height="24"`.
- `.cart-body`: `flex: 1; min-height: 0; overflow-y: auto; padding: 24px 40px 0`, column flex. `min-height: 0` is necessary so that the body scrolls in the column flex. This is a good addition to the plan.
- M1: `.orders-list--unsubmitted { flex: 1 0 auto; }` together with the step 2 `:host` / root `flex: 1 0 auto` and the footer `margin-top: auto` gives the full flex chain. The body has no bottom padding, so the sticky footer touches the panel bottom.
- The submitted section and the empty state have `padding-bottom: 24px`. The empty state changed from `height: 100%` to `flex: 1`. This is the right choice in a flex body.
- Section title H4 with gap 16 to the cards. The cards gap of 16 is in the child (step 2).
- The backdrop did not change.

No change is outside the step. The Prettier re-indent in the HTML comes from the new wrapper. The test hooks did not change: `<h4>My Pre-Order</h4>`, `app-loading-spinner`, `aria-label="Close cart"` and all child bindings, including `[orderMode]="view.mode"` and `(saved)`.

## Visual check (screenshots against `figma/c2-cart.png`)

I looked at `a-single-short.png`, `b-group-short.png`, `c-group-many-dishes.png` and `figma/c2-cart.png`.
- Header, title position, close X, border, "My Pre-Order", cards, Grand Total and the gradient CTA agree with Figma within a few pixels.
- (a) and (b) with short content: the footer is at the panel bottom, with empty space above it (M1 satisfied). The footer border spans the full 640 px.
- (c) with long content: the cards scroll under the footer, and the footer stays fixed.
- C1 shows "Submit Pre-Order" and C2 shows "Save Pre-Order".
- Accepted differences (decisions Q2/Q3, Q4, M4): the reservation caption and "Add dish" show, `$17.00` shows where Figma has `$17`, and there is no "Your guests" line.

## Mobile (≤ 650 px), from the SCSS

- `.cart` is `width: 100%`. `min(640px, 100vw)` already gives this, so the media rule is redundant but harmless.
- The body padding `24px 16px 0` matches the child footer's `margin: 0 -16px; padding: 24px 16px` (`not-submitted-order.component.scss`, `.order-footer` media rule). So the footer border still spans the full width, and the CTA aligns with the cards.
- The header keeps `padding: 24px 40px` on mobile. The title is centred and the close button is absolute (16 px), so this has no visible effect at 320-650 px. The header still fits: "Cart" is about 60 px wide.
- The panel is `100dvh` with `overflow: hidden`, and only `.cart-body` scrolls. This is the same model as on desktop, so I expect the sticky footer to behave the same. Nobody took a mobile screenshot (the implementer said so in §7). This is a residual risk, not a finding.

## Findings

None of severity blocker, major or minor.

## Notes (no action in this step)

- **Tests for step 4: I agree that none are needed.** The change is layout CSS and one wrapper `div`. Karma/Jasmine cannot check the layout in a useful way (flex fill, sticky, scroll). A test of class names would check the implementation and not the behaviour. The step 2-3 tests still pass with the new wrapper, so the DOM contract is pinned. The screenshots give the layout evidence.
- **Not checked visually:** a cart with both an unsubmitted and a submitted order. From the SCSS: `.orders-list--unsubmitted` takes all free space, so with short content the order footer sits directly above the "Submitted pre-orders" section. That section is at the end of the body. The sticky footer sticks only inside its own order host. When the user scrolls to the submitted section, the footer scrolls away with its order. This is acceptable, because Figma has no frame for this case. With two or more unsubmitted orders, each host gets a part of the free space. The same rule applies to each footer. Step 5 changes the submitted section, so its visual check can cover the mixed case.
- `.list` in `cart.component.scss` has no user in the template. This is dead code from before the task, in lines that the change did not touch. The implementer already handed it on to step 5.
- `header.component.html` renders `<app-cart>` twice. This is debt from before the task (see the step 3 review). It is not in this diff.

## Carried-over item from step 3

- step-3/review-r1 minor (unknown `orderId` in `saveOrder` not tested): **fixed**. `45ae08b` adds exactly the test that I asked for: `cart.component.spec.ts:270-278`. It uses `renderWith([unsubmittedOrder], of([reservation('res-1', 4)]))`, calls `component.saveOrder('missing-id')`, and expects that `toastMock.success` and `closeCartEvent.emit` are not called. `toastMock` is created again in each `beforeEach` (`:76`), so no earlier call can leak into it. The cart holds a valid group order. So the test fails without the guard at `cart.component.ts:186-188`. It also fails if the lookup falls back to the first order. The commit changes nothing else (1 file, +10 lines). The test passes on its first run because it pins behaviour that already exists. The test-writer report says so, and this is acceptable for a gap that a review found.

## Check results (run by the reviewer, `frontend/restaurant-frontend-app`)

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left." Initial total 805.90 kB raw / 171.74 kB transfer. 13 `WARNING` lines, none for `cart.component.scss` or `not-submitted-order.component.scss`. |
| `cart.component.scss` compiled size (`npx sass --style=compressed`) | 1610 bytes (4 kB warning budget) |
| `npx eslint src/app/components/cart/cart.component.html` | exit 0, 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `TOTAL: 46 FAILED, 248 SUCCESS`. No failure in `CartComponent` or `NotSubmittedOrderComponent`. The 46 failures are the pre-existing baseline. |

## Verdict

**approve**: 0 blocker, 0 major, 0 minor. The carried-over step 3 minor is fixed. I agree that step 4 has no tests.
