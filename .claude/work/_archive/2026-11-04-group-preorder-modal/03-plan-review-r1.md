# 03 — Plan review r1: Group Pre-Order modal (owner)

Reviewer: plan-reviewer. Inputs: `00-task.md` (D1–D6), `01-explore.md`, `figma/*.png` (M00, M01, M02, M04, M05 opened), `02-plan-r1.md`. I checked the plan claims against the code at base `fa6b784`.

## Verdict

changes required

Two major findings. Both are build breaks that the pipeline does not allow. The bonus rules, the D6 submit fix, the rule that the owner's order is not duplicated, the interceptor scope and the card layout are correct in substance.

## Findings

### [major] BE-2, BE-3 — the planned scaffold compile breaks are not allowed in scaffold mode
- Problem: BE-2 and BE-3 plan a scaffold that leaves the test project unable to compile until the test-writer edits the tests. The pipeline says that the scaffold build must pass. `dotnet build restaurant-backend-app.sln` builds the tests too (backend `CLAUDE.md`, Commands table). The implementer cannot edit `Tests/**`, so the implementer's scaffold check fails at once.
- Evidence: `.claude/skills/orchestrate/SKILL.md:89-90` ("This lets the tests compile and fail on assertions"); `.claude/agents/implementer.md:36` ("The build must pass"). The calls that break: `Tests/Services/GuestSessionServiceTests.cs:35`, `Tests/Services/GroupPreOrderServiceTests.cs:30`, `Tests/Handlers/GetGroupSessionsHandlerTests.cs:58`, `Tests/Handlers/DeleteGuestSessionHandlerTests.cs:67,262`.
- Required change: make each scaffold compile against the current tests, or move the call-site update to a test-writer turn that comes before the scaffold check. For example, `Owner` can be a non-positional `init` property of `GetAllSessionsResponse`, so the 3 record calls do not break. For the 2 new constructor parameters, state how the build stays green. If you keep a planned break, record it as an explicit exception that the user approves at the checkpoint, and change the scaffold "Check" lines to match.

### [major] FE-2 — required model fields break `tsc` for the dev mock, which the plan puts out of scope
- Problem: FE-2 adds required fields `owner` to `GroupSessionsResponse`, and `orderedCount` and `bonus` to `GroupStatusResponse`. `GroupOrdersMockStore.snapshot()`, `remove()` and `groupStatus()` return these types and do not set the new fields. The mock store is in the app graph (`app.config.ts` registers `groupOrdersMockInterceptor`) and in the spec graph (`group-orders.mock-store.spec.ts`). So both `tsc` gates fail after FE-2. The plan lists the dev mock under "Out of scope".
- Evidence: `FE/shared/mocks/group-orders/group-orders.mock-store.ts:66-73,93`; `FE/app.config.ts:25`; `tsconfig.spec.json:9`.
- Required change: add the mock store (and the mock interceptor, if it is needed) to FE-2's Files, with the smallest type-complete values (`owner`, `orderedCount`, `bonus`). Keep the larger mock clean-up (owner in `participants`, wrong submit path) out of scope as tech debt. The other option is optional fields, but that makes the real API contract weaker, so the first option is better.

### [minor] BE-3 step 3 — a second "owner has items" rule
- Problem: BE-3 uses `order?.Dishes` is not empty. Data shape 2 (`GroupPreOrderRules.OwnerOwnItems`) leaves out `Canceled` items, and the frontend `canSubmit` uses `orderedCount` (from the same rule). An order that has only `Canceled` items and no submitted guest passes the backend check and submits an order with no active dishes. The modal disables the button for the same data. One decision must live in one place.
- Evidence: plan Data shape 2 and 4, BE-3 contract step 3, FE-3 `canSubmit`.
- Required change: in BE-3, define "owner has items" through `OwnerOwnItems(order, sessions, Open)` and add the case to the "nothing to submit" test, or state why the two rules must differ.

### [minor] BE-3 step 5 — the location can be null
- Problem: `(await locationService.GetLocationByTableIdAsync(...)).Address` dereferences a value that can be null. `CreateOrderHandler` handles the same case with `?? throw new ValidationException("Location not found for the reservation's table.")`.
- Evidence: `Handlers/Order/CreateOrderHandler.cs:155-156`.
- Required change: state what happens when the location is missing (the same 400 as `CreateOrderHandler`, before the transaction).

### [minor] BE-1 tests — two boundaries that matter are missing
- Problem 1: no row has all seats ordered with `groupIsOpen = false`. M10 ("10% off applied" after submit) depends on `Achieved` after the group is `Submitted`. If the implementation checked `groupIsOpen` first, it would still pass every planned row.
- Problem 2: the inclusive boundary for the in-progress state at exactly `now = start − 2h` (expected: `InProgress` + `FreeDessert`) has no row. If the implementation used `<` instead of `≤` there, it would still pass.
- Required change: add these 2 rows to the existing theories. Do not add more.

### [minor] FE-5 test — the grand-total test does not check the discount binding
- Problem: the fixture uses `InProgress`, where `finalTotal == grandTotal` (83). If the template showed `grandTotal` instead of `finalTotal`, the test would still pass. FE-3 tests only the calculation, so no test checks the template.
- Required change: use an `Achieved` 10 % fixture for the footer assertion (for example $136 → $122.40, as in M05), or replace the current assertion with it. This adds no new test.

### [minor] FE-5 contract — the bonus texts for each tier are not defined
- Problem: the contract says "Achieved green box" and "tier text", but it gives the text only for the 10 % tier. US2 AC1 requires "Your reservation received a free dessert bonus for the table" for `Achieved` + `FreeDessert`. The `InProgress` + `FreeDessert` heading ("N more pre-orders — free dessert for the table"?) and the M09 dialog variant are not defined either.
- Evidence: `00-task.md:37-41`; plan FE-5, FE-6.
- Required change: list the text for each `state` × `tier` pair in FE-5. No new test is needed.

### [minor] Coverage of US2 AC3 — the total before discount is not shown
- Problem: US2 AC3 requires the total before discount, the discount amount and the final total. M05 shows only the breakdown (own + guests), the discount line and the final total. The plan follows M05 and does not record the gap.
- Required change: show the pre-discount total (for example in the discount line), or add it to "Deviations" for the user to decide at the checkpoint.

### [minor] Risks — show these to the user as decisions, not only as tech debt
- Frontend-only discount: the modal says "10% off applied", but the restaurant order and the waiter see the full price (`Order` has no discount field). D1 accepts that the frontend shows the totals, but the plan lists this only as tech debt. Show it at the checkpoint as a visible consequence.
- Public status route (Q4): the route has no authorizer. `orderedCount`, the bonus and `deadline` (which gives the reservation start time) become public to anyone who has the group id. The status handler also reads the `Orders` table without authentication. The plan has a simpler option: put `orderedCount` and `bonus` in the owner-only sessions response (it already has the owner, the guests and the group) and keep the status route as it is. Show both options to the user.
- Legacy records (Q2): if no time is known, the result is "on time" and the bonus is `TenPercentDiscount`. Only data from before the deploy is affected, but the result is a money display. The default is acceptable. Ask the user to confirm it.

### [minor] Docs — FE tech-debt 2.4 becomes partly out of date
- Problem: BE-4 fixes the status section drift that `frontend/restaurant-frontend-app/docs/tech-debt.md` 2.4 records. The plan does not say who updates that entry.
- Required change: add the update to BE-4 or give it to the orchestrator's tech-debt step.

## Test volume (user asked: do not inflate)

- The total is about 45 cases over 11 steps. The volume is acceptable. Most cases are a boundary or a user-visible behaviour.
- Missing critical cases: the 2 BE-1 rows and the FE-5 fixture change above. No other case is missing.
- Candidates to cut (optional): FE-2 `getStatus$` URL test (a plain GET, the same shape as `getSessions$`). Everything else is worth its cost. The BE-1 JSON test stays: a nullable enum with `JsonStringEnumConverter<T>` can fail at run time, and `ResponseBuilder` (`Helpers/ResponseBuilder.cs:84-88`) does not register an enum converter.

## Non-blocking questions — which ones go to the user

| Q | Send to user? | Reason |
|---|---|---|
| Q1 owner creation time | Yes (short) | It is a business rule: an owner can create a 1-dish order early and add dishes later, and still be "on time". The default is reasonable. |
| Q2 legacy null = on time | Yes | It affects the money display (see Risks). |
| Q3 no dialog when all seats ordered | No | It matches M09. Keep the default. |
| Q4 public status with bonus | Yes | It is a security choice, and there is an alternative (see Risks). |
| Q5 `button--small` in every customer status | Yes (short) | It is a visual change outside the Reserved row (it also changes "Leave Feedback"). It follows M00 and D4. |
| Q6 no 30-min cutoff for group submit | No | Keep the default. The group closes when the reservation status changes (`ReservationService.cs:117-125`). The risk is small. |

Also show at the checkpoint: the frontend-only discount and the US2 AC3 gap.

## Earlier findings (round 2+)

Not applicable (round 1).

## Confirmed

- Bonus rules and boundaries match US2 AC1/AC2. All seats ordered with the last time ≤ start−24h gives 10 %, and ≤ start−2h gives dessert. A missing order after start−2h means no bonus. The boundaries are inclusive ("at least"). The owner counts as a seat, as in M01 ("4 of 6", You filled) and M05.
- M01 "−10% unlocks at 6 of 6 (−$10)" = 10 % of the current $100 total, so `unlockDiscountAmount` is correct. M05: 136 → 13.60 / 122.40 is correct. FE-5: 27+36+20 = 83 is correct.
- D6: `GroupPreOrderService.cs:41-54` has the 3 errors that the plan removes. `SubmitGroupPreOrderAsync` in the repository uses `AddSaveItem(order)` (a full put), so a new order works. The group condition `StatusIsOpen` makes a second submit fail, so no second order is created (idempotent).
- No duplication: the owner session has no dishes (`GuestSessionService.cs:366-379`). The merge adds only consolidated session dishes (`:104-120`). `OwnerOwnItems` subtracts them again after submit. This is also correct for the D5b case (a `Canceled` owner item becomes the guest quantity, and the subtraction gives 0).
- Group creation: `ReservationService.cs:72-73` creates the group when a reservation is created with `guestCount >= 2`, so `guest-session-id` works for normal group reservations. The 404 risk applies only to `PUT /reservations` from 1 guest, as the plan says.
- `GetOrderByReservationIdAsync` returns the non-cancelled order (`OrderRepository.cs:52`). `CreateOrderHandler.cs:131` → `AddOrderAsync`, `:90` → `AddOrderForWaiterAsync`. These are the right places to set `CreatedAt`. `Order` is mapped through `OrderMapper.ToOrderResponse`, so `createdAt` does not leak into `GET /orders`.
- DI: `IOrderRepository` (`RouteRegistry.cs:93`) and `ILocationService` (`:107`) are registered. `GuestSessionService` and `GroupPreOrderService` are singletons built by the container (`:112,115`). No manual `new` exists. No route table change is needed.
- `CreateOrderHandler.cs:159` has the format string that `OrderMapper.FormatReservationTime` will own. `OrderMapper.cs:30-42` is correct.
- Interceptor: `token.interceptor.ts:23` is the only exclusion. `/reservations/{id}/guest-session-id` does not contain `/guest/`. The status route (public) also gets the header under D3's `/guest/groups/...` wording, and this is harmless. The 3 FE-1 tests cover the owner, guest-with-token and public-guest cases.
- Card: Reserved and InProgress Cancel are shared with the waiter in today's markup (`card-reservation.component.html:127-134,175-182`). The plan correctly keeps the waiter text button. `isGroupReservation` and `buildPreOrderQueryParams` exist. M00 shows the trash icon top-right and small buttons.
- Page: `CartStateService.refresh()`, `firstName`/`lastName` signals and the page modal-signal pattern exist. `ModalComponent` closes on document Escape (`modal.component.ts:8`) and lets the parent decide, so "Escape closes only the dialog" can be done in the parent.
- Contracts: the API changes (status, sessions, delete, submit) are in BE-4 (`ApiRoutes.md:1198,1208,1268,1314` exist; ADR 0002 exists) and the FE models are in FE-2, in the same task. No new AWS resource and no infra review are needed (only new optional attributes).
- Conventions: OnPush, signal API and `@app/` imports follow the card and page, and the plan lists this as a deviation from app `CLAUDE.md` §5. No new packages.
- Step order: the pure rules come first, then the data, the submit, the docs, the frontend data layer and the UI. Each step names its tests, except BE-4 (docs, with a reason).
- Explorer blocking questions 1–5 are answered by D1–D4/D5. The non-blocking defaults are applied, except "update the dev mock" (see the FE-2 finding).
