# Task: Cart redesign — screens C1 and C2

Mode: `--lite`. Started: 2026-11-03T13:55:16Z.

## User task (verbatim, Ukrainian)

> я додала тобі посилання на макет (екрани С1 та С2) Cart за цим макетом треба підправити існуючу корзину (стилі)
> та логіку відображення кнопки: коли бронь була на одну людину, то екран С1
> коли брнь була на групу, то екран С2
> https://www.figma.com/design/7h25yCbLdVjuctyhYSjJVt/EPAM-IT-Marathon-6.0--Copy-?node-id=115001-49572

## Translation

Update the existing cart (styles) to match the Figma screens C1 and C2. Also change the
logic that selects the footer button:
- reservation for one person (owner only, 1 guest) → screen C1;
- group reservation (more than 1 guest) → screen C2.

## Figma facts (collected by the orchestrator with the Figma MCP; subagents have no Figma access)

File key `7h25yCbLdVjuctyhYSjJVt`.
- C1 frame `120069:30083` "C1 · Cart — owner only (1 guest): Submit", cart panel node `120069:30119`.
- C2 frame `120069:30376` "C2 · Cart — group reservation: Save", cart panel node `120069:30412`.
- Rendered C2 panel: `figma/c2-cart.png` in this folder. C1 is identical except the footer.

Cart panel: 640px wide, full height, white, right side, drop-shadow `0 0 5px rgba(195,193,173,0.39)`. Column layout: header / scrollable body / sticky footer.

Header: padding 24px 40px, border-bottom 1px #F7F7F7. Title "Cart" centered, H3 (Poppins 500 26px/40px, #232323). Close icon button absolute top 16px right 16px, padding 8px, 24px icon (existing `assets/svg/close.svg`).

Body: flex 1, overflow-y auto, padding 24px 40px.
- Section title "My Pre-Order": H4 (Poppins 500 18px/32px, #232323). Gap 16px to cards.
- Cards list: column, gap 16px.
- Pre-order card (one per dish): white, radius 8px, padding 16px, gap 16px, drop-shadow `0 0 5px rgba(218,218,218,0.7)`, position relative.
  - Dish picture: 96px circle (radius 50%), object-fit cover, box-shadow `0 0 10px 4px rgba(218,218,218,0.7)`.
  - Container (flex 1, column, gap 16px):
    - Name: Poppins 500 14px/24px #232323. Under it (gap 8px): "Expected ready time - 10 minutes" Caption Poppins 400 12px/16px #232323.
    - Row (space-between, align center): quantity control + price.
      - Quantity: gap 8px. "-" button: white, border 1.4px #DADADA, radius 8px, padding 4px 12px, text Poppins 500 14px/24px #898989. Number: width 16px, centered, 14px/24px 500 #232323. "+" button: same but border and text #0F7A37.
      - Price: Poppins 500 18px/32px #0F7A37, right aligned.
  - Delete icon button: absolute top 8px right 8px, padding 8px, 24px trash icon (existing `assets/svg/trash.svg` matches the Figma delete icon).

Footer: padding 24px 40px, border-top 1px #F7F7F7, column gap 16px.
- Row "Grand Total" (H4 18px/32px 500 #232323) and amount (Poppins 500 26px/40px #0B5B29), space-between.
- CTA button: full width, height 56px, radius 10px, background `linear-gradient(-83.73deg, #0F7A37 0%, #0B5B29 98.29%)`, text Poppins 600 16px/24px white.
  - C1 (owner only, 1 guest): label "Submit Pre-Order".
  - C2 (group reservation): label "Save Pre-Order".
- C2 only, under the button, centered row, gap 4px: "Your guests: 3 of 5 ordered" (Caption 12px/16px 400 #575757), "·" (same style), link "View group pre-order" (Poppins 600 14px/24px #0F7A37, underline), then a 16px chevron-right icon. No `chevron-right.svg` in `assets/svg` (Figma asset download is blocked by the network proxy); closest existing asset is `assets/svg/arrow-right.svg` — the planner decides (reuse or add a small inline/new SVG).

Design tokens in Figma: Neutral 900 #232323, 700 #454444, 600 #575757, 400 #898989, 200 #DADADA, 100 #F7F7F7; Green600 #0F7A37, Green800 #0B5B29.

## Decisions

User answers after plan r1 and review r1 (2026-11-03, translated from Ukrainian):

- **Q1, "Save Pre-Order" (C2).** User's intent: for a reservation with more than 1 guest, the owner makes their own order in the cart and saves it. Later, when the group has also ordered, the owner goes to the reservation card. There will be a "View group pre-order" button there that opens a modal with all orders, and the owner submits the pre-order from that modal. If the owner submits their own pre-order alone, it will be duplicated in the group pre-order. So: Save must **not** call the submit API. The order stays not submitted. Dishes are already saved by `PUT /orders` on each change, so Save makes no new API call (toast + close cart, the planner's recommended option). A group owner must not be able to submit alone from the cart. That the group owner cannot submit from the UI until the group pre-order modal exists is accepted by the user (that modal is a separate future task, not in scope).
- **Q2/Q3, the line "Your guests: X of Y ordered · View group pre-order".** Do not show it. Do not change the token interceptor. Do not add anything to `docs/tech-debt.md` for it. Skip.
- **Q4, reservation caption and "Add dish" button.** Leave unchanged (keep both as they are now).
- Step 5 of plan r1 (guest counter, interceptor) is dropped.

Plan review r2 verdict: approve with minor. `plan.md` = `02-plan-r2.md`. The orchestrator sends the 4 minor findings of `03-plan-review-r2.md` to the makers as binding additions to the plan (no third plan round; the code-reviewer checks them):
- M1 (step 2/4): the footer must sit at the panel bottom also with a short list (flex fill / `margin-top: auto`, not only `position: sticky`). Manual check includes a short-content case.
- M2 (step 3): add a test that `submitOrder` does not submit an order in `'unknown'` mode.
- M3 (step 2): `orderMode` is `input.required<CartOrderMode>()` (no fail-open default). Tests set it explicitly.
- M4: price format `$20.00` (currency pipe) stays; whole-dollar format in Figma is an accepted deviation.

Plan checkpoint (user, 2026-11-03): **approved, with Q5 option (b)**: also hide "Edit Pre-order" for already submitted group orders in `app-submitted-order`. The planner adds this as step 5 (plan r3); a new plan-reviewer checks the delta.

Plan r4 approved (review r4: approve). plan.md = 02-plan-r4.md. Karma here needs --browsers=ChromeHeadless. Pre-existing baseline: 46 failing tests before this task.

Step 2 review r1: approve with minor. Minor 1 (unknown-mode label test) → fix now by test-writer, step 3 reviewer checks it as carried-over. Minor 2 ('-' button grey when enabled) → defer to docs/tech-debt.md (needs design decision).
