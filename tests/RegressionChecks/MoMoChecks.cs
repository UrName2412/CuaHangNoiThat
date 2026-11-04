using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MoMo.Api;

internal static class MoMoChecks
{
    public static async Task RunAsync()
    {
        var intent = new PaymentIntent("order-1", "request-1", 100000, "Test order", "");
        var options = new MoMoOptions("partner", "access", "test-secret",
            new Uri("https://test-payment.momo.vn/v2/gateway/api/create"),
            new Uri("https://merchant.test/return"), new Uri("https://merchant.test/ipn"));
        var handler = new GatewayHandler();
        var gateway = new MoMoGateway(new HttpClient(handler), options);
        var link = await gateway.CreateAsync(intent);
        if (link.PayUrl != "https://test-payment.momo.vn/pay") throw new Exception("Payment URL missing.");
        var callback = new MoMoNotification("partner", "order-1", "request-1", 100000,
            "Test order", "momo_wallet", 123, 0, "Successful.", "qr", 1000, "", "");
        // Explicit provider wire-format fixtures, independent of the gateway's sorting logic.
        const string ipnRaw = "accessKey=access&amount=100000&extraData=&message=Successful.&orderId=order-1&orderInfo=Test order&orderType=momo_wallet&partnerCode=partner&payType=qr&requestId=request-1&responseTime=1000&resultCode=0&transId=123";
        callback = callback with { Signature = Sign(ipnRaw) };
        if (!gateway.Verify(callback, intent) || gateway.Verify(callback with { Amount = 1 }, intent) ||
            gateway.Verify(callback with { ResultCode = 9000 }, intent) ||
            gateway.Verify(callback with { RequestId = "other" }, intent) ||
            gateway.Verify(callback with { Signature = new string('z', 64) }, intent) ||
            gateway.Verify(callback with { Signature = null! }, intent))
            throw new Exception("IPN signature/identity/amount validation failed.");
        handler.UnsafeUrl = true;
        try { await gateway.CreateAsync(intent); throw new Exception("Unsafe redirect accepted."); }
        catch (HttpRequestException) { }
        Console.WriteLine("PASS: MoMo request signature, IPN verification, tamper rejection and payment URL validation (offline).");
    }

    private static string Sign(string text) => Convert.ToHexStringLower(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes("test-secret"), Encoding.UTF8.GetBytes(text)));

    private sealed class GatewayHandler : HttpMessageHandler
    {
        public bool UnsafeUrl { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            const string expectedRaw = "accessKey=access&amount=100000&extraData=&ipnUrl=https://merchant.test/ipn&orderId=order-1&orderInfo=Test order&partnerCode=partner&redirectUrl=https://merchant.test/return&requestId=request-1&requestType=captureWallet";
            if (json.RootElement.GetProperty("signature").GetString() != Sign(expectedRaw))
                throw new Exception("Create signature differs from provider wire format.");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    resultCode = 0, message = "Successful", orderId = "order-1", requestId = "request-1",
                    payUrl = UnsafeUrl ? "https://attacker.test/pay" : "https://test-payment.momo.vn/pay"
                }))
            };
        }
    }
}
