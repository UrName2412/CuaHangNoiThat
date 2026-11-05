# Production readiness: current blockers and validation

The app's service changes pass offline checks, but the following backend issues prevent a production readiness claim. Full Android packaging still needs validation. No production database was changed during this work.

## P0: Customer identity is not enforced by the backend

`SupabaseService.LoginAsync` reads `dangnhap.matkhau` through REST and verifies it on the device. `RestoreSessionAsync` accepts a serialized `AuthenticatedUser` from local secure storage. Data requests use the public anon key as the bearer token, and many requests select or update rows using the customer ID held by the client. Consequently, this client session does not provide a backend verified identity for per customer RLS. Registration, default addresses, and profile updates now use direct table requests and no longer call custom RPCs.

Before handling real customer data, move login and password verification behind Supabase Auth or a trusted backend. Bind customer rows to a server verified identity, enforce RLS for reads and writes, and remove public access to password hashes. Review all existing RPC grants and direct table policies as part of that migration. A local UI or storage change alone cannot enforce this boundary.

## P0: Order creation can leave partial orders

`SupabaseService.CreateOrderAsync` inserts a `hoadon` and then inserts each `chitiethoadon` in separate REST requests. A failed or timed out detail request can leave an invoice with no or incomplete items. Retrying can create another invoice. An HTTP timeout during the invoice insert can also leave the client unsure whether an order exists.

Replace this with a transaction based backend operation that validates customer, address, inventory, prices, shipping, and voucher under one database transaction and accepts an idempotency key. Deploy and test it against a disposable database before switching the client. The current repository does not contain a complete schema or a trusted identity mechanism to implement and verify that transaction safely in isolation.

## P1: Default addresses and profiles use multiple writes

`SupabaseService.AddAddressAsync` inserts the new address before setting it as default. Changing defaults sets the selected address first, then clears other defaults. Failure can leave multiple defaults, with a message to retry the selection. Profile changes update the login name and then the customer; definitive customer-write rejection triggers a best-effort restoration of the old login name. Ambiguous failures require reloading the current state. These operations use existing table access because deploying database functions is unavailable; they do not provide transactions across requests or across devices.

## Validation still required

- Verify existing table permissions and RLS against a disposable database matching `db_design.md`. The current client does not require deploying the legacy RPC migration files. `db_design.md` enables RLS but does not define access policies, so it alone does not establish that the current API role can read or write the tables.
- Test login, cart, checkout, order history, and the Account → Orders → Home → Account tab path on an Android emulator or device. No device was connected for this iteration.
- Test the rendered layouts at common phone widths and increased system font sizes. The Android build validates XAML compilation, not visual layout.
