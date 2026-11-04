# 03 — Plan review r2: Group Pre-Order modal (owner)

Reviewer: plan-reviewer. Inputs: `00-task.md` (D1–D6 and the user answers after r1), `01-explore.md`, `03-plan-review-r1.md`, `02-plan-r2.md`. I checked the r2 claims against the code at base `fa6b784`.

## Verdict

approve with minor

All r1 findings are fixed. The two build-break majors are fixed correctly against the real call sites. I found 2 new minor items. Neither blocks the plan. The implementer can handle both in the step.

## Findings

### [minor] BE-2 contract — the non-nullable `Bonus` property gives a new CS8618 warning
- Problem: `GroupReservationStatusResponse` is a class with `{ get; set; }` properties, and `<Nullable>enable</Nullable>` is on. A new property `GroupBonusResponse Bonus` that has no initializer gives CS8618. The BE-2 Check requires 0 new warnings in the changed files. "The service always sets it" does not remove the warning.
- Evidence: `BE/Models/GuestSessions/GroupReservationStatusResponse.cs:6-17`; `BE/APIHandler.csproj:7`. The only construction site is `BE/Services/GuestSessions/GuestSessionService.cs:167`, so `required` breaks nothing.
- Required change: in the BE-2 contract, declare `Bonus` as `required`, or give it an initializer. Do not use `= null!`.

### [minor] FE-6 contract — `getInviteToken$` emits an object, not a string
- Problem: the contract passes the result of `GuestInviteService.getInviteToken$(...)` directly to `buildInviteLink(token: string)`. The service emits `GroupInviteToken { token: string }`.
- Evidence: `FE/services/reservation/guest-invite.service.ts:14`; `FE/models/reservations/guest-invite.model.ts:13-15`; the invite modal uses `next: ({ token }) => ...` (`reservation-invite-modal.component.ts:73`).
- Required change: in FE-6, write `({ token }) => buildInviteLink(token)`, so that the contract and the test fixture (`…/join/t1`) use the correct type.

## Earlier findings (round 2+)

- [major] BE-2/BE-3 scaffold compile breaks — **fixed**. Both services are primary constructors with exactly the arguments that the tests pass (`GuestSessionService.cs:27-33` has 6, `GuestSessionServiceTests.cs:35`; `GroupPreOrderService.cs:16-20` has 4, `GroupPreOrderServiceTests.cs:30`). A last optional parameter keeps these calls valid. These are the only `new` call sites in `BE` and `BT`. DI uses `AddSingleton<I, T>` (`RouteRegistry.cs:111,115`). The container resolves a registered service for an optional parameter, and `IOrderRepository` (`:93`) and `ILocationService` (`:107`) are registered. `GetAllSessionsResponse` is a positional record (`GetAllResponse.cs:7`). A non-positional `init` property `Owner` keeps `GetGroupSessionsHandlerTests.cs:58` and `DeleteGuestSessionHandlerTests.cs:67,262` valid. `GroupReservationStatusResponse` is a class with an object initializer, so new members break nothing. The test mocks are `new()` (Moq Loose), so the existing status and sessions tests get a `null` order and empty lists from the new calls, and they stay green.
- [major] FE-2 dev mock `tsc` break — **fixed**. `GroupStatusResponse` and `GroupSessionsResponse` are used only in `group-orders.mock-store.ts:66,71,93` and in `group-order.preview.ts:11`. The preview uses `snapshot()`, so the mock store change covers it. `group-orders-mock.interceptor.ts:51,54,61` only returns the store results. The mock spec reads `snapshot().participants` and `groupStatus().status` only (`group-orders.mock-store.spec.ts:8-18`), so it stays green without changes. The values are in the FE-2 scaffold, so both `tsc` gates stay green.
- [minor] Shared `OwnerOwnItems` rule — **fixed**. BE-3 step 3 uses `GroupPreOrderRules.OwnerOwnItems(order, sessions, group.Status)`. The "nothing to submit" Theory has the "only `Canceled` items" row. FE `canSubmit` uses `orderedCount` from the same rule.
- [minor] Null location — **fixed**. `?? throw new ValidationException("Location not found for the reservation's table.")`, the same as `CreateOrderHandler.cs:156-157`, before the transaction. Note: `LocationService.GetLocationByTableIdAsync` (`LocationService.cs:29-44`) already throws `ValidationException` for a missing table or location. The `??` only protects against a mock that returns null, and it is harmless.
- [minor] BE-1 boundary rows — **fixed**. "All seats ordered, group not open → Achieved / TenPercent" and "now = start − 2h → InProgress / FreeDessert" are in the theories. No other rows were added.
- [minor] FE-5 total test — **fixed**. Fixture 27 + 36 + 73 = 136, `Achieved` 10 % → $122.40 Grand Total and $136.00 before discount. The test fails if the template binds `grandTotal` instead of `finalTotal`. No new test was added.
- [minor] Free-dessert texts — **fixed**. The FE-5 table covers each state × tier pair for the box and the footer. `Achieved` + `FreeDessert` uses the US2 AC1 text "Your reservation received a free dessert bonus for the table". The M09 clause and the M10 text are defined for each tier in FE-6 and FE-5.
- [minor] US2 AC3 — **fixed**. The `Achieved` + 10 % footer shows "Total before discount", the discount amount and the final Grand Total, and the FE-5 test asserts the first and the last value. This is listed under Deviations.
- [minor] Risks as user decisions — **fixed**. The "Accepted user decisions" section matches the user answers in `00-task.md:84-88` (display-only discount, Q1 creation time, Q2 legacy = on time, Q4 public status with `orderedCount` and `bonus`, Q5 `button--small` on customer cards, Q3 and Q6 defaults).
- [minor] FE tech-debt 2.4 — **fixed**. BE-4 updates `frontend/restaurant-frontend-app/docs/tech-debt.md` 2.4 (status drift) and 2.1 (the 46-failures baseline is out of date; the explorer measured 364/364, `01-explore.md:211`). The implementer zone allows this file (`.claude/hooks/rules.mjs:42-45`).
- Optional cut of the FE-2 `getStatus$` test — done.

## Test volume (user: do not inflate)

About 47 cases over 11 steps. Each new case in r2 is one of the boundary rows or the regression rows that r1 asked for. One case was cut. No step adds exhaustive tables. The volume is lean and acceptable.

## Confirmed

- `ResponseBuilder` serializes the body with default `JsonSerializer` options (`Helpers/ResponseBuilder.cs:77`), so the BE-1 JSON test with the default options is the right check for the nullable `Tier` enum and the `Z` deadline.
- The existing `GroupPreOrderServiceTests` that BE-3 rewrites exist with the planned names (`:248,261,276,291,306`). `SubmittedOwnerSessionWithDishes_IsIncluded` (`:363`) stays valid: `Consolidate(sessions.Where(IsSubmitted))` includes a submitted owner session, and `OwnerOwnItems` for `Submitted` subtracts the same set.
- `GuestSessionService.cs:157-173` (status), `:176-198` (delete, sessions) and `:200-223` (`BuildGuestSnapshotAsync`) are the lines that BE-2 changes. `SubmitCartAsync` sets `IsSubmitted` at `:245-254`.
- The `/guest/groups/` routes are only the 4 owner and public routes (`RouteRegistry.cs:195,199-201`). No frontend guest flow calls them today, so the FE-1 condition does not change the guest token flow.
- `token.interceptor.ts:23` is the single exclusion. The normal branch sets `Authorization` only when tokens exist (`:72-79`).
- `assets/svg/trash.svg`, `.button--small` (`shared/styles/buttons.scss:61`), `ModalComponent` default and `[modal-actions]` slots (`modal.component.html:18,22`) exist. The additive `[modal-title-addon]` slot does not change the existing slots.
- `PreOrdersService.editPreOrderByReservationId` (`pre-orders.service.ts:73`) reads only `orderItems[].dish.id` and `quantity`, so the widened parameter type is safe. An empty list calls `cancelPreOrderByReservationId`, as FE-7 says.
- `Owner` is `Participant?` in the backend type (to keep the scaffold green) and required in the frontend model. The service always sets it, and BE-4 documents it. This is acceptable.
- Contracts: no route table change; `ApiRoutes.md` and the FE models change in the same task; no new AWS resource and no infra review; no new packages.
- Explorer blocking questions are answered by D1–D6. The plan has no open questions.
