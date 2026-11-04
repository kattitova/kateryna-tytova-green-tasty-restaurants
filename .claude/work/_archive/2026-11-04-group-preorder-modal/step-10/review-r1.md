# Step 10 (FE-6) review r1
Checks: tsc app exit 0; tsc spec exit 0; karma ChromeHeadless 395 of 395 SUCCESS, 0 failed.
Maker/checker: only the spec is a test file, changed by test-writer; implementer reports no spec change. OK.

Verified: dialog titles/messages/buttons/clauses match plan (singular seat and pre-order, 10%/dessert/Missed clauses). Remove -> removeGuest$ -> changed + reload; Keep no call. Submit: no dialog when remaining 0; `submitting` blocks double click; error re-enables the button. Escape/backdrop/close call onClose, which closes the dialog first (ModalComponent host listens on document Escape). Timer: outside zone, takeUntilDestroyed on destroyRef, cleaned on destroy. button--danger is defined only in confirm-dialog.component.scss; no global one exists; it works because the element is in the dialog template (emulated encapsulation). Carry-over FE-5 minors (null deadline, formatCurrency): fixed, template hides empty sub-line. M2 destructure OK. D5: no calendar button. OK.

## Findings
### [minor] [code] confirm-dialog.component.html
- Problem: role="alertdialog" + aria-modal but no focus management: focus stays on the Remove/Submit button behind the overlay; Tab can reach the modal content; no aria-describedby for the message.
- Fix: focus the cancel button on open (autofocus/ViewChild in ngAfterViewInit), add aria-describedby; optional focus return on close.

### [minor] [code] group-pre-order-modal.component.ts:copyLink
- Problem: one error handler covers both the token request and clipboard. A token API error shows "Could not copy the link" in addition to the interceptor toast (if it toasts). Second copy within 3 s: the first timer resets the label early.
- Fix: catch clipboard error separately (catchError on the from(...) inner), and switchMap/restart the timer (keep a Subscription or use a Subject).

### [minor] [tests] group-pre-order-modal.component.spec.ts
- Problem: no test for clipboard failure toast, submit error re-enabling the button, Escape closing only the dialog, or the 3 s reset.
- Fix: add these 3-4 tests (fakeAsync for the timer is tricky outside zone; use jasmine clock or skip the timer).

### [minor] [code] group-pre-order-modal.component.scss
- Problem: 4.91 kB, over the 4 kB warning (implementer notes). Warning only. Dialog overlay is absolute in the card; a scrolled card can put it off screen (not checked in a browser).
