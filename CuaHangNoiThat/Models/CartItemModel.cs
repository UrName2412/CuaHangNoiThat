using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public partial class CartItemModel : ObservableObject
{
    [JsonPropertyName("id_sanpham")]
    public int ProductId { get; set; }

    [JsonPropertyName("tensanpham")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("soluong")]
    public int Quantity { get; set; }

    [JsonPropertyName("dongia")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("hinhanh")]
    public string? ImageName { get; set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    public string UnitPriceFormatted => UnitPrice.ToString("N0") + " đ";
    public string TotalPriceFormatted => TotalPrice.ToString("N0") + " đ";

    public void SetQuantity(int quantity)
    {
        Quantity = Math.Max(1, quantity);
        OnPropertyChanged(nameof(Quantity));
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(TotalPriceFormatted));
    }
}