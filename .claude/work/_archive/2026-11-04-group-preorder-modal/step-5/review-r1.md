# Review step 5 (FE-1) r1
Scope: token.interceptor.ts, token.interceptor.spec.ts. Maker/checker split OK (impl touched only the .ts; spec only by test-writer).
Findings: 0 blocker, 0 major, 0 minor.
- Guest branch condition now excludes only `/guest/groups/`; `/guest/invites/*` and `/guest/sessions/*` still take the unchanged guest path (no header added, same toasts). Matches D3.
- `/guest/groups/*` falls through to the normal path (Cognito Bearer, normal error handling). Error handling code unchanged.
- Tests: 3 new, behavioural (header value asserted; guest token kept; no header on invites). Owner test was red before the change.
Checks: tsc app exit 0; tsc spec exit 0; Karma ChromeHeadless 367 of 367 SUCCESS, 0 failed.
Verdict: approve
