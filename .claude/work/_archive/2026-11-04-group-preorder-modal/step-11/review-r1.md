# Review r1, step 11 (FE-7)

Verdict: approve with minor

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 397 of 397 SUCCESS, 0 failed. Only the 4 modal files changed (no test/prod split violation: the test-writer touched the spec, the implementer touched ts/html/scss).

Verified
- Item list: built from `sessions.owner.items`. Backend `OwnerOwnItems` takes the order items; in an Open group the order holds only owner dishes (guest quantities are subtracted only for Submitted groups, where editing is hidden). No guest dish leaks, no duplication.
- Empty list: `editPreOrderByReservationId` calls cancel on an empty list. Remove of the last dish cancels the order; the row then shows "Add my dishes".
- "-" disabled at quantity 1 and while saving. Double click ignored by the `editing` guard.
- Open only: edit rows and "Add my dishes" are inside `v.isOpen`; Submitted/Closed show a read-only list.
- a11y: aria-labels "Decrease/Increase/Remove {dish}"; images have empty alt and aria-hidden. The nested-button problem is gone.
- Add my dishes: emits `closed({changed})` and navigates to /menu with `buildPreOrderQueryParams`. Test covers it.
- Error path: `editing` is reset and the interceptor shows the toast. No test for the error path (minor).

Findings
- [minor] [tests] group-pre-order-modal.component.spec.ts: no tests for "-" disabled at 1, remove-last-dish, error path (`changed` stays false, buttons enabled again), or read-only in Submitted.
- [minor] [code] group-pre-order-modal.component.scss: 5.52 kB, over the 4 kB style budget warning (already noted in impl-r1 as follow-up). Add to docs/tech-debt.md.
- [minor] [code] `ownItems()` comes from the sessions snapshot, which skips dishes that no longer exist. An edit then drops those dishes from the order. Rare; acceptable, note in tech debt.

Whole-task check (fa6b784..working tree)
- Backend: `OwnerOwnItems`, bonus and `Owner` in sessions match the frontend models (`group-sessions-response`, `group-status`). ApiRoutes.md updated in the same task. No cross-step inconsistency found.
- Frontend: token interceptor, mocks, service and helpers are consistent with the modal. Not re-run: backend tests (not in scope of this step).
