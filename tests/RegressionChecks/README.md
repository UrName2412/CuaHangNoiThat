# Regression checks

Run from the repository root:

```powershell
dotnet run --project tests/RegressionChecks/RegressionChecks.csproj
```

This executable compiles the actual cart/checkout controllers, models, Supabase
service and MoMo gateway. Platform storage/navigation and HTTP are replaced by
local fakes. It checks cart selection and stock limits, shipping totals, selected
checkout items, COD completion, retained unpurchased items, duplicate submission
guard, unconfigured MoMo handling, registration payloads and gateway signatures.
Catalog checks cover product-type/wood mapping and combined attribute filters, draft/apply/
cancel/reset behavior, invalid prices, retained category/search and product reloads.
The product type follows `sanpham.id_kieudang` through `kieudang.id_loaisp` to
`loaisp`; fixtures also cover duplicate style names belonging to different types.

It does not contact Supabase or MoMo, exercise Android bitmap decoding, verify
database transactions/triggers, or prove a real payment integration is working.
