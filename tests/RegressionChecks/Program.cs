using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using CuaHangNoiThat.Controllers;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var cart = new CartController();
cart.AddItem(new CartItemModel { ProductId = 1, StockAvailable = 10, Quantity = 7, UnitPrice = 100 });
var notifications = new List<string?>();
cart.Items[0].PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
cart.AddItem(new CartItemModel { ProductId = 1, StockAvailable = 2, Quantity = 1 });
Check(cart.Items[0].Quantity == 2 && cart.Items[0].StockAvailable == 2, "Fresh stock must clamp existing quantity.");
Check(!cart.Items[0].CanIncreaseQuantity && notifications.Contains("CanIncreaseQuantity"), "Quantity button must refresh.");
cart.AddItem(new CartItemModel { ProductId = 1, StockAvailable = 20, Quantity = 1 });
Check(cart.Items[0].Quantity == 3 && cart.Items[0].CanIncreaseQuantity, "Restocked items must allow increases.");
cart.AddItem(new CartItemModel { ProductId = 2, StockAvailable = 2, Quantity = 100 });
Check(cart.Items[1].Quantity == 2, "First insert must clamp quantity too.");
cart.AddItem(new CartItemModel { ProductId = 1, StockAvailable = 0, Quantity = 1 });
Check(cart.Items.All(i => i.ProductId != 1), "Known out-of-stock items must be removed.");

foreach (var (width, height) in new[] { (1, 1), (1024, 1024), (1025, 1), (8000, 4000), (4000, 8000), (16384, 1) })
{
    var sample = ImageDecodePolicy.GetSampleSize(width, height);
    Check((width + sample - 1) / sample <= 1024 && (height + sample - 1) / sample <= 1024,
        "Sample must bound both decoded dimensions.");
}
foreach (var (width, height) in new[] { (0, 1), (-1, 100), (6000, 6000), (int.MaxValue, int.MaxValue), (16385, 1) })
{
    try { ImageDecodePolicy.GetSampleSize(width, height); throw new Exception("Oversized image accepted."); }
    catch (InvalidDataException) { }
}

var handler = new RegistrationHandler();
var service = new SupabaseService(new HttpClient(handler));
var user = await service.RegisterAsync(" test@example.com ", "test-password", " Test User ");
Check(user.Id == "42" && handler.Calls == 1, "Registration must use one RPC and return its inserted ID.");
Check(service.CurrentUser?.Id == "42", "Successful RPC must establish the session.");

handler.Fail = true;
service = new SupabaseService(new HttpClient(handler));
try { await service.RegisterAsync("test@example.com", "test-password", "Test User"); throw new Exception("Failed RPC accepted."); }
catch (HttpRequestException) { }
Check(service.CurrentUser is null && handler.Calls == 2, "Failed RPC must not establish a session or fall back to separate inserts.");
Console.WriteLine("PASS: cart stock refresh/clamping, image dimension limits, registration RPC success/failure and PBKDF2 payload.");

cart.Clear();
cart.AddItem(new CartItemModel { ProductId = 1, StockAvailable = 10, Quantity = 1, UnitPrice = 100000 });
cart.AddItem(new CartItemModel { ProductId = 2, StockAvailable = 10, Quantity = 2, UnitPrice = 300000, IsSelected = false });
Check(cart.SelectedCount == 1 && cart.SubtotalAmount == 100000 && cart.TotalAmount == 350000,
    "Unselected items must not affect payment or shipping.");
cart.IsAllSelected = true;
Check(cart.SelectedCount == 2 && cart.TotalAmount == 700000, "Select-all must recalculate free shipping.");
cart.IsAllSelected = false;
Check(cart.TotalAmount == 0 && !cart.HasSelectedItems, "An empty selection must cost zero and disable checkout.");
cart.Items[0].IsSelected = true;

var checkoutHandler = new CheckoutHandler();
var checkoutService = new SupabaseService(new HttpClient(checkoutHandler));
await checkoutService.RegisterAsync("test@example.com", "test-password", "Test User");
var checkout = new CheckoutController(checkoutService, cart);
Check(await checkout.LoadOrderAsync(), "Checkout should load selected products and addresses.");
checkout.Phone = "0901234567";
Check(checkout.CheckoutItems.Count == 1 && checkout.SelectedAddressId == 7 && checkout.CanPlaceOrder,
    "Checkout must show selected products and default address.");
checkout.SelectedPaymentMethod = "Ví MoMo (đang thiết lập)";
await checkout.ConfirmCheckoutCommand.ExecuteAsync(null);
Check(!checkout.CanPlaceOrder && checkoutHandler.Invoices == 0, "Unconfigured MoMo must never create a COD invoice.");
checkout.SelectedPaymentMethod = "Thanh toán khi nhận hàng (COD)";
await checkout.ConfirmCheckoutCommand.ExecuteAsync(null);
Check(checkout.IsOrderCompleted && checkout.CompletedOrderId == 99 && checkoutHandler.Invoices == 1,
    "COD must show a successful order result after persistence.");
Check(checkoutHandler.Products.SequenceEqual(new[] { 1 }) && cart.Items.Count == 1 && cart.Items[0].ProductId == 2,
    "Only purchased items must be submitted and removed; unselected items remain.");
await checkout.ConfirmCheckoutCommand.ExecuteAsync(null);
Check(checkoutHandler.Invoices == 1, "A completed checkout must not submit another invoice.");
Console.WriteLine("PASS: selected totals, select-all, shipping, COD result, retained cart items and unconfigured MoMo guard.");

var profileFaultHandler = new ProfileAtomicHandler();
var profileFaultService = new SupabaseService(new HttpClient(profileFaultHandler));
await profileFaultService.RegisterAsync("profile@example.com", "test-password", "Profile User");
try
{
    await profileFaultService.UpdateProfileAsync("New Name", "new@example.com", "0900000000");
    throw new Exception("Faulted profile update unexpectedly succeeded.");
}
catch (HttpRequestException) { }
Check(profileFaultHandler.ProfileRpcCalls == 1 && !profileFaultHandler.LegacyPatchTouched,
    "Profile update must use a single atomic RPC and avoid two-step PATCH writes.");

var raceHandler = new DefaultAddressRaceHandler();
var raceService = new SupabaseService(new HttpClient(raceHandler));
await raceService.RegisterAsync("race@example.com", "test-password", "Race User");
var first = raceService.SetDefaultAddressAsync(1);
await Task.Delay(5);
var second = raceService.SetDefaultAddressAsync(2);
await Task.WhenAll(first, second);
Check(raceHandler.DefaultAddressId is 1 or 2,
    "Atomic default-address RPC must keep a valid default even under concurrent updates.");
Console.WriteLine("PASS: profile atomic-RPC guard and atomic default-address RPC concurrency check (offline).");

await MoMoChecks.RunAsync();
await CatalogChecks.RunAsync();

internal sealed class RegistrationHandler : HttpMessageHandler
{
    public int Calls { get; private set; }
    public bool Fail { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        if (request.Method != HttpMethod.Post || request.RequestUri?.AbsolutePath != "/rest/v1/rpc/register_customer")
            throw new Exception("Registration used a non-atomic endpoint.");
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        if (body.RootElement.GetProperty("p_email").GetString() != "test@example.com" ||
            body.RootElement.GetProperty("p_full_name").GetString() != "Test User")
            throw new Exception("Registration payload was not normalized.");
        var parts = body.RootElement.GetProperty("p_password_hash").GetString()!.Split('$');
        static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/').PadRight((value.Length + 3) / 4 * 4, '='));
        if (parts[0] != "pbkdf2-sha256" || !CryptographicOperations.FixedTimeEquals(
            Rfc2898DeriveBytes.Pbkdf2("test-password", Decode(parts[2]), int.Parse(parts[1]), HashAlgorithmName.SHA256, 32), Decode(parts[3])))
            throw new Exception("New password hash does not verify.");
        return new HttpResponseMessage(Fail ? HttpStatusCode.BadRequest : HttpStatusCode.OK)
        {
            Content = new StringContent(Fail ? "{\"message\":\"Account insert failed\"}" : "[{\"id_khachhang\":42}]")
        };
    }
}

internal sealed class CheckoutHandler : HttpMessageHandler
{
    public int Invoices { get; private set; }
    public List<int> Products { get; } = new();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri!.AbsolutePath;
        var response = path switch
        {
            "/rest/v1/rpc/register_customer" => "[{\"id_khachhang\":42}]",
            "/rest/v1/diachigiaohang" => "[{\"id_diachi\":7,\"id_khachhang\":42,\"duong\":\"Test address\",\"macdinh\":1}]",
            "/rest/v1/kmtheovoucher" => "[]",
            "/rest/v1/hoadon" => "[{\"id_hoadon\":99}]",
            "/rest/v1/chitiethoadon" => "[]",
            _ => throw new Exception("Unexpected endpoint (including inventory mutation): " + path)
        };
        if (path == "/rest/v1/hoadon") Invoices++;
        if (path == "/rest/v1/chitiethoadon")
        {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Products.Add(payload.RootElement.GetProperty("id_sanpham").GetInt32());
        }
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
    }
}

internal sealed class ProfileAtomicHandler : HttpMessageHandler
{
    public int ProfileRpcCalls { get; private set; }
    public bool LegacyPatchTouched { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path == "/rest/v1/rpc/register_customer")
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("[{\"id_khachhang\":42}]") });

        if (request.Method == HttpMethod.Post && path == "/rest/v1/rpc/update_customer_profile")
        {
            ProfileRpcCalls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            { Content = new StringContent("{\"message\":\"profile update failed\"}") });
        }

        if (request.Method == HttpMethod.Patch && (path == "/rest/v1/dangnhap" || path == "/rest/v1/khachhang"))
        {
            LegacyPatchTouched = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("[]") });
        }

        throw new Exception("Unexpected profile endpoint: " + path);
    }
}

internal sealed class DefaultAddressRaceHandler : HttpMessageHandler
{
    private readonly object sync = new();
    public int DefaultAddressId { get; private set; } = 7;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri!.AbsolutePath;

        if (path == "/rest/v1/rpc/register_customer")
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("[{\"id_khachhang\":42}]") };

        if (request.Method == HttpMethod.Post && path == "/rest/v1/rpc/set_default_address")
        {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var addressId = payload.RootElement.GetProperty("p_address_id").GetInt32();
            if (addressId == 1)
                await Task.Delay(80, ct);
            else if (addressId == 2)
                await Task.Delay(10, ct);

            lock (sync) DefaultAddressId = addressId;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }

        throw new Exception("Unexpected race endpoint: " + request.RequestUri);
    }
}
