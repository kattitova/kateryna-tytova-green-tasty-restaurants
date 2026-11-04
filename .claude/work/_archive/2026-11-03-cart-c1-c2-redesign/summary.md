# Summary: cart redesign C1 / C2

Mode: `--lite`. Branch: `claude/vibrant-clarke-x333eh`. Started 2026-11-03T13:55:16Z.

## Result

The header cart (`src/app/components/cart/`) now matches the Figma screens C1 and C2. The cart reads `guestCount` from `GET /reservations`, one time for each cart open, and maps each order to a mode:

| Mode | Condition | Open order | Submitted order |
|---|---|---|---|
| `single` (C1) | `guestCount` < 2 | "Submit Pre-Order" (submit API) | "Edit" + "Cancel" |
| `group` (C2) | `guestCount` >= 2 | "Save Pre-Order": no API call, toast, cart closes | "Cancel" only |
| `unknown` | load failed, or the reservation is not in the response | button disabled + inline message | "Cancel" only |

No API change. The guests line "Your guests: X of Y · View group pre-order" is not shown (user decision).

## Steps and review rounds

| Step | Content | Review |
|---|---|---|
| Plan | r1 → r4 | r1 changes required (blocker), r2 approve with minor, r3 changes required (NG8008), r4 approve |
| 1 | `CartOrderMode`, `toCartOrderMode`, `MIN_GROUP_GUEST_COUNT` + spec | approve with minor (file name, kept) |
| 2 | Order card: Figma styles, mode-driven CTA | approve with minor (1 test fixed, 1 deferred) |
| 3 | Cart: reservations join, Save, submit guard | approve with minor (1 test fixed) |
| 4 | Cart panel layout and styles | approve (also checked the step 3 fix) |
| 5 | Hide "Edit Pre-order" for group / unknown submitted orders | approve (also checked the whole branch) |

Files changed (frontend): `models/cart/cart-order-mode.model.ts` (new), `shared/helpers/group-reservation.helpers.ts` + spec (new), `components/cart/models.ts` (new), `components/cart/cart.component.{ts,html,scss,spec.ts}`, `components/cart/components/not-submitted-order/*` (4 files), `components/cart/components/submitted-order/submitted-order.component.{ts,html,spec.ts}`, `docs/tech-debt.md` (new).

## Checks (orchestrator run, final state)

- `npx tsc -p tsconfig.app.json --noEmit`: 0 errors. `npx tsc -p tsconfig.spec.json --noEmit`: 0 errors.
- `BACKEND_URL=https://example.com npm run build`: "Bundle check passed", initial total 806.36 kB raw / 171.79 kB transfer. No style-budget warning in the cart files.
- `npm test -- --watch=false --browsers=ChromeHeadless`: 310 executed, 46 FAILED, 264 SUCCESS. The 46 failures existed before the task (baseline 46 failed, 210 passed). No cart test fails. Plain `npm test` does not start Chrome in this container.
- Visual check: Playwright screenshots with a mocked API in `step-4/` and `step-5/`. Positions agree with `figma/c2-cart.png` within a few pixels.

## Audit

`node .claude/hooks/audit-log.mjs --since 2026-11-03T13:55:16Z`: 10 violations, all of type "restricted command (needs an explicit user request)" by `main`. They are the WIP `git commit` and `git push` commands to `claude/vibrant-clarke-x333eh`. The session Stop hook asked for each commit and push. The user did not ask for them in chat. Other entries are warnings for test commands of read-only agents (Karma writes only build output).

## Accepted deviations

- Prices keep the `$17.00` format (Figma shows `$17`).
- The reservation caption and the "Add dish" button stay (user decision Q4).
- The cart files keep OnPush, signal `input()` / `output()` and `@app/` imports, against `CLAUDE.md` §5 (the existing code wins).
- A group owner cannot submit from the UI until the future "View group pre-order" modal exists (user decision).

## Tech debt added

`frontend/restaurant-frontend-app/docs/tech-debt.md`: 1.1 "-" button style, 1.2 "unknown" message text, 1.3 console-only errors, 1.4 unused `.list`, 1.5 two group-size constants, 2.1 the 46 failing tests, 2.2 headless test command, 2.3 stale `CLAUDE.md` §4–§5, 2.4 `ApiRoutes.md` drift. All P2.

## Needs a deploy to check

Real data from `GET /reservations` (`guestCount`), the toast on a failed load, and the mobile width.
