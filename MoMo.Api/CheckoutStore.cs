namespace MoMo.Api;

// Implement against the real schema before enabling the payment endpoints.
// Never use a customer ID or price supplied by the app as authorization/pricing.
public interface ICheckoutStore
{
    // Authenticate the buyer, validate address ownership, stock and voucher, compute
    // the total on the server, then persist an idempotent draft BEFORE contacting MoMo.
    Task<PaymentIntent> PrepareAsync(string bearerToken, CheckoutRequest request, CancellationToken cancellationToken);
    Task<PaymentIntent?> FindAsync(string orderId, CancellationToken cancellationToken);
    // Persist IPN durably; on resultCode=0 create invoice/details and adjust inventory
    // atomically and once only. Replayed IPNs must not create another invoice/deduct again.
    // A failure must throw so that the endpoint never acknowledges an uncommitted result.
    Task ApplyVerifiedNotificationAsync(MoMoNotification notification, CancellationToken cancellationToken);
}

public sealed record CheckoutLine(int ProductId, int Quantity);
public sealed record CheckoutRequest(Guid CheckoutKey, int AddressId, string? VoucherCode, IReadOnlyList<CheckoutLine> Items);
