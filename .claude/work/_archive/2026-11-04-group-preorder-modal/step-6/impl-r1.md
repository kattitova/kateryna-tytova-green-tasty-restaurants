# FE-2 implement r1

Mode: implement, step 6 (FE-2).

Log:
- Read tests-r1.md (4 red, "Not implemented").
- `group-orders.service.ts`: typed HttpClient calls for the 5 methods. `getOwnerGroupId$` maps `guestSessionId`, strips `owner-`, throws `Error('Unexpected owner session id')` when the prefix is missing (error Observable through `map`).
- Removed an unused `throwError` import.
- Spec unchanged.

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 371 of 371 SUCCESS, 0 failed.
Build `BACKEND_URL=https://example.com npm run build`: initial total 810.98 kB raw / 172.69 kB transfer. Existing budget warnings (initial 500 kB, several scss files) were not caused by this change; the service is not imported by any page yet. I did not rerun a baseline build to compare.
