# FE-3 tests r1
File: frontend/restaurant-frontend-app/src/app/shared/helpers/group-pre-order.helpers.spec.ts (new).
Tests (10): totals with waiting guest excluded (27+36+73=136); Achieved 10 % -> 13.60 / 122.40; InProgress -> discount 0, unlock 13.60; seats for 6 seats/3 guests (owner, guests in API order, empty 5 and 6, initials); canSubmit false for 0 ordered; false for Submitted; canSubmit true for open group with 1 order; formatTimeLeft 27 h, 45 min, past -> null.
tsc -p tsconfig.spec.json --noEmit: exit 0.
Karma ChromeHeadless: 381 total, 10 failed, 371 success. Failure reason: Error: Not implemented (scaffold stubs).
Existing tests: all pass.
