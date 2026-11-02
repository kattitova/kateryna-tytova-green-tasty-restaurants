---
paths:
  - "frontend/**"
---

# Frontend rules (Angular)

Area guide: `frontend/restaurant-frontend-app/CLAUDE.md`. It is the single source of truth for frontend work. Do not copy its rules into other files.

Load these skills before you write or review frontend code:

| Task | Skills |
|---|---|
| Write or change TypeScript, templates, SCSS | `ub-mode`, `angular-developer` |
| Write or change specs | `angular-developer` (Testing section, Karma + Jasmine) |
| Change `README.md` or other Markdown | `asd-ste100` (STE-flavored mode) |

Hard rules:

1. Read the "Project overrides" table in `angular-developer` first. Current conventions win over target conventions (app `CLAUDE.md` §5).
2. Run the four verification gates (app `CLAUDE.md` §6) before you report "done". Report the bundle size and test totals.
3. Use `npm ci`, not `npm install`. Do not add npm packages without the user's approval.
4. When a model disagrees with the API, `backend/restaurant-backend-app/docs/ApiRoutes.md` wins.
5. In the `/orchestrate` pipeline, `test-writer` writes the `*.spec.ts` files and `implementer` never edits them.
