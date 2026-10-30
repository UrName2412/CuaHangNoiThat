# MOKHA mock bank API

Local sandbox for testing Checkout connectivity and payment outcomes. No money is moved, no Supabase data is written, and simulated success is not evidence of a real payment. The MAUI Debug Checkout exposes a separate **Thanh toán thử qua API** action; the existing order confirmation remains independent. Release builds hide and reject this simulation action.

Requires the existing .NET 10 SDK; no new NuGet packages.

Start the API from the repository root:

```powershell
dotnet run --project MockBank.Api --no-launch-profile
```

It listens on `http://127.0.0.1:5099`. Connect an Android Emulator:

```powershell
& 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe' reverse tcp:5099 tcp:5099
```

Run the MAUI Debug build, restore/sign in to the account, add a product, open Checkout and scroll to **Ngân hàng giả lập**. Choose an outcome and run the test. The amount includes the current voucher/shipping calculation. A changed total clears the old receipt. The API must remain running. Debug cleartext HTTP is permitted only for localhost; normal network connections still require HTTPS.

## API

All request/response fields use lowercase snake_case. No card, account, customer or authentication data is collected.

- `GET /health`: sandbox health.
- `POST /sandbox/payments`: JSON `{ "amount": 500000, "currency": "VND" }`, UUID `Idempotency-Key` header. Returns a pending transaction, `simulation: true`, ID and 10-minute expiry. Reusing a key with another amount returns 409.
- `POST /sandbox/payments/{id}/simulate`: JSON `{ "outcome": "succeeded" }`. Also accepts `failed`, `cancelled`, `expired`. Terminal states cannot be changed; repeating the same outcome is safe.
- `GET /sandbox/payments/{id}`: read the authoritative simulated result; pending transactions expire after 10 minutes.

Transactions live in memory and are lost on restart. The server is limited to 1,000 transactions per run. It binds loopback only and has no production banking integration. There is no payment ledger/order association in the existing Supabase schema; adding real payment reconciliation would require a separately reviewed backend/data design. Do not expose the simulate endpoint publicly or treat this server as a production payment gateway.
