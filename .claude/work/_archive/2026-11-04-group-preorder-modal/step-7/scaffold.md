# FE-3 scaffold report

Mode: scaffold, step 7 (FE-3).
- New `models/group-orders/group-pre-order-view.model.ts`: `GroupSeat`, `GroupPreOrderView` (exact contract).
- New `shared/helpers/group-pre-order.helpers.ts`: `buildGroupPreOrderView`, `formatTimeLeft` (bodies throw `Error('Not implemented')`); re-exports the two types.
- Checks: tsc app exit 0; tsc spec exit 0. Karma not run (no spec for the helper yet; pure scaffold, no call sites).
