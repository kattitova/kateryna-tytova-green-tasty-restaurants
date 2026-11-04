# Review r1, step 8 (FE-4) - verdict: approve with minor

Checks: tsc app exit 0; tsc spec exit 0; Karma ChromeHeadless 385 of 385 SUCCESS, 0 failed. Only 4 card-reservation files changed; implementer did not touch the spec, test-writer did not touch production code.

Verified: waiter markup unchanged (waiter Cancel is the same button, now wrapped in `@if role()===WAITER`); customer Cancel is a real `<button type="button">` with aria-label "Cancel reservation", native keyboard access, focus-visible style from `.icon-button`, img `alt=""` + aria-hidden; Group Pre-Order = `button--secondary`, only Reserved + `isGroupReservation(guestCount)` (>= 2) + customer; all customer buttons `button--small` (Q5).

### [minor] [code] card-reservation.component.scss:37 - icon is not in the top-right corner
- Problem: D4/M00 put the trash icon in the card's top-right corner. It sits right after the status badge with only `margin-left: 0.5rem`.
- Fix: add `margin-left: auto` (if the address line is a flex row) or position it to the right; check on mobile. Layout was not checked in a browser (implementer says so too).

### [minor] [tests] card-reservation.component.spec.ts - no test that the waiter card has no icon button / keeps the text Cancel
- Fix: add one test with role WAITER (text "Cancel" present, no `[aria-label="Cancel reservation"]`). Optional: icon hidden for Completed/Canceled.
