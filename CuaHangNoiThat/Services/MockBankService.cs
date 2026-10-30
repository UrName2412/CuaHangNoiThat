using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Services;

/// <summary>Local sandbox only. A successful result never represents a real bank transfer.</summary>
public sealed class MockBankService
{
    private readonly HttpClient client = new()
    {
        BaseAddress = new Uri("http://localhost:5099/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<MockPayment> SimulateAsync(decimal amount, string outcome)
    {
#if !DEBUG
        return await Task.FromException<MockPayment>(new InvalidOperationException("Thanh toán giả lập chỉ có trong bản Debug."));
#else
        using var create = new HttpRequestMessage(HttpMethod.Post, "sandbox/payments")
        {
            Content = JsonContent.Create(new { amount, currency = "VND" })
        };
        create.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var created = await client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        var payment = await created.Content.ReadFromJsonAsync<MockPayment>()
            ?? throw new HttpRequestException("API giả lập không trả về giao dịch.");
        using var simulated = await client.PostAsJsonAsync($"sandbox/payments/{payment.Id}/simulate", new { outcome });
        simulated.EnsureSuccessStatusCode();
        var verified = await client.GetFromJsonAsync<MockPayment>($"sandbox/payments/{payment.Id}")
            ?? throw new HttpRequestException("Không xác minh được giao dịch giả lập.");
        if (!verified.Simulation || verified.Id != payment.Id || verified.Amount != amount || verified.Currency != "VND")
            throw new HttpRequestException("Phản hồi API giả lập không khớp giao dịch.");
        return verified;
#endif
    }
}

public sealed record MockPayment(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("simulation")] bool Simulation);
