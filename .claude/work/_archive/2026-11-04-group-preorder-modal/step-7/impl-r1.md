# FE-3 implement r1

Mode: implement, step 7 (FE-3).
- Implemented `buildGroupPreOrderView` (Data shape 7: grand total = owner subtotal if ordered + submitted guests; discount only for `Achieved`, unlock amount only for `InProgress`; round to 2 decimals; seats owner, guests, numbered empty seats; `canSubmit` = Open and orderedCount >= 1) and `formatTimeLeft` (past -> null; days+hours; hours; minutes, minimum 1 min). Pure, no Angular imports. Spec unchanged.
- Decision: days always print hours ("1 day 0 h left"); the plan does not define the zero-hour case.
- Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless result below in the return line.
- Karma: 381 of 381 SUCCESS, 0 failed (10 were red).
