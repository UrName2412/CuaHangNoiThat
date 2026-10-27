using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace CuaHangNoiThat.Models;

public sealed class OrderItemModel
{
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }

    public string UnitPriceFormatted => UnitPrice.ToString("N0") + " đ";
    public string TotalPriceFormatted => TotalPrice.ToString("N0") + " đ";
}

public partial class OrderModel : ObservableObject
{
    public string Code { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string TotalAmountFormatted => TotalAmount.ToString("N0") + " đ";
    public string Date { get; init; } = string.Empty;

    [ObservableProperty]
    private ObservableCollection<OrderItemModel> items = new();
}