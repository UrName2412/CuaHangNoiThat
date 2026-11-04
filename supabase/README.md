# Database migrations

Apply `migrations/202610310001_atomic_customer_registration.sql` in the Supabase
SQL Editor (or through your normal migration deployment) **before deploying the
updated app**. Registration now calls `POST /rest/v1/rpc/register_customer`.

The function creates `khachhang` and `dangnhap` in one transaction and returns the
inserted `id_khachhang`. A failure in either insert rolls back the transaction.
Concurrent calls for the same trimmed email are serialized; existing customers
and login names are rejected. The function uses the caller's existing table
permissions and RLS policies (`SECURITY INVOKER`); it does not bypass them.

This migration assumes the existing `khachhang.id_khachhang` database default
generates IDs, as required by the app's previous insert implementation.
It does not delete historical customer records that lack a login: those may be
legitimate customers and need to be reviewed before any data cleanup.

Verify against a disposable database: successful registration inserts both
records with the same ID; force the login insert to fail and verify neither
record remains; retry the email after removing the failure; submit two
concurrent registrations for one email and verify only one succeeds.
