# Tech debt

Known problems that are not fixed yet. Each item says where the problem is, what can go wrong, and a possible fix. Delete an item when the fix is merged.

## Security

### Admin endpoints have no authorization

- **Where:** `backend/restaurant-backend-app/deployment_resources.json`. `POST /locations`, `POST /tables`, `POST /dishes`, `POST /waiters` and `DELETE /waiters` use `authorization_type: NONE`. The handlers do not check a role.
- **Risk:** anyone who knows the API URL can create or delete locations, tables, dishes and waiter links.
- **Fix:** add the Cognito authorizer to these routes and check for an Admin role in the handlers.

## Backend

### Email validator rejects top-level domains longer than 3 letters

- **Where:** `backend/restaurant-backend-app/dnapp/lambdas/api-handler/Helpers/PersonalDataValidator.cs`, `EmailRegex`.
- **Risk:** users with addresses such as `name@company.info` or `name@shop.store` cannot register.
- **Fix:** allow TLDs of 2 or more letters, and add tests for long TLDs.

### Failed table read in the reservation list is not logged

- **Where:** `GetReservationsForUserHandler.TryGetTableNumberAsync`.
- **Risk:** the response contains `tableNumber: null`, and the logs do not show why.
- **Fix:** log the table ID and the error message in the `catch` block.

## API contract

### `ApiRoutes.md` shows the reservation status as a string

- **Where:** `backend/restaurant-backend-app/docs/ApiRoutes.md`, reservation samples (`"status": "Reserved"`).
- **Risk:** the API sends a number (`0` = Reserved, `1` = Cancelled, `2` = InProgress, `3` = Completed). A client that follows the document breaks.
- **Fix:** change the samples to numbers and add the value table, or add `JsonStringEnumConverter` to `ReservationStatus` and update the frontend in the same task.

## Frontend

### Profile photo has no client-side validation

- **Where:** `frontend/restaurant-frontend-app/src/app/auth/components/profile/general-user-info/general-user-info.component.ts`, `onFileSelected`.
- **Risk:** a user can select any file of any size. Only `accept="image/*"` limits the file picker.
- **Fix:** check the type (JPEG, PNG, WebP), the size and an empty file before the read. Check what the upload endpoint accepts.

### `PlaceOrderWaiterComponent` throws when no user is signed in

- **Where:** `frontend/restaurant-frontend-app/src/app/components/place-order-waiter/place-order-waiter.component.ts`, constructor.
- **Risk:** the component creation fails with an error instead of a handled state.
- **Fix:** handle a missing user without a throw. Then change the spec test "current behaviour: shows an error toast and throws when nobody is signed in".

### Page specs have only a creation test

- **Where:** specs of `register`, `reporting-dashboard`, `waiter-dashboard` and `waiter-pre-order-page`.
- **Risk:** a change in page behaviour does not fail a test.
- **Fix:** add behaviour tests (inputs, rendered output, empty, loading and error states), as `frontend/restaurant-frontend-app/CLAUDE.md` §6 requires.

### Some specs send real HTTP requests

- **Where:** specs that render `feedback-qr-modal` and `waiter-make-reservation`. The test log shows "Error fetching feedback token" and "Failed to load locations" with `TypeError: Failed to fetch`.
- **Risk:** the tests pass now, but they depend on the network and can fail at random.
- **Fix:** provide `provideHttpClientTesting()` or mock the services in those specs.
