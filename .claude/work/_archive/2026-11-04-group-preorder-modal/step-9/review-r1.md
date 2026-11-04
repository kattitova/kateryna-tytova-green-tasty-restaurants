# Step 9 (FE-5) review r1

Checks (frontend/restaurant-frontend-app): tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 388 of 388 SUCCESS, 0 failed.
Maker/checker: only the new spec is a test file (test-writer); the implementer's report states no spec change. No violation seen.

Verified against the plan:
- Load: getOwnerGroupId$ -> forkJoin(getSessions$, getStatus$) -> buildGroupPreOrderView; loading spinner, error `gpo-error` with the exact text, no toast. `reload()` repeats the last two calls. Both subscriptions use takeUntilDestroyed.
- Bonus texts: all 5 rows of the table (heading, sub-line, footer line) match exactly, including the singular "pre-order", MINUS sign, Missed sub-line variants, and the Submitted box (bonus part only for Achieved).
- Totals: `gpo-grand-total` shows `v.finalTotal`; the footer uses `grandTotal`, `discountAmount` and `unlockDiscountAmount` from the helper. No total is computed in the template.
- Submitted: badge in `[modal-title-addon]`, green box, no submit button.
- OnPush and signals are consistent. Clickable rows and arrows are `<button type="button">` with `aria-expanded` and `aria-label`.
- Page: `closeGroupPreOrder` clears the signal and, on `changed`, calls `loadReservations()` and `cartState.refresh()`.
- FE-4 carry-over: `.with-table .details:first-child { margin-right: auto }` pushes badge and trash icon to the right end; `.cancel-icon` has `align-self: flex-end` in the column layout. This matches the fix-r1 intent (M00). Not checked in a browser.
- SCSS 4.58 kB (583 bytes over the 4 kB warning): acceptable, warning only, other files exceed it too. No split suggested (not cheap, no gain).

## Findings

### [minor] [code] group-pre-order-modal.component.html:95-101, 131, 138, 160, 190
- Problem: small logic in the template: the bonus icon ternary chain, `item.dish.price * item.quantity`, `guest.name.slice(0,1).toUpperCase()`, plural `=== 1 ? 'Guest' : 'Guests'`.
- Fix: put `icon` in `BonusBox`; use a helper or computed for the line price and initials. Not blocking.

### [minor] [code] group-pre-order-modal.component.ts:buildFooterBonusLine
- Problem: `money()` formats `$x.xx` by hand while the rest of the modal uses the `currency` pipe. Output is the same today for USD en-US. Also `Group discount — deadline passed` is a template literal with no placeholder.
- Fix: leave, or use `formatCurrency`. Not blocking.

### [minor] [tests] group-pre-order-modal.component.spec.ts
- Problem: only 3 tests (as the plan). Bonus texts for InProgress, FreeDessert, Missed and the footer lines have no test; `closed` emit and the dashboard `closeGroupPreOrder({changed:true})` reload have no test. A change to a text would pass.
- Fix: add tests for one InProgress and one Missed text, for the `closed` emit, and for the dashboard reload on `changed: true`. FE-6 and FE-7 are the first to set `changed`, so the dashboard test can wait until then.

### [minor] [code] ts: buildBonusBox InProgress
- Problem: with `deadline: null` the sub-line is "Until " (empty date). The backend sets the deadline for InProgress, so this is an edge case only.
- Fix: omit the sub-line when there is no deadline.

## Verdict: approve with minor
