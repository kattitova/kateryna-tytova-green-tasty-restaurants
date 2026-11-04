# FE-1 implement r1

Mode: implement, step 5 (FE-1).

Log:
- Read tests-r1.md (1 red: owner-groups test).
- `token.interceptor.ts`: guest-route condition is now `req.url.includes('/guest/') && !req.url.includes('/guest/groups/')`; comment updated. `/guest/groups/*` now follows the normal path (Cognito Bearer, normal error handling).
- Spec and docs not touched.

Checks: `tsc -p tsconfig.app.json --noEmit` exit 0; `tsc -p tsconfig.spec.json --noEmit` exit 0; `npm test -- --watch=false --browsers=ChromeHeadless`: 367 of 367 SUCCESS, 0 failed.
Not checked: owner calls through the real Cognito authorizer (needs the deployed stack).
