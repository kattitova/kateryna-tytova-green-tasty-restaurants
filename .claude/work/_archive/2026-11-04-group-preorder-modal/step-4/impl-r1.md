# BE-4 docs r1

Mode: implement (docs), step 4 (BE-4).

Log:
- ApiRoutes.md: status section rewritten with the code path `GET /guest/groups/{groupReservationId}/status`, public note, fields `currentParticipants`, `maxCapacity`, `status`, `orderedCount`, `bonus`, bonus rules, errors (400 "Missing groupReservationId in path parameters." copied from GetGroupReservationStatusHandler, 404, 500 "Internal server error: {message}").
- Submit: removed the 3 old errors, added 2 new 400 messages (copied from GroupPreOrderService), notes for owner-order creation, no duplication and `[]` response.
- Sessions and delete: added `owner` to examples and notes.
- ADR 0002: dated amendment (2026-11-04) before References.
- Frontend tech-debt.md: 2.1 rewritten as fixed (0 failed of 364); 2.4 status drift removed (paths and order `status` type stay).

STE lint: ApiRoutes.md 0 violations (first run 9, 5 hard, fixed); ADR 0002 0 violations; tech-debt.md 0 violations.
Not changed: frontend code. Not checked: real API output (needs the deployed stack); the `deadline` "Z" format comes from System.Text.Json for UTC values.
Follow-up: the frontend `ApiRoutes` drift for `/guest-sessions/{id}/dishes` (lines 1139, 1154) stays in tech-debt 2.4.
