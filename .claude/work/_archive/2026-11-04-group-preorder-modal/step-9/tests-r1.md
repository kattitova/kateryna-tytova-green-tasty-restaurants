# FE-5 tests r1
File: frontend/restaurant-frontend-app/src/app/components/reservations/group-pre-order-modal/group-pre-order-modal.component.spec.ts (new; GroupOrdersService spy; providers: router, http testing, API_BASE_URL).
Tests (3): loaded Achieved 10 % (owner 27, guests 36 and 73, 3 of 3) -> gpo-ordered-summary "3 of 3 ordered", gpo-grand-total "$122.40", text has "$136.00"; getOwnerGroupId$ error -> gpo-error "Could not load the group pre-order."; status Submitted -> "Sent to Restaurant" and no "Submit Pre-Order" button.
tsc -p tsconfig.spec.json --noEmit: exit 0.
Karma ChromeHeadless: 388 total, 3 failed, 385 success. Reasons: getSessions$ never called (no loading yet); gpo-error missing; "Sent to Restaurant" missing. Existing tests pass.
Note: the "no Submit Pre-Order button" assertion is vacuous until the template has buttons; it passes with the scaffold, and is guarded by the "Sent to Restaurant" assertion in the same test.
