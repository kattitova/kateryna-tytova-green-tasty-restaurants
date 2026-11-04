# FE-4 fix r1

Mode: fix, step 8.
- Review minor 1 (icon not top-right): fixed. `card-reservation.component.scss`: in the `with-table` line the address `.details:first-child` has `margin-right: auto`, so the status badge and the cancel icon sit at the right end. Under 837 px (column layout) `.cancel-icon` has `align-self: flex-end`.
- Review minor 2 (waiter test): for test-writer.
- Checks: tsc app exit 0; karma ChromeHeadless 385 of 385 SUCCESS. Layout not checked in a browser.
