# FE-1 tests r1
File: frontend/restaurant-frontend-app/src/app/auth/token.interceptor.spec.ts (authService spy now shared; new describe 'owner and guest routes', tokens { idToken: 'id-1' }).
Tests (3): /guest/groups/g1/sessions gets Bearer id-1 (red); guest session POST keeps Bearer guest-token (passes); /guest/invites/t1 has no Authorization (passes).
tsc -p tsconfig.spec.json --noEmit: exit 0.
npm test (ChromeHeadless): 367 executed, 1 failed, 366 success. Failure: the owner-groups test (no Authorization header, expected Bearer id-1).
Existing tests: all pass.
