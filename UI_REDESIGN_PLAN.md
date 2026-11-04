# MOKHA Living UI/UX Redesign Plan

## Progress

- [x] Phase 1 – Project Audit (source inventory completed; full runtime audit remains for verification phase)
- [x] Phase 2 – Design Direction
- [x] Phase 3 – Design System (shared tokens/styles implemented; Android x64 build verified)
- [x] Phase 4 – Screen Redesign (all existing screens reviewed/redesigned; Android x64 build verified; emulator review tracked in Phase 6)
- [x] Phase 5 – Responsive/accessibility and interaction implementation complete; build and supported emulator checks verified. Full spoken TalkBack review remains a Phase 6 verification limitation.
- [ ] Phase 6 – Build, emulator and regression verification (build and smoke verification run; full regression remains pending)

## 1. Project Architecture

- Framework: .NET MAUI, project currently targets `net10.0-android36.0`; nullable and implicit usings enabled.
- UI: XAML pages under `Views`; Shell tab navigation in `AppShell.xaml` with Home, Catalog, Cart and Account tabs. Orders is an existing page, and other flows use Shell routes.
- MVVM: CommunityToolkit.Mvvm 8.3.2; page controllers use observable state and relay commands. Code-behind performs page lifecycle/data-load tasks.
- Dependency injection: services/controllers registered through `MauiProgram.cs`.
- Supabase: `Services/SupabaseService.cs` calls Supabase REST; configuration loaded from a raw asset. Existing C# DTOs map API payloads.
- Resource system: `App.xaml` merges `Resources/Styles/Colors.xaml` and `Styles.xaml`, then adds app styles. No separate reusable ContentView component library was found in source inventory.
- Assets: Open Sans Regular/Semibold fonts, navigation SVG icons, several category SVGs; no third-party UI package beyond CommunityToolkit.Mvvm and BCrypt.
- Images: product DTOs carry image values and product pages bind them to `Image`; verify runtime behavior and fallback per page before changing.

## 2. Screen Inventory and Functionality Audit

| Screen | Binding/controller | Existing features evidenced in source | UI review focus | Logic risk |
|---|---|---|---|---|
| Home | HomeController | Load products, dynamic category/product display, address/catalog/product-detail navigation | Shared header/search, category and product presentation | Medium: preserve existing load/search/navigation |
| Catalog | CatalogController | Load products, category selection, product detail, add to cart | Filter chips, responsive product grid, empty/loading/error | High: preserve filtering and cart commands |
| Product detail | ProductDetailController | Load by product id, add to cart | Image/data hierarchy, long content, fixed purchase action | High: preserve product id and stock logic |
| Cart | CartController | Session cart, update quantity, remove, checkout navigation | Item density, total and empty state | High: preserve calculations and item lifecycle |
| Checkout | CheckoutController | Address selection/management, voucher apply, order confirmation | Form clarity, summary and validation feedback | Very high: preserve order and pricing workflow |
| Account/auth | AccountController | Sign-in, registration, profile entry, orders/cart/address navigation, logout | Signed-in/out states, form accessibility and busy/error states | Very high: authentication behavior must not change |
| Profile | ProfileController | Update personal info and password | Form grouping, validation and loading feedback | High: preserve profile/password operations |
| Address | AddressController | Load/add/delete/set default addresses | Empty state, clear default and destructive action feedback | High: preserve address ownership/default behavior |
| Orders | OrdersController | Load/refresh order history | Status/data hierarchy, empty/loading/error | High: preserve status values and order navigation |
| Promotions | PromotionController | Load promotions/vouchers and copy voucher | Show only actual available data and clear copy feedback | Medium: preserve voucher values and command |

No standalone authentication, search, category-detail, or order-detail page was found in the current Views inventory. Search and category selection are implemented within Home/Catalog; order details appear to be represented in the orders flow only if the controller/page supports them (confirm before redesign).

## 3. Database Mapping and Data Constraints

- The attached SQL snapshot defines legacy uppercase identifiers such as `SANPHAM`, `KHACHHANG`, `DANGNHAP`, `DIACHIGIAOHANG`, `HOADON`, `CHITIETHOADON`, `LOAISP`, `LOAIGO`, `KIEUDANG`, `CHUONGTRINHKHUYENMAI`, `KMTHEOVOUCHER`, plus supplier/employee/inventory tables.
- The current Supabase REST service uses lowercase paths/fields (for example `sanpham`, `khachhang`, `diachigiaohang`, `id_khachhang`, `id_diachi`). The pasted SQL appears to rely on unquoted identifiers, which PostgreSQL folds to lowercase; preserve the actual lowercase identifiers used by Postgres and the existing service.
- The mobile app is customer-facing. Employee, supplier, and inventory-management data are outside redesign scope.
- Existing product DTO fields include product id, name, price, quantity/stock, style/wood references, image, and description. Do not invent ratings, materials, dimensions, brand, discounts, or collections without verified fields and logic.
- Address, promotion/voucher, order and order-item data are present in DTOs/service methods. Cart is session state in the client, not a database-backed cart.
- No schema migrations, API/query changes, or assumed foreign-key nested selects are part of UI work.

## 4. Existing Design Direction (Phase 2)

- Brand direction: warm minimalism / contemporary Scandinavian furniture retail. Product photography and real product data lead; avoid admin-dashboard framing and fabricated promotional content.
- Retain the existing earthy terracotta identity and warm ivory backgrounds. Consolidate to semantic tokens rather than introducing a second competing palette.
- Proposed palette based on current resources: page `#FAF8F5`, surface `#FFFFFF`, primary terracotta `#9A4F24`, dark text `#271B15`, secondary text `#756C65`, muted surface `#F3F0EC`, supporting sand `#F3E7DA`, success `#477A5B`. Review contrast before implementation; use terracotta primarily for actions/selected states.
- Typography: reuse Open Sans Regular/Semibold (Vietnamese-capable installed assets); establish a compact scale for page title, section heading, card title, body, caption and labels. Avoid adding fonts/dependencies until rendering is verified.
- Layout: consistent page gutters, compact but breathable vertical rhythm, 44dp minimum action targets where practical, 16dp-radius surfaces, lightweight dividers, image-first product cards, and persistent current Shell navigation.
- Interaction: use existing loading/busy/error and empty data where present; make selected, disabled, pressed and validation feedback visible without changing command semantics.
- Responsive direction: use adaptive Grid/collection layouts and scrollable content; keep critical cart/checkout actions visible without obscuring content. Validate phone first and wider sizes if supported.
- Reuse-first: promote existing colors/styles and existing XAML patterns into shared styles/templates; add shared components only where repeated structures justify them.

## 5. Design System Specification (planned for Phase 3)

Implemented in existing resource dictionaries: semantic Danger, BorderSubtle, SurfaceElevated and FocusRing color tokens; spacing values 8, 12, 16, 24 and 32; PageTitle, SectionTitle, CardTitle, BodyText, Caption and Eyebrow typography; Card and FieldSurface borders; Primary, Secondary and Danger button styles. App page background now references the shared PageBackground token. Status badge and product image fallback remain screen-level work for later phases.

## 6. Shared Components

Current source review found global styles (`Card`, `SectionTitle`, `Eyebrow`, `MutedLabel`, `PrimaryButton`) and app-wide control styles, but no dedicated reusable ContentView components. Phase 3 will first normalize these existing resources. Product card, section header, empty/error/loading surfaces and image fallback may become shared XAML templates if repeated page layouts support it.

## 7. Redesign Checklist

- [x] Phase 3: Design tokens and shared styles/components implemented/build-verified
- [x] Phase 4: Shell/navigation visual consistency reviewed (current Shell already uses shared tab colors; routes retained)
- [x] Phase 4: Home and Catalog implemented/build/UI-verified on phone; Catalog also reviewed at tablet-size and 130% text
- [x] Phase 4: Product Detail implemented/build/UI-verified on phone
- [x] Phase 4: Cart and Checkout implemented/build/UI-verified on phone; regression coverage documented below
- [x] Phase 4: Account, Profile and Address implemented/build/UI-verified on phone; write actions not fully tested
- [x] Phase 4: Orders and Promotions implemented/build-verified; Orders phone UI verified, Promotions runtime review pending
- [x] Phase 5: responsive layouts, semantic labels/headings, keyboard focus and interaction states implemented and checked within the scope below
- [ ] Phase 6: build, Android emulator screenshots and feature regression checks

## 8. Implementation Progress

Phase 4 redesign completed across all existing customer screens. Phase 5/6 checking is underway: catalog uses idiom-adaptive columns (2 phone / 3 tablet / 4 desktop); Home search has a 44dp minimum touch height; Address input scrolls with its list; shared buttons show focus/pressed/disabled states; semantic descriptions/hints were added to key interactive cards/actions. Emulator review found and fixed low-contrast Status Bar icons on pushed pages, overlapping Set Default/Delete actions in Address cards, and the fixed Cart/Checkout footer obscuring landscape content. Rebuilt and installed the fixes, then checked Pixel 7 API 36 on portrait and landscape. Home, Catalog, Product Detail, Cart (empty/populated/remove/quantity), Checkout navigation/data summary, Account (signed-in), Profile, Address and Orders were opened; the four tab destinations were exercised. No crash signature was found in the recent logcat window. `git diff --check` reported no whitespace errors (Git emitted line-ending normalization warnings). Remaining checks: signed-out/auth and logout flows, checkout submission (intentionally not sent), voucher/promotion screen, screen-reader operation, and tablet. No service/query/database contract changed. The database returns at least one mismatched product image and some items fall back to the placeholder; UI cannot correct that source data.

## 9. Testing Results

### Phase 5 closeout

- Final changes: shared implicit buttons now have a 44dp minimum width plus focused/pressed/disabled states; page/section labels have semantic heading levels. Home category/product cards and Account navigation rows now expose native focusable Buttons instead of only gesture targets. Checkout address choices have an explicit native button. Product image descriptions/add-to-cart descriptions include the product name; decorative placeholders are excluded from the accessibility tree.
- Fixed observed Home discount-row crowding at 130% text by separating current price from original price/discount on two rows and increasing carousel/card height. Final loaded screenshot `phase5-home-final.png` visually verified the full discount text. Home/Catalog empty states are now hidden while loading or displaying an error (UI-only computed state).
- Wide viewport + font 130% pass: Home, Account, Profile, Address (empty state/form), Orders (empty state), Catalog, Product Detail, populated Cart and Checkout captured and visually reviewed. Evidence: `phase5-wide-*.png` and corresponding XML files in the session visualization directory. These are Pixel 7 overrides to 1600x2560/240dpi, not physical tablet testing. Promotion page is not exposed by the current customer navigation; its runtime check remains for Phase 6.
- Final build/install command: `dotnet build CuaHangNoiThat.csproj -f net10.0-android36.0 -r android-x64 -t:Install --no-restore -v:minimal`; passed with 0 warnings and 0 errors. `git diff --check` found no whitespace errors (only line-ending normalization notices).
- Keyboard verification: Tab traversed Orders -> Cart -> Profile action; Profile had a visible terracotta focus outline, and Enter opened the actual Profile page. Screenshot `phase5-keyboard-focus.png` and live XML confirmed this.
- Final native-tree verification: Home categories and product cards expose `android.widget.Button`, clickable and focusable, with actual category/product names. Existing bottom tabs remain focusable.
- TalkBack service was temporarily enabled and confirmed bound with spoken/haptic/audible capabilities. Android displayed its first-run notification permission dialog; this is NOT a passed test of app speech/focus narration. No audio was captured/evaluated. Full spoken TalkBack audit is explicitly deferred to Phase 6/manual review; keyboard and native semantic checks above are the verified evidence for this phase.
- Restored after checks: 1080x2400, 420dpi, font_scale 1.0, enabled_accessibility_services absent/null, accessibility_enabled 0. Dismissed the system permission dialog and brought the app back to foreground. No real order/profile/address writes were performed.
- Phase 5 implementation is closed. Overall redesign/final regression is not complete until Phase 6 outcomes and limitations are reviewed.

- Phase 1/2: source and resource inspection only.
- Phase 3 Android x64 build: succeeded with 0 warnings and 0 errors using `dotnet build CuaHangNoiThat.csproj -f net10.0-android36.0 -r android-x64 --no-restore -v:minimal`.
- Catalog Android x64 build: succeeded with 0 warnings and 0 errors using the same command for `CuaHangNoiThat.csproj`.
- Product Detail Android x64 build: succeeded with 0 warnings and 0 errors after adding the computed error-state binding property.
- Cart and Checkout Android x64 build: succeeded with 0 warnings and 0 errors.
- Cart and Checkout emulator/functional regression: pending.
- Account, Profile and Address Android x64 build: succeeded with 0 warnings and 0 errors.
- Account, Profile and Address emulator/functional regression: pending.
- Orders and Promotions Android x64 build: succeeded with 0 warnings and 0 errors.
- Orders and Promotions emulator/functional regression: pending.
- Phase 5 XAML/resource build: succeeded with 0 warnings and 0 errors.
- Phase 5 diff whitespace check: clean; explicit sub-44dp dimensions remaining are decorative elements, not action targets.
- Build/install after Status Bar and landscape fixes: succeeded, 0 warnings/errors.
- Pixel 7 API 36 portrait screenshots: captured for Home, Catalog, Cart, Account, Profile, Address, Orders, Checkout and Product Detail. Landscape was reviewed for Cart and Catalog. Status Bar contrast verified again on a pushed page.
- Cart runtime smoke: adding from Catalog and Detail plus increment updated quantity/totals; remove and empty state appeared; checkout navigation opened and showed contact/address/summary data. Decrement and order submission were not tested.
- Account/orders/address runtime smoke: signed-in state, profile/address/orders navigation, address list, default label and order status rendering confirmed. Logout and signed-out state not checked in this run.
- Recent logcat search: no `FATAL EXCEPTION` or `Unhandled Exception` match.
- Remaining: TalkBack/manual speech review, other screens at tablet dimensions, authentication/logout, voucher copy and order submission/regression. Voucher dropdown selection/application/removal is verified in the follow-up section.
- Catalog runtime filter/search verified using Android UI hierarchy: selecting Tối giản returned 2 products (Tủ quần áo óc chó 3 cánh and Gỗ thông); searching zz_no_match_123 returned 0 products with the expected empty-state title and guidance. Test text cleared and Tất cả selected afterward.
- Android build without explicit RID exited failed during runtime identifier computation (`android-arm64` app host unavailable), without compiler warnings/errors; explicit `android-x64` build completed successfully.
- Emulator/UI and functional verification are partial; see the specific passed and untested cases above.
- Follow-up responsive verification: Pixel 7 API 36 overridden to 1600x2560 at 240dpi (1067dp width) displayed Catalog in 3 columns. First-row action bounds had matching Y coordinates for three cards, and screenshot tablet-catalog.png confirmed images/text/actions clear of bottom tabs. This is a tablet-size viewport simulation, not a separate tablet hardware/AVD test.
- Phone text-size verification: 1080x2400 at 420dpi with font_scale=1.3; Catalog screenshot catalog-font130.png showed wrapped text, readable prices and available action buttons without first-row overlap. Other screens at this text size remain untested.
- TalkBack package is installed; package discovery alone does not verify spoken labels or focus order. Manual speech/focus testing remains pending.
- Restored emulator after responsive checks: physical size 1080x2400, density 420, font scale 1.0, user_rotation 0, accelerometer_rotation 0 (the settings read before this pass).

## Voucher selection enhancement (requested after redesign)

- Implemented selectable eligible vouchers in Checkout instead of manual code entry, with applied-code/savings display, remove and reload actions.
- Reused existing lowercase Supabase queries unchanged: positive stock, active linked campaign and current UTC date within start/end dates. Excluded nonpositive percentage discounts and empty subtotal. Schema has no voucher-specific minimum order amount; no threshold was invented or borrowed from invoice promotions.
- Selection revalidates with existing ApplyVoucherAsync against merchandise subtotal. Reload/re-entry also uses that same subtotal. Before order creation, a changed/unavailable voucher updates the total and asks for review rather than silently submitting a changed amount.
- Build/install Android x64 passed: 0 warnings, 0 errors. Pixel 7 API 36 emulator loaded 2 real 12% vouchers valid through 31/10/2026. Selected sadsad: 5,000,000 đ subtotal -> 600,000 đ discount -> 4,400,000 đ total. Switched to aaa1 with the same correct amount; removed voucher -> 0 đ discount and 5,000,000 đ total. Screenshot: voucher-selected.png in the session visualization directory.
- No order submitted and no voucher stock was modified during verification. Expiry/stock changing between selection and final submission was not simulated.
- Previously observed cold-start Checkout session/contact initialization issue was resolved in the follow-up customer-default request below.

## Checkout customer defaults and voucher dropdown (follow-up request)

- `LoadOrderAsync` now restores the existing saved session when needed and fills name, email and phone from the actual current customer when a new Checkout page opens. It clears stale contact values during initialization, leaves missing profile data blank, and clears stale address selection if the customer has no addresses.
- Checkout page tracks its first successful load so returning from address management does not overwrite contact edits. A new Checkout navigation initializes the current customer defaults again.
- Replaced inline voucher cards with a native Picker dropdown displaying code and discount percentage. Selection invokes the existing validated voucher command; the applied summary shows savings and expiry. Removing a voucher resets the dropdown; refresh/re-entry synchronizes the selection with the validated code.
- Changed source: CheckoutController.cs, CheckoutPage.xaml, CheckoutPage.xaml.cs and VoucherDto.cs (JSON-ignored display label only). Supabase queries and calculation rules were retained.
- Final Android Debug build/install on x64: passed, 0 warnings/errors.
- Pixel 7 API 36 final-build verification: cold start -> Catalog -> Cart -> Checkout without visiting Account populated all three contact fields from the saved customer. Dropdown showed sadsad and aaa1 (12% each). Selecting sadsad on 5,000,000 đ yielded 600,000 đ savings and 4,400,000 đ total; removing reset the dropdown to its placeholder. No order submitted.
- Evidence: checkout-autofill.png, voucher-dropdown.png and voucher-dropdown-applied.png in the session visualization directory. Contact-edit preservation on returning from address management is implemented but was not exercised in this test pass.

## 10. Known Issues / Audit Caveats

### Catalog advanced filters (requested enhancement)

- Added scrollable filter modal with draft minimum/maximum VND price, in-stock-only, discounted-only and default/price ascending/price descending/Vietnamese name sorting. Apply validates nonnegative integer prices (digits or dot-separated thousands) and min <= max. Close discards the draft; reset clears draft options. Catalog displays active count/summary and supports clearing only advanced filters while retaining search/category.
- Filters operate on the existing loaded FurnitureItem collection, using effective Price, IsInStock and HasDiscount; no Supabase query/schema changes or invented material/brand/rating filters. Existing search, category, detail and cart commands remain.
- Android x64 build/install passed: 0 warnings/errors. Pixel 7 emulator: min 9m/max 8m rejected; max 8m + stock + descending price returned 2 products, bed 7.5m then table 5m. Active count/summary matched. Sale + Tối giản returned 2 products; read-only DB cross-check confirmed wardrobe original12m and Gỗ thông original1b both have an active12% campaign. Clear retained category; closing a max-price1 draft preserved all4 products. Emulator returned to All/default advanced filters.
- Screenshots: advanced-filters.png, advanced-filters-result.png and advanced-filters-restored.png in the session visualization directory. Ascending/name sort and invalid nonnumeric price input were not separately exercised at runtime. Modal uses a ScrollView; dedicated tablet/landscape pass for this new modal remains part of final regression.
- Home explanation: unchanged `products.Take(3).OrderBy(x => x.Price)` in HomeController. Service currently returns active/nondeleted products ordered by id_sanpham. Home therefore takes the first3 IDs then sorts that subset by price; it is not a DB count limit or a data-driven top-featured ranking. View All opens the complete loaded Catalog.

- Project currently specifies Android target only. Wider-device or other-platform support needs confirmation during layout verification.
- Data/schema names in the pasted SQL are uppercase spelling, while PostgreSQL unquoted identifiers resolve lowercase and the service paths are lowercase. Do not change queries based only on pasted spelling.
- No standalone search, category detail, authentication, or order detail page was found in the current source inventory.
- The workspace has many existing modified files. They are not automatically attributed to this redesign; review diffs before editing related areas.

### Full Home redesign (2026-10-30/31, additional user request)

- Rebuilt Home as a single virtualized CollectionView with an editorial header, brand/address action, real search, horizontally scrolling typography-based category cards, two-column phone / three-column tablet product grid, and a footer opening the existing Catalog route. No promotional banner, collections, invented categories, or replacement product photos.
- Reused existing palette, typography/button resources, product models, category names, icons, Shell routes and Catalog filtering. Product cards show the actual effective price, conditional original price/discount, stock and detail action. The existing `Take(3).OrderBy(Price)` selection is unchanged; this request does not introduce a featured-product ranking or database change.
- Added reusable `Controls/ProductImageView.xaml(.cs)` with AspectFit, centered loading indicator, local furniture placeholder for invalid/missing/failed HTTP or undecodable images, cancellation and a bounded 12 MiB presentation cache (4 MiB per image, 12-second timeout). The actual first product photo is unrelated to furniture in current data; it is deliberately not silently replaced. The other two featured image URLs fail and use the placeholder.
- Home controller passes search/category selections into the existing singleton CatalogController; database/service queries remain unchanged. Android Enter handling consumes the action before queued navigation; this fixes an observed Android focus-search crash when changing tabs directly during the editor event. Hardware Enter was retested successfully: `1m8` opens Catalog with one matching bed.
- Native bottom navigation keeps the existing four Shell destinations and icons, with equal item widths, a rounded theme-opacity selected indicator and ripple. Home uses container safe area; actual screenshots confirmed the brand no longer overlaps the Status Bar.
- Files changed for this request: Views/HomePage.xaml(.cs), Controllers/HomeController.cs, new Controls/ProductImageView.xaml(.cs), Platforms/Android/MainActivity.cs, .gitignore (explicit inclusion of MainActivity previously hidden by android ignore), and this plan. Other dirty workspace files predate this request.
- Final Android Debug x64 build/install (`net10.0-android36.0`, `android-x64`): passed, zero warnings/errors. Pixel 7 API 36: checked normal phone layout, image fallback, opening table details, Tối giản category (two real products), Home search via Enter (one bed), Cart and Account tabs. Offline cold start displayed error and retry; restoring connectivity and tapping Retry reloaded the three real featured products.
- Visually inspected emulator with 360dp-wide/640dp-high configuration and font scale 1.3: two-column cards, two-line truncated long names and readable prices; vertical scrolling remains available. Inspected 1067dp tablet configuration: three columns and all three real products visible. Restored emulator size/density/font scale and network after checks. These are emulator configuration overrides, not separate physical devices.
- Screenshots in the session visualization directory: home-redesign-top.png, home-redesign-grid.png, home-tablet.png, home-small-font130-top.png (loading), home-small-font130-bottom.png / home-small-font130-cards.png, home-offline.png. The final tiny spinner refinement does not alter the previously inspected layout.
- Remaining limits: no empty Home database state was induced (no production data mutations); no runtime long-category fixture was inserted; category rendering supports two wrapped lines by XAML. Soft-keyboard search and TalkBack speech require a separate pass; hardware Enter/search is verified. Authenticated address editing/order submission/logout were not exercised in this Home-only request. Original broader Phase 6 items remain open.
- Final installed build also verified View All opens all four real Catalog products, and Home address action redirects an unauthenticated session to Account. Footer screenshot `home-redesign-footer.png` and native bounds confirm discounted price/original price/discount/stock/detail button and the Catalog footer are scrollable above Bottom Navigation. Final screenshots were visually inspected; source diff whitespace check passed. MainActivity is no longer ignored by Git.
