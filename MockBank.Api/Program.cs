using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
// Sandbox is deliberately local-only. No bank credentials or customer PII are used.
builder.WebHost.UseUrls("http://127.0.0.1:5099");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
var app = builder.Build();
var payments = new ConcurrentDictionary<Guid, Payment>();
var keys = new Dictionary<string, Guid>(StringComparer.Ordinal);
var gate = new object();

app.MapGet("/health", () => Results.Ok(new { service = "MOKHA mock bank", simulation = true }));

app.MapPost("/sandbox/payments", (CreatePayment request, HttpRequest http) =>
{
    var key = http.Headers["Idempotency-Key"].ToString();
    if (!Guid.TryParse(key, out _) || request.Currency != "VND" ||
        request.Amount <= 0 || request.Amount > 1_000_000_000_000m || decimal.Truncate(request.Amount) != request.Amount)
        return Results.BadRequest(new { message = "Use a UUID Idempotency-Key and a positive whole VND amount up to 1 trillion." });

    lock (gate)
    {
        if (keys.TryGetValue(key, out var existingId) && payments.TryGetValue(existingId, out var existing))
            return existing.Amount == request.Amount && existing.Currency == request.Currency
                ? Results.Ok(existing)
                : Results.Conflict(new { message = "Idempotency key was already used for another amount." });

        // Bounded sandbox memory. Restarting this service clears all simulated transactions.
        if (payments.Count >= 1000)
            return Results.Problem("Sandbox transaction limit reached. Restart the local service.", statusCode: 503);
        var payment = new Payment(Guid.NewGuid(), request.Amount, request.Currency, "pending", DateTimeOffset.UtcNow.AddMinutes(10));
        payments[payment.Id] = payment;
        keys[key] = payment.Id;
        return Results.Created($"/sandbox/payments/{payment.Id}", payment);
    }
});

app.MapGet("/sandbox/payments/{id:guid}", (Guid id) =>
{
    lock (gate)
    {
        if (!payments.TryGetValue(id, out var payment)) return Results.NotFound();
        if (payment.Status == "pending" && payment.ExpiresAt <= DateTimeOffset.UtcNow)
            payments[id] = payment = payment with { Status = "expired" };
        return Results.Ok(payment);
    }
});

app.MapPost("/sandbox/payments/{id:guid}/simulate", (Guid id, SimulatePayment request) =>
{
    if (request.Outcome is not ("succeeded" or "failed" or "cancelled" or "expired"))
        return Results.BadRequest(new { message = "Unsupported simulation outcome." });
    lock (gate)
    {
        if (!payments.TryGetValue(id, out var payment)) return Results.NotFound();
        if (payment.Status == "pending" && payment.ExpiresAt <= DateTimeOffset.UtcNow)
            payments[id] = payment = payment with { Status = "expired" };
        if (payment.Status != "pending")
            return payment.Status == request.Outcome
                ? Results.Ok(payment)
                : Results.Conflict(new { message = "A completed simulation cannot change status." });
        payments[id] = payment = payment with { Status = request.Outcome };
        return Results.Ok(payment);
    }
});

app.Run();

record CreatePayment(decimal Amount, string Currency);
record SimulatePayment(string Outcome);
record Payment(Guid Id, decimal Amount, string Currency, string Status, DateTimeOffset ExpiresAt)
{
    public bool Simulation => true;
}
