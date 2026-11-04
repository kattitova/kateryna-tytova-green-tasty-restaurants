# Review step 7 (FE-3) round 1
Verdict: approve

Checks (frontend/restaurant-frontend-app): tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 381 of 381 SUCCESS, 0 failed.
Maker split: only 3 new files (model, helper, spec); no existing file changed.

Checked against plan and AC: totals (owner if ordered + submitted guests; waiting guest excluded), discount only on Achieved, unlock amount only on InProgress, round2; seats owner, guests in API order, empty seats numbered up to seatCount (none if guests exceed it); canSubmit = Open and orderedCount >= 1; initials of first two words; formatTimeLeft (past/zero -> null, days+hours, hours, min with minimum 1). No Angular imports, no `any`. Tests are lean and check behaviour.

Findings: none blocker/major.
- minor (tests): no test for Achieved/InProgress split of unlockDiscountAmount at 0 total, nor for "1 day 0 h" or the 1-min floor; low risk, optional.
