# Task: Group Pre-Order modal (owner) on the Reservations page

Mode: full. Started: 2026-11-04T11:50:21Z. Base commit: `fa6b784` (main).

## User task (verbatim, Ukrainian)

> наступна задача по реалізації модального вікна Groupe pre-order воно викликається з картки бронювання на сторінці Reservation для цього додати кнопку Group Pre-order (button--secondary), треба одразу продумати проблему місця, бо для owner там вже і так є три кнопки Cancel, Edit, Pre-order
> оптимально розписуй задачі, не роздувай тест-кейси
>
> ось user story на цей функціонал https://docs.google.com/document/d/1U4-r6kd36cWSLg5wlWcdzyqogHYZSFYN3D2kLx03g3s/edit?tab=t.0
>
> макет екрани з М00 по М11 https://www.figma.com/design/7h25yCbLdVjuctyhYSjJVt/EPAM-IT-Marathon-6.0--Copy-?node-id=115001-49572

## Translation

Implement the "Group Pre-Order" modal. The owner opens it from the reservation card on the Reservations page. Add a "Group Pre-Order" button (`button--secondary`) to the card. Solve the space problem now: for the owner the card already has three buttons (Cancel, Edit, Pre-order).
User constraints: **split the work into optimal steps; do not inflate test cases** (test only behaviour that matters, no exhaustive tables).

## Context from the previous task (archived: `.claude/work/_archive/2026-11-03-cart-c1-c2-redesign/`)

- In the header cart, a group owner (reservation `guestCount >= 2`) can only "Save Pre-Order" (no submit). The owner submits the whole group pre-order from THIS modal. The owner's own order must not be duplicated in the group submit.
- Known facts: the token interceptor does not add the Authorization header to URLs that contain `/guest/` (`token.interceptor.ts`), so owner calls to `GET /guest/groups/{id}/sessions` fail with 401 today. Backend endpoints exist for group sessions (list, DELETE a guest session), group status and group submit; see `backend/restaurant-backend-app/docs/ApiRoutes.md` and `RouteRegistry.cs`. `ApiRoutes.md` has drift vs code for group status (see `frontend/restaurant-frontend-app/docs/tech-debt.md` 2.4) — the code wins.
- `MIN_GROUP_GUEST_COUNT = 2` and `toCartOrderMode` exist in `src/app/shared/helpers/group-reservation.helpers.ts`.

## User story (Google Doc, copied verbatim)

**US 1: group owner views a consolidated group pre-order.** As a group owner of a table reservation, I want to see all pre-orders made by my guests, with each guest's dishes and the total order amount, so that I can track who has already ordered, know the group's expected spending in advance, and make sure everything is ready before we arrive.

- AC1 Viewing guests' pre-orders: GIVEN I am the group owner AND at least one guest has made a pre-order via the shared link, WHEN I open the pre-order overview, THEN I see a list of all guests who joined, AND for each guest the ordered dishes with name, quantity and price, AND each guest's subtotal.
- AC2 Total: the total equals the sum of all guests' subtotals, including my own pre-order if I made one.
- AC3 Guest without a pre-order: a guest who joined but has not ordered is shown with status "Waiting" and is not counted in the total.
- AC4 No pre-orders at all: message that there are no pre-orders yet, total 0, option to copy the invitation link.

**US 2: group owner sees the group bonus and can remind guests.** Users value: bonus for ordering in advance. Business value: orders in advance.

- AC1 Bonus tiers:
  - all guests ordered AND last pre-order at least 24 h before reservation time → "10% discount" bonus on the total; highlighted message (doc says "Your reservation received a 100% discount" — typo, design says "Everyone has ordered — 10% off applied").
  - all guests ordered AND last pre-order < 24 h but >= 2 h before → "Free dessert for the table" bonus; message "Your reservation received a free dessert bonus for the table".
  - at least one guest has not ordered 2 h before → bonus not available.
- AC2 Bonus progress: while someone has not ordered and the deadline has not passed: which bonus the group can still get, how many guests still need to order, time left until the deadline, AND the owner can send a reminder to each guest without a pre-order.
- AC3 Total with bonus: with the 10% bonus, show total before discount, discount amount, final total.

## Figma (file `7h25yCbLdVjuctyhYSjJVt`; subagents have no Figma access — use the PNGs)

Screens saved in `figma/` (this folder). Full React/Tailwind reference of M02 with every style value: `figma/M02-design-context.tsx.txt` (a reference only; convert to the project SCSS and tokens).

| File | Screen |
|---|---|
| `M00-on-reservations.png` | Modal over the Reservations page. The owner card in the background shows "Invite Guests" and an outlined "Group Pre-Order" button with an orange dot (notification badge). |
| `M01-default.png` | Default: header (title, address, date, time, guests). "Table · 4 of 6 ordered" + "Copy pre-order link". Seat avatars (You = filled green, ordered guests = light green with initials, empty seats = dashed circles "5 Waiting"). "Guest can't come?" + "Seats − 6 +" stepper. Bonus progress box: "2 more pre-orders — 10% off for the whole table", "Until Oct 16, 12:00 · 1 day 3 h left", progress bar. "My Pre-Order" collapsed row (avatar, dish names, $27, chevron). "Guests' Pre-Orders · 3 guests" + "Expand all". Sticky footer: "Grand Total $100", breakdown "My pre-order $27 · Guests' pre-orders (3) $73", "Group discount -10% unlocks at 6 of 6 (-$10)", "Submit Pre-Order". |
| `M02-expanded.png` | "My Pre-Order · 2 dishes" expanded: dish rows with image, name, − qty +, price, trash. |
| `M03-seat-removed.png` | Seats reduced 6 → 5: header "5 Guests", "4 of 5 ordered", "1 more order". |
| `M03b-link-copied.png` | "Copy pre-order link" becomes "✓ Link copied". |
| `M04-remove-guest-confirm.png` | Confirm dialog "Remove Anna from the table? Anna's pre-order will be deleted and her seat removed. The group bonus will then need 5 orders." Buttons "Keep" / "Remove Anna" (red outline). |
| `M05-bonus-achieved.png` | 6 of 6 ordered: green box "Everyone has ordered — 10% off applied / The discount is included in the group total"; no seats stepper; total $122.40, "Group discount -10% applied (-$13.60)". |
| `M06-no-guest-preorders.png` | 1 of 6 ordered: only You filled, others Waiting; "Guests' Pre-Orders" empty state box (people icon); total = own order $27. |
| `M07-owner-not-ordered.png` | You avatar dashed; "My Pre-Order — You haven't added your dishes yet" + "+ Add my dishes" button; breakdown "My pre-order —". |
| `M08-large-group-slider.png` | 12 seats: avatar row becomes a horizontal slider with ‹ › arrows. |
| `M09-submit-empty-seats.png` | Submit while seats are empty: dialog "2 seats haven't pre-ordered yet / If you confirm now, the restaurant will receive 4 pre-orders and the 10% group discount won't apply. After confirmation, guests can't change their orders." Buttons "Wait for guests" / "Confirm anyway". |
| `M10-sent.png` | After submit: badge "Sent to Restaurant" next to the title; green box "Pre-order sent to the restaurant / 10% off applied · guests can no longer change their orders"; footer button "Add to calendar". |
| `M11-deadline-passed.png` | Grey box with clock "The group bonus is no longer available / Not all seats ordered before Oct 16, 12:00"; breakdown "Group discount -10% — deadline passed". |

Design tokens in M02: Neutral 900 #232323, 700 #454444, 600 #575757, 400 #898989, 200 #DADADA, 100 #F7F7F7, 0 #FFF; Green600 #0F7A37, Green800 #0B5B29, Green100 #E9FFEA, Green150 #C5E8C7, BG Green #D9E5D9; Red400 #B70B0B. Fonts: Poppins; H3 26/40 500, H4 18/32 500, H5 16/24 500, Body 14/24 400, Body bold 14/24 500, Caption 12/16 400, Link 14/24 600, Button L 16/24 600, Button S 14/24 500.

Notable: the design does NOT show a per-guest "remind" action (US 2 AC2) nor the "free dessert" tier — open questions for the explorer/planner to confirm against the backend.

## Decisions

(filled after exploration and the plan checkpoint)

User answers after exploration (2026-11-04, translated):

- **D1 Bonus:** implement on **backend and frontend**. The backend computes the bonus (10% discount if all seats ordered ≥ 24 h before the reservation time; free dessert if all ordered < 24 h and ≥ 2 h before; not available otherwise), the deadline and the progress; the frontend shows it (M01, M05, M11) and the totals with discount (US2 AC3).
- **D2 Seats:** **Remove guest only** (M04 confirm, existing DELETE). No seats stepper; the seat count is display only. ("Seat removed" M03 behaviour only as far as DELETE supports it.)
- **D3 Interceptor:** allowed — add the Cognito token for owner routes `/guest/groups/...` only; guest invite-token routes unchanged; cover with interceptor tests.
- **D4 Card:** follow M00 — Cancel becomes a trash icon in the card's top-right corner; Edit, Pre-order and Group Pre-Order in one row. Group Pre-Order (`button--secondary`) shows only for reservations with ≥ 2 guests in status Reserved.
- **D5 Reminders / calendar:** the "Copy pre-order link" button is the reminder (owner sends the link). **No "Add to calendar" button.** After submit (M10) the footer has no calendar button.
- **D6 Group submit 400:** **fix the backend** to accept a submit when at least one order exists (owner's or a guest's). If the owner has no own order, the backend creates it from the guests' dishes. No duplication of the owner's order.
- Out of scope: notification dot on the card button (no backend data) unless the planner finds it cheap with existing data.
- Backend baseline (orchestrator, after installing dotnet-sdk-10.0 from apt): `dotnet build --no-incremental` 0 errors, 97 warnings; `dotnet test` 337 passed, 0 failed. Frontend baseline (explorer): tsc 0/0, Karma 364/364 passed.
- Deploy: backend changes need a `syndicate deploy` by the user; nobody runs it in this task.

User answers after plan r1 / review r1 (2026-11-04):
- Q-discount: **display only** (frontend computes the discounted total; storing the bonus on the order for the waiter = tech-debt follow-up).
- Q1/Q2: owner time = order **creation** time; records without timestamps (before deploy) count as **on time**.
- Q4: `GET /guest/groups/{id}/status` stays **public** and returns `orderedCount` and `bonus`.
- Q5: `button--small` on **all customer cards** in every status (waiter card unchanged).
- Q3, Q6: planner defaults kept (no dialog when all seats ordered; group submit keeps today's time/status rules).

Plan review r2: approve with minor. plan.md = 02-plan-r2.md. Binding additions for the makers (no third plan round; code-reviewers check them):
- M1 (BE-2): `GroupReservationStatusResponse.Bonus` must be `required` (or initialized) — no new CS8618 warning.
- M2 (FE-6): `getInviteToken$` returns `{ token }`; destructure before `buildInviteLink(token)`.

Plan checkpoint (user, 2026-11-04): **approved; commit and push to main after each approved step.** Budget constraint: the account has about $31; do not start a new step that cannot be finished within the budget. Orchestrator decision: run makers and reviewers on a cheaper model (sonnet), one review round per step where possible, short prompts.

BE-1 done: review r1 approve (optional minor: partial-subtraction test for OwnerOwnItems → defer). Commit a5ac82d pushed to main.

BE-2 done: review r1 approve with minor (MinValue clamp kept only for default-start tests → defer). Commit pushed.

BE-3 done: review r1 approve (note: real DynamoDB transaction with a new order needs a deploy to check). Commit pushed.

User (2026-11-04): balance $27; keep sonnet for subagents; continue BE-4 then FE-1. Backend BE-1..BE-3 is deployed by the user.

BE-4 done: review approve. Committed.

FE-1 done: review approve. Committed.

FE-2 done: review approve with minor (no spec for getSessions$/getStatus$ — accepted as lean, the plan cut it). Committed.

FE-3 done: review approve (optional minor: extra formatTimeLeft edge tests → skipped, lean). Committed.

FE-4 done: review approve with minor. Minor 1 (trash icon not top-right) → fix now, checked by the FE-5 reviewer as carried-over. Minor 2 (waiter card test) → skipped (lean). Committed.

FE-5 done: review approve with minor. Fix in FE-6: InProgress null deadline shows 'Until ' (hide the line), footer money() → currency pipe. Defer: small template logic, few modal tests (lean). Committed.

FE-6 done: review approve with minor (dialog focus/aria-describedby, copyLink single error handler + overlapping copies, missing error-path tests, scss 4.91 kB) → defer to tech-debt. Committed.
