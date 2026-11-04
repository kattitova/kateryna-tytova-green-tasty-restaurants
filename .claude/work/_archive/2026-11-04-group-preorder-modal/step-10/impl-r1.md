# FE-6 implement r1

Mode: implement, step 10 (FE-6).

Log:
- Read tests-r1.md (6 red) and M03b, M04, M09. Texts follow the plan, not the Figma wording where they differ (M04: "the seat becomes free").
- `copyLink()`: `GuestInviteService.getInviteToken$` (`{ token }`) -> `navigator.clipboard.writeText(buildInviteLink(token))` -> button shows "Link copied" for 3 s; failure -> `toast.error('Could not copy the link. Please try again.')`. Button only when the group is Open. The 3 s reset timer runs outside the Angular zone: inside the zone it kept the app unstable for 3 s, so `fixture.whenStable()` waited for the reset and the spec saw the old text.
- Remove guest: "Remove {name}" link in an expanded guest row (Open only) opens `app-confirm-dialog` with title "Remove {name} from the table?", message "{name}'s pre-order will be deleted and the seat becomes free.", buttons "Keep" / "Remove {name}" (danger). Confirm -> `removeGuest$(groupId, id)` -> `changed = true` -> `reload()`. Keep -> no call.
- Submit: `remainingCount === 0` -> `submit$(groupId, reservationId)` at once; otherwise dialog "{n} seats haven't pre-ordered yet" ("1 seat hasn't ..."), message with the clause by bonus (InProgress 10 %: " and the 10% group discount won't apply"; InProgress dessert: " and the free dessert bonus won't apply"; else none), buttons "Wait for guests" / "Confirm anyway". Success -> `changed = true` -> `reload()`; error -> the button is enabled again (the interceptor shows the toast). Double click is blocked while `submitting`.
- Escape and the modal close button close only the open dialog first (`onClose`).
- Decision: "{ordered} pre-orders" uses the singular for 1.
- Spec and docs not touched.

Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 395 of 395 SUCCESS, 0 failed.
Build: "Bundle check passed"; initial total 842.87 kB raw / 178.89 kB transfer (836.24 kB before). Style budget: `group-pre-order-modal.component.scss` 4.91 kB (908 bytes over the 4 kB warning; was 4.58 kB). `confirm-dialog.component.scss` no warning.
Not checked: layout of the dialog overlay in a browser (it is absolute over the modal card; the card scrolls with overflow-y, so a scrolled card could place it off screen); real clipboard permission in a browser.
