# FE-4 tests r1
File: frontend/restaurant-frontend-app/src/app/components/reservations/card-reservation/card-reservation.component.spec.ts ("should create" kept).
Tests (4): group Reserved card shows "Group Pre-Order" and click emits groupPreOrder with the reservation (red: button undefined); no button for guestCount 1 (passes now, guards the template condition); no button for InProgress (passes now, same); icon button [aria-label="Cancel reservation"] emits cancelReservation 'res-1' (red: button null).
tsc -p tsconfig.spec.json --noEmit: exit 0.
Karma ChromeHeadless: 385 total, 2 failed, 383 success. Existing tests pass.
