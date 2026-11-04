# BE-4 docs review r1

Verdict: approve

Checked against GuestSessionService, GroupPreOrderService, GroupPreOrderRules, GroupBonusCalculator, GroupReservationStatusResponse, GroupBonusResponse, GetGroupReservationStatusHandler, RouteRegistry.cs:195.
- Status route, keys, enum names, tier/discountPercent/deadline rules, Missed deadline (start-2h), inclusive limits, orderedCount, 400/404/500 bodies: match code.
- Submit: both new 400 messages exact; check order (nothing-to-submit before location) correct; owner order creation fields and no double add match; `[]` response correct.
- Sessions/delete `owner` (id `owner-{id}`, name `Owner`, isSubmitted = has own items): match; delete reuses BuildSnapshotAsync.
- ADR amendment and tech-debt 2.1/2.4 consistent with the plan.
- STE lint: ApiRoutes.md 0 violations, ADR 0002 0 violations, tech-debt.md 0 violations.

Findings: none blocker/major.
- [minor] not checked: "Z" in deadline relies on DateTimeKind.Utc (Before() sets it); needs deployed stack.
- [minor] ApiRoutes.md still has no newline at end of file (pre-existing).
