# FE-2 tests r1
File: frontend/restaurant-frontend-app/src/app/services/group-orders/group-orders.service.spec.ts (new, HttpTestingController, API_BASE_URL = https://test.api).
Tests (4): getOwnerGroupId$ emits g1 for owner-g1 (GET /reservations/r1/guest-session-id); errors 'Unexpected owner session id' for g1; removeGuest$ DELETE /guest/groups/g1/sessions/guest-anna; submit$ POST /guest/groups/g1/submit with body { reservationId: 'r1' }.
tsc -p tsconfig.spec.json --noEmit: exit 0.
Karma ChromeHeadless: 371 total, 4 failed, 367 success. All 4 fail with Error: Not implemented (scaffold stubs throw).
Existing tests: all pass.
