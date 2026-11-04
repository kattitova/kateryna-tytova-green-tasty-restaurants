# FE-5 fix r1

Mode: fix, step 9.
- Minor "Until " with null deadline: fixed. `buildBonusBox` builds the "Until ..." part only when `bonus.deadline` exists; the sub-line is empty otherwise and the template hides the empty `<p>`.
- Minor `money()` by hand: fixed. `buildFooterBonusLine` now uses `formatCurrency(value, LOCALE_ID, '$')`, the same formatter as the `currency` pipe. The literal without a placeholder is now a plain string.
- Other minors (template logic, extra tests): not in the coordinator request; the tests finding is for test-writer.
