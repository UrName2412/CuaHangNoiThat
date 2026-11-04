using MoMo.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("momo", client => client.Timeout = TimeSpan.FromSeconds(35));
// Register ICheckoutStore only after mapping the existing auth, invoice and stock schema.
// No permissive in-memory implementation is provided for real payments.
var app = builder.Build();
app.UseHttpsRedirection();

MoMoGateway? Gateway(IServiceProvider services, IConfiguration configuration)
{
    var section = configuration.GetSection("MoMo");
    var partner = section["PartnerCode"];
    var access = section["AccessKey"];
    var secret = section["SecretKey"];
    if (string.IsNullOrWhiteSpace(partner) || string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret) ||
        !Uri.TryCreate(section["Endpoint"], UriKind.Absolute, out var endpoint) ||
        endpoint.Scheme != "https" || endpoint.Host is not ("test-payment.momo.vn" or "payment.momo.vn") ||
        endpoint.AbsolutePath != "/v2/gateway/api/create" ||
        !Uri.TryCreate(section["RedirectUrl"], UriKind.Absolute, out var redirect) || redirect.Scheme != "https" ||
        !Uri.TryCreate(section["IpnUrl"], UriKind.Absolute, out var ipn) || ipn.Scheme != "https") return null;
    return new MoMoGateway(services.GetRequiredService<IHttpClientFactory>().CreateClient("momo"),
        new MoMoOptions(partner, access, secret, endpoint, redirect, ipn));
}

app.MapGet("/health", () => Results.Ok(new { service = "MOKHA MoMo backend" }));
app.MapPost("/payments/momo", async (CheckoutRequest request, HttpRequest http, CancellationToken ct) =>
{
    var store = app.Services.GetService<ICheckoutStore>();
    var gateway = Gateway(app.Services, app.Configuration);
    if (store is null || gateway is null)
        return Results.Problem("MoMo integration is not configured.", statusCode: 503);
    if (!http.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.Ordinal))
        return Results.Unauthorized();
    if (request.CheckoutKey == Guid.Empty || request.Items is null || request.Items.Count is < 1 or > 50 ||
        request.Items.Any(i => i.Quantity <= 0 || i.ProductId <= 0) ||
        request.Items.Select(i => i.ProductId).Distinct().Count() != request.Items.Count)
        return Results.BadRequest();
    var intent = await store.PrepareAsync(http.Headers.Authorization.ToString()[7..], request, ct);
    var link = await gateway.CreateAsync(intent, ct);
    return Results.Ok(new { intent.OrderId, link.PayUrl, status = "pending" });
});
app.MapPost("/payments/momo/ipn", async (MoMoNotification notification, CancellationToken ct) =>
{
    var store = app.Services.GetService<ICheckoutStore>();
    var gateway = Gateway(app.Services, app.Configuration);
    if (store is null || gateway is null)
        return Results.Problem("MoMo integration is not configured.", statusCode: 503);
    var intent = await store.FindAsync(notification.OrderId, ct);
    if (intent is null || !gateway.Verify(notification, intent)) return Results.BadRequest();
    await store.ApplyVerifiedNotificationAsync(notification, ct);
    return Results.NoContent();
});
// Redirect parameters are presentation-only and are never used to change payment state.
app.MapGet("/payments/momo/return", () => Results.Text(
    "Bạn có thể quay lại MOKHA. Kết quả thanh toán sẽ được xác nhận từ máy chủ MoMo.", "text/plain; charset=utf-8"));
app.Run();
