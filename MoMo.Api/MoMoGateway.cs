using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace MoMo.Api;

public sealed record MoMoOptions(string PartnerCode, string AccessKey, string SecretKey,
    Uri Endpoint, Uri RedirectUrl, Uri IpnUrl);

// Amount, IDs and description must come from the persisted, server-priced checkout.
public sealed record PaymentIntent(string OrderId, string RequestId, long Amount, string OrderInfo, string ExtraData);
public sealed record PaymentLink(int ResultCode, string Message, string? PayUrl, string? OrderId, string? RequestId);
public sealed record MoMoNotification(string PartnerCode, string OrderId, string RequestId,
    long Amount, string OrderInfo, string OrderType, long TransId, int ResultCode,
    string Message, string PayType, long ResponseTime, string ExtraData, string Signature);

public sealed class MoMoGateway(HttpClient client, MoMoOptions options)
{
    public async Task<PaymentLink> CreateAsync(PaymentIntent intent, CancellationToken cancellationToken = default)
    {
        if (intent.Amount <= 0 || string.IsNullOrEmpty(intent.OrderId) || string.IsNullOrEmpty(intent.RequestId))
            throw new ArgumentException("A persisted checkout with a positive VND amount is required.");
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["accessKey"] = options.AccessKey, ["amount"] = intent.Amount.ToString(CultureInfo.InvariantCulture),
            ["extraData"] = intent.ExtraData, ["ipnUrl"] = options.IpnUrl.AbsoluteUri,
            ["orderId"] = intent.OrderId, ["orderInfo"] = intent.OrderInfo,
            ["partnerCode"] = options.PartnerCode, ["redirectUrl"] = options.RedirectUrl.AbsoluteUri,
            ["requestId"] = intent.RequestId, ["requestType"] = "captureWallet"
        };
        using var response = await client.PostAsJsonAsync(options.Endpoint, new
        {
            partnerCode = options.PartnerCode, partnerName = "MOKHA Living",
            requestId = intent.RequestId, orderId = intent.OrderId, amount = intent.Amount,
            orderInfo = intent.OrderInfo, redirectUrl = options.RedirectUrl.AbsoluteUri,
            ipnUrl = options.IpnUrl.AbsoluteUri, lang = "vi", requestType = "captureWallet",
            extraData = intent.ExtraData, autoCapture = true, signature = Sign(fields)
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaymentLink>(cancellationToken)
            ?? throw new HttpRequestException("MoMo returned an empty response.");
        if (result.ResultCode != 0) throw new HttpRequestException($"MoMo rejected the request ({result.ResultCode}).");
        var expectedHost = options.Endpoint.Host;
        if (result.OrderId != intent.OrderId || result.RequestId != intent.RequestId ||
            !Uri.TryCreate(result.PayUrl, UriKind.Absolute, out var payUrl) ||
            payUrl.Scheme != "https" || payUrl.Host != expectedHost)
            throw new HttpRequestException("MoMo returned an invalid payment link.");
        // Creating a link does NOT confirm payment. Only a verified IPN can do that.
        return result;
    }

    public bool Verify(MoMoNotification notification, PaymentIntent expected)
    {
        if (notification.PartnerCode != options.PartnerCode || notification.OrderId != expected.OrderId ||
            notification.RequestId != expected.RequestId || notification.Amount != expected.Amount ||
            notification.OrderInfo != expected.OrderInfo || notification.ExtraData != expected.ExtraData)
            return false;
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["accessKey"] = options.AccessKey, ["amount"] = notification.Amount.ToString(CultureInfo.InvariantCulture),
            ["extraData"] = notification.ExtraData, ["message"] = notification.Message,
            ["orderId"] = notification.OrderId, ["orderInfo"] = notification.OrderInfo,
            ["orderType"] = notification.OrderType, ["partnerCode"] = notification.PartnerCode,
            ["payType"] = notification.PayType, ["requestId"] = notification.RequestId,
            ["responseTime"] = notification.ResponseTime.ToString(CultureInfo.InvariantCulture),
            ["resultCode"] = notification.ResultCode.ToString(CultureInfo.InvariantCulture),
            ["transId"] = notification.TransId.ToString(CultureInfo.InvariantCulture)
        };
        try
        {
            return notification.Signature is { Length: 64 } && CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(Sign(fields)), Convert.FromHexString(notification.Signature));
        }
        catch (FormatException) { return false; }
    }

    private string Sign(SortedDictionary<string, string> fields) => Convert.ToHexStringLower(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SecretKey), Encoding.UTF8.GetBytes(
            string.Join('&', fields.Select(pair => $"{pair.Key}={pair.Value}")))));
}
