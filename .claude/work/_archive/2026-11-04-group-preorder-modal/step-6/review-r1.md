# Step 6 (FE-2) review r1
Verdict: approve with minor

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 371 of 371 SUCCESS, 0 failed.
Maker/checker: impl changed no spec; scaffold touched only production files. OK.
Models match ApiRoutes.md (status: orderedCount, bonus {state, tier|null, discountPercent, deadline|null}; sessions: owner; guest-session-id; submit body/response). URLs, methods, body correct. owner- prefix stripped, error if missing. Mock type-complete (satisfies Participant), no `any`.

### [minor] [tests] group-orders.service.spec.ts — getSessions$ and getStatus$ have no test
- Problem: two GET methods (URL only) are untested.
- Fix: add two HttpTestingController cases for /guest/groups/g1/sessions and /status.

### [minor] [code] group-orders.mock-store.ts groupStatus — bonus is a fixed constant
- Problem: orderedCount is computed, bonus is hardcoded InProgress/10%. Acceptable for preview; note only.
