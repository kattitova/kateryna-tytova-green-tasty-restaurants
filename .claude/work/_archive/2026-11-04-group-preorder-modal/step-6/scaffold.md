# FE-2 scaffold report

Mode: scaffold, step 6 (FE-2).

Log:
- New `models/group-orders/group-bonus.model.ts` (`GroupBonusState`, `GroupBonusTier`, `GroupBonus`).
- `GroupStatusResponse` adds `orderedCount`, `bonus`; `GroupSessionsResponse` adds `owner`; Ukrainian comment translated to English.
- `submit-group-pre-order.model.ts`: comment paths now `/guest/groups/{groupReservationId}/submit`.
- New `services/group-orders/group-orders.service.ts` with the 5 exact signatures (`getOwnerGroupId$`, `getSessions$`, `getStatus$`, `removeGuest$`, `submit$`); bodies throw `Error('Not implemented')`; `API_BASE_URL` and `HttpClient` injected.
- `PreOrdersService.editPreOrderByReservationId` param type widened (behaviour unchanged).
- Mock store (real values): `snapshot()` adds `owner`; `groupStatus()` adds `orderedCount` and `bonus`.

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 367 of 367 SUCCESS, 0 failed.
No spec changed, nothing committed.
