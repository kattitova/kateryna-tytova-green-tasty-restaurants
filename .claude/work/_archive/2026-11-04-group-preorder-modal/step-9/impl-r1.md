# FE-5 implement r1

Mode: implement, step 9 (FE-5).

Log:
- Read tests-r1.md (3 red) and the PNGs M01, M05, M06, M07, M08, M10, M11. Used the project colour tokens, `button button--primary button--extra-large` and the `currency` pipe.
- `group-pre-order-modal.component.ts`: loads `getOwnerGroupId$` then `forkJoin(getSessions$, getStatus$)`, builds the view with `buildGroupPreOrderView`; states loading (spinner), error (`data-testid="gpo-error"`, text "Could not load the group pre-order.", no toast) and content; `reload()` repeats the last two calls for the stored group id; Escape or close emits `closed({ changed })` (`changed` stays false in this step; FE-6 and FE-7 set it).
- Template: header line (address, date, time, guests); "Table" with `gpo-ordered-summary`; seats row (rings; arrows scroll the row when more than 7 seats); bonus box with the texts of the plan table for each state and tier, progress bar while InProgress; "My Pre-Order" row (collapsible, M07 text when not ordered); "Guests' Pre-Orders" with Expand all / Collapse all, collapsible guest rows, "Waiting" for a not-submitted guest, empty state box (M06); footer with `gpo-grand-total` (final total), breakdown, bonus line (achieved 10 %: total before discount, discount, final total).
- Submitted (M10): badge "Sent to Restaurant" in `[modal-title-addon]`, green box "Pre-order sent to the restaurant", no submit button. Open: "Submit Pre-Order" button, disabled when `!canSubmit`, no click handler (FE-6).
- `modal.component.html/.scss`: the title and the addon sit in one `.modal-heading` flex row (an addon in its own row would not sit next to the title as in M10).
- Dashboard wiring was already in the scaffold (signal, `(groupPreOrder)`, `closeGroupPreOrder` reloads reservations and refreshes the cart when `changed`).
- Not in this step (left out on purpose): Copy pre-order link, Remove guest, Add my dishes, seats stepper (D2), edit buttons.
- Spec and docs not touched.

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 388 of 388 SUCCESS, 0 failed.
Build: "Bundle check passed"; initial total 836.24 kB raw / 177.59 kB transfer (814.64 kB before; the page imports the modal eagerly). Style budget: `group-pre-order-modal.component.scss` 4.58 kB, 583 bytes over the 4.00 kB warning budget (warning only; several older files exceed it too).
Not checked: the layout in a browser (seat ring look, scroll height, arrows threshold of 7 seats is a fixed estimate); the new footer/box texts only through the 3 spec tests.
Follow-up: split or trim the scss to meet the 4 kB budget; `check-circle.svg` is the existing icon (colour not compared with Figma).
