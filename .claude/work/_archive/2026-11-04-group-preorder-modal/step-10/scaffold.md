# FE-6 scaffold report

Mode: scaffold, step 10 (FE-6).
- New `shared/custom-components/confirm-dialog/` (ts, html, scss): inputs `title`, `message`, `cancelLabel`, `confirmLabel`, `confirmVariant: 'danger' | 'primary'`; outputs `confirmed`, `cancelled`; minimal overlay markup. The shared button styles have no danger variant, so the dialog scss defines `.button--danger` with the `red-400` token.
- New `shared/helpers/invite-link.ts`: `buildInviteLink(token)` (real); `reservation-invite-modal` uses it. That modal already destructures `{ token }` from `getInviteToken$` (M2), so no change was needed there.
- `group-pre-order-modal.component.ts`: protected method signatures `copyLink`, `askRemoveGuest(participantId)`, `confirmRemove`, `keep`, `submit`, `confirmSubmit`, `waitForGuests`; bodies throw `Error('Not implemented')`; none is referenced by the template.
- Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 388 of 388 SUCCESS.
