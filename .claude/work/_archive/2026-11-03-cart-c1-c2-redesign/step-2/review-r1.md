# Code review: step 2, round 1

Scope: `not-submitted-order.component.{ts,html,scss,spec.ts}` (spec at commit `318cff4`, production files unstaged) and the interim binding `[orderMode]="'single'"` in `cart.component.html:46`. The step 3 changes that are in the working tree now (`cart.component.ts`, `models.ts`) are not in scope.

Inputs read: `00-task.md` (Figma facts, Q1-Q5, M1-M4), `plan.md` (r4) step 2, `step-2/scaffold.md`, `step-2/tests-r1.md`, `step-2/impl-r1.md`, `figma/c2-cart.png`, `.claude/rules/frontend.md`, `angular-developer` "Project overrides", global `_typography.scss` (h4 = 18/32 500) and `buttons.scss` (`.icon-button` = 40 px, padding 8).

## Plan conformance

| Contract item | Result |
|---|---|
| `orderMode = input.required<CartOrderMode>()` (M3) | done, `.ts:23` |
| `saved = output<string>()` emits `order().orderId` | done, `.ts:26`, `.ts:69` |
| `ctaLabel`: group → Save, single/unknown → Submit | done, `.ts:41-43` |
| `ctaDisabled = orderMode() === 'unknown'` | done, `.ts:45` |
| `onCtaClick`: single → `submitted`, group → `saved`, unknown → nothing | done, `.ts:63-74`. single goes through `submit()`, so the emit is in one place |
| `.cta-unavailable` with `role="status"` and the exact text, only in unknown | done, `.html:179-183`. driven by `ctaDisabled()` (one decision, one place) |
| Kept: `submit()`, `addDish`, `removeDish`, `updateQuantity`, inc/dec, `totalPrice`, caption text (Q4), "Add dish" (Q4) | kept, no logic change |
| DOM hooks: `.cart-cta`, "Grand Total", `.grand-total-value` with currency pipe (M4), aria-labels | done |
| Interim binding `[orderMode]="'single'"` in cart, no other cart change in step 2 | done (`cart.component.html:46`) |
| M1 child side: `:host` and root flex column `flex: 1 0 auto`, footer `margin-top: auto` | done, `.scss:9-20`, `.scss:140-160` |
| Footer sticky, -40 px / -16 px margins, padding, border-top, background | done |

Figma vs SCSS (`00-task.md` values and `c2-cart.png`): card radius 8 / padding 16 / gap 16 / shadow. 96 px round image with `object-fit: cover` and shadow. name 14/24 500. caption 12/16 400 with a hyphen. quantity buttons 1.4 px border, radius 8, padding 4px 12px, `-` neutral-200/400, `+` green-600. number 16 px centred. price 18/32 500 green-600. trash button absolute 8/8, 40 px. Grand Total h4 (global 18/32 500) and value 26/40 500 green-800. CTA 100 % x 56, radius 10, gradient, 600 16/24 white. All match. The "Your guests" line is correctly absent (Q2/Q3). Whole-dollar prices are the accepted M4 deviation.

Maker/checker split: the spec is changed only in the test-writer commit `318cff4`. the implementer changed only the three production files. No violation.

## Findings

### [minor] [tests] not-submitted-order.component.spec.ts:119-148 — the label in `'unknown'` mode is not tested
- Problem: the plan says `'unknown'` → `'Submit Pre-Order'`. No test reads the label in `'unknown'` mode.
- Scenario: mutate `ctaLabel` to `this.orderMode() === 'single' ? 'Submit Pre-Order' : 'Save Pre-Order'`. Single shows Submit, group shows Save, unknown shows a disabled "Save Pre-Order". All 17 tests still pass.
- Fix: in the `unknown mode` describe, add `expect(cta()?.textContent?.trim()).toBe('Submit Pre-Order');` (in "disables the button" or a new test).

### [minor] [code] not-submitted-order.component.scss:95-115 — the enabled and the disabled "-" button look the same
- Problem: `.quantity-button--decrease` is always grey (`$neutral-200` border, `$neutral-400` text). `:disabled` changes only the cursor. At quantity 2 or more the "-" button is enabled but looks disabled. at quantity 1 there is no visual "disabled" cue except the cursor. This follows the plan text literally, and Figma shows only quantity 1, so it is not a contract violation.
- Scenario: a dish with quantity 3 → the "-" button looks inactive, the user may think it cannot decrease.
- Fix (optional, needs a design decision): give the grey style to `.quantity-button:disabled` and keep the green style for an enabled "-". If the team wants the literal Figma look, leave it and record nothing.

## Notes (no action in this step)

- More than one not-submitted order gives one sticky footer per order (each sticks inside its own order block). The plan defines the layout for one order only. The implementer reported it too.
- The footer's -40 px / -16 px margins assume the body padding from step 4. Before step 4 the desktop cart padding is 40 px, so the desktop result is already right. check mobile after step 4.
- `onCtaClick` has no `never` exhaustiveness check. With today's 3-member union this is fine.
- Visual result not checked in a browser (sticky footer, M1 short content, 650 px breakpoint). That is the step 4 manual check.

## Check results (run by the reviewer, `frontend/restaurant-frontend-app`)

| Check | Result |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | exit 0, 0 errors |
| `npx tsc -p tsconfig.spec.json --noEmit` | exit 0, 0 errors |
| `BACKEND_URL=https://example.com npm run build` | exit 0, "Bundle check passed: no configuration placeholders left.", no NG8xxx. no budget warning for `not-submitted-order.component.scss` |
| `npm test -- --watch=false --browsers=ChromeHeadless` | `Executed 280 of 280 (51 FAILED)`, `TOTAL: 51 FAILED, 229 SUCCESS` |

Failure analysis: 0 `NotSubmittedOrderComponent` failures (all 17 pass). 5 failures are `CartComponent` tests (`should create`, `should return unsubmitted orders with status 0`, `should return submitted orders with statuses 1 and 3`, `should navigate to reservations ...`, `should emit closeCartEvent ...`) with `NG0201: No provider found for InjectionToken API_BASE_URL. Path: ReservationService2`. The cause is the step 3 scaffold that is now in the working tree (`cart.component.ts` injects `ReservationService`. the cart spec has no mock yet). They are not caused by step 2: the implementer's run before the step 3 edit had 0 cart failures. The other 46 are the pre-existing baseline. The step 3 test-writer must add the `ReservationService` mock (the plan already requires it).

## Verdict

**approve with minor** — 0 blocker, 0 major, 2 minor (1 tests, 1 code, the code one optional/design decision).
