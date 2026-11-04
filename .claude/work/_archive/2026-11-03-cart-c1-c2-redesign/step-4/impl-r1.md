# Implementer report: step 4, implement, round 1

## 1. Mode and step

Mode: implement. Step 4 (plan r4): "Cart panel layout and styles". This step is presentational and has no new unit tests.

## 2. Log

1. Read plan r4 step 4, `00-task.md` (panel and header facts, M1), `figma/c2-cart.png`, `cart.component.html` and `cart.component.scss`.
2. `cart.component.html` (structure only):
   - Added the wrapper `<div class="cart-body">` around the loading spinner, the unsubmitted section, the submitted section and the empty state.
   - The two `.orders-list` sections get the modifiers `orders-list--unsubmitted` and `orders-list--submitted`.
   - The close button: `icon-button icon-button--small` became `icon-button` (40 px, padding 8 px), so the 24 px icon matches Figma "padding 8px, 24px icon". The `<img>` now has `width="24" height="24"`. `aria-label="Close cart"` did not change.
   - I ran Prettier on the file, which re-indented the block inside the new wrapper.
   - All DOM hooks that the tests use are unchanged: `.cart-cta` (in the child), `<h4>My Pre-Order</h4>`, `app-loading-spinner`, `aria-label="Close cart"`, and the child bindings.
3. `cart.component.scss`:
   - `.cart`: `width: min(640px, 100vw)`, `height: 100dvh`, `display: flex; flex-direction: column`, no padding, `overflow: hidden` (no panel scroll), `box-shadow: 0 0 5px rgba(195,193,173,0.39)`. At ≤ 650 px: `width: 100%`.
   - `.cart-header`: `position: relative; padding: 24px 40px; border-bottom: 1px solid $neutral-100`, a centred title (global H3 26/40 500, `$neutral-900`, margin 0), and the close button `position: absolute; top: 16px; right: 16px` (this replaces `right: 5%`).
   - `.cart-body`: `flex: 1; min-height: 0; overflow-y: auto; padding: 24px 40px 0; display: flex; flex-direction: column; gap: 24px`. At ≤ 650 px: `padding: 24px 16px 0`. There is no bottom padding, so the sticky child footer touches the panel bottom (M1).
   - `.orders-list`: gap 16 px (section title to cards), `h4` margin 0 and `$neutral-900`. `.orders-list--unsubmitted { flex: 1 0 auto; }` (M1, so the child host can grow). `.orders-list--submitted` keeps its old 24 px gap and gets `padding-bottom: 24px`.
   - `.empty-cart`: `height: 100%` became `flex: 1`, and it has `padding-bottom: 24px`. This keeps it centred in the flex body.
   - The backdrop did not change. I did not change the styles of the submitted section beyond the padding. Step 5 owns that section.
4. M1 child side: I made no change to `not-submitted-order`. Step 2 already has `:host` and the root with `flex: 1 0 auto` and the footer with `margin-top: auto`, and its -40 px / -16 px margins match the body padding.
5. I did not change `cart.component.spec.ts` or the `not-submitted-order` files.
6. Ran the gates, ESLint, and a Playwright visual check (below). Then I stopped the dev server.

## 3. Files changed

- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.html`
- `/home/user/kateryna-tytova-green-tasty-restaurants/frontend/restaurant-frontend-app/src/app/components/cart/cart.component.scss`

Screenshots (new, in this folder): `a-single-short.png`, `b-group-short.png`, `c-group-many-dishes.png`, `c-group-many-dishes-scrolled.png`.

## 4. Check results

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left." |
| Initial bundle | 805.90 kB raw, 171.74 kB transfer (805.32 kB after step 3) |
| Style budget lines | none for `cart.component.scss` or `not-submitted-order.component.scss`. The build has 13 `WARNING` lines, the same as before, and all are in other files. |
| `cart.component.scss` measured | `npx sass --style=compressed`: 1610 bytes (4 kB warning budget) |
| ESLint `cart.component.html` | no output, 0 problems |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 294 of 294 (46 FAILED)`, `TOTAL: 46 FAILED, 248 SUCCESS` |

The failing names are identical to the 46 pre-existing ones (`diff` gives no output).

### Visual check (Playwright, Chromium, viewport 1280 × 1013, deviceScaleFactor 1)

Method: I ran `ng serve` (development config) on port 4300. A script in the scratchpad (not in the repo) put a fake unexpired JWT and a `Customer` user in `localStorage`. It answered all API calls with `page.route`: `/me` returns a Customer, `/orders` returns one status-0 order for `res-1`, and `/reservations` returns `[{ id: 'res-1', guestCount }]`. The dish images come from `assets/img/dishes`. The script clicked the header "Cart" button and took a screenshot of the visible `.cart` element. Poppins loaded (`document.fonts.check('600 16px Poppins')` is `true`).

| Screenshot | Case | Measured |
|---|---|---|
| `a-single-short.png` | (a) 1 guest, 2 dishes | CTA "Submit Pre-Order"; panel 640 × 1013; footer 852-1013, so it is at the panel bottom with empty space above "Grand Total" (M1); footer width 640 (the border spans the full panel) |
| `b-group-short.png` | (b) 5 guests, 2 dishes (the same data as Figma C2) | CTA "Save Pre-Order"; the footer is at the bottom (852-1013), width 640 |
| `c-group-many-dishes.png` | (c) 5 guests, 6 dishes, scroll top | the cards scroll under the footer; the footer stays at 852-1013 |
| `c-group-many-dishes-scrolled.png` | (c) scrolled to the end | the last card and "Add dish" are visible above the footer; the footer is in the same place |

Comparison with `figma/c2-cart.png`:

- These match Figma: the header (title "Cart" centred at y≈44, the close X at top right, the border at y≈88), "My Pre-Order" at y≈128, the cards (round images with a shadow, name and caption, the `-`/`+` buttons, a green price, the trash icon at top right), "Grand Total" with the green amount, and the full-width gradient CTA. The positions agree within a few pixels.
- Accepted differences:
  - The caption "Pre-order #… (address, time)" and the "Add dish" button show (Q4, keep both).
  - Prices show `$17.00` and Figma shows `$17` (M4).
  - The "Your guests: … · View group pre-order" line does not show (Q2/Q3). So our footer starts at y = 852 and Figma's starts at y ≈ 812.
- The left 14 px strip in Figma is the page behind the panel. The screenshot shows only the panel.

## 5. Fix mode

Not applicable.

## 6. Test defects

None.

## 7. Not checked locally

- The real API data and the real auth. The visual check used mocked responses.
- The mobile breakpoint (≤ 650 px) and a cart with both not-submitted and submitted orders. I did not take screenshots of these.
- Browsers other than Chromium.

## 8. Follow-ups (not done)

- The header renders `<app-cart>` in two places (`header.component.html:63` and `:250`, desktop and mobile). Both instances exist in the DOM, and each one calls `GET /orders` and `GET /reservations` when the cart opens. This behaviour was there before this task (it is outside the step). Record it as tech debt if you want to.
- `.list-header`, `.list` and `.body` in `cart.component.scss` serve only the submitted section and the empty state. Step 5 can review them.
