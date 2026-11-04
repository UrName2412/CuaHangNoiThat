# MoMo backend — integration foundation (not enabled for real payments yet)

This project implements MoMo `captureWallet` request signing, payment link validation,
IPN signature/amount/identity verification, and a presentation-only return URL.
An IPN is acknowledged with 204 only after durable processing succeeds.
It never trusts a browser redirect or link creation as evidence of payment.

## Required before enabling

1. Supply the database schema and triggers for `hoadon`, `chitiethoadon`, `sanpham`,
   vouchers and customers. Implement and register `ICheckoutStore` against that schema.
   It must authenticate the buyer server-side, price products/promotions/shipping/vouchers,
   persist the draft before calling MoMo, authorize status reads, and handle duplicate
   checkout keys/IPNs, retries, timeouts, restarts and transaction reconciliation.
2. Only a verified successful payment (`resultCode=0`) may finalize the online invoice
   and update stock, in one database transaction. Ensure existing triggers don't also
   subtract the same stock. Record failed/authorized results without finalizing invoices.
   If a paid draft cannot be fulfilled, persist it for reconciliation/refund, never lose it.
3. Current mobile login only stores a customer locally; it is not a server-verified
   bearer session. Add server authentication and a mobile payment status/resume flow
   before connecting the mobile MoMo selector. Never authorize using the local customer ID.
4. Configure a MoMo merchant/sandbox account, public HTTPS backend/IPN URLs, and
   credentials using environment variables or a server secret manager:
   `MoMo__PartnerCode`, `MoMo__AccessKey`, `MoMo__SecretKey`, `MoMo__Endpoint`,
   `MoMo__RedirectUrl`, `MoMo__IpnUrl`. The sandbox endpoint is
   `https://test-payment.momo.vn/v2/gateway/api/create`; production is
   `https://payment.momo.vn/v2/gateway/api/create`.
   Never bundle merchant keys or Supabase service-role keys in the APK.
5. Complete sandbox callback/replay/reconciliation tests and MoMo merchant approval
   before production. Until then the mobile option is visibly unavailable and the backend
   payment endpoints return 503, preventing unintended charges.

Run locally: `dotnet run --project MoMo.Api`. Expose a properly configured HTTPS
endpoint for MoMo IPN testing; loopback alone cannot receive provider callbacks.

Official references:
- https://developers.momo.vn/v3/docs/payment/api/wallet/onetime/
- https://developers.momo.vn/v3/docs/payment/api/result-handling/notification/
- https://developers.momo.vn/v3/docs/payment/onboarding/integration-process/
