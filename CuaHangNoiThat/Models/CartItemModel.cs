using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public partial class CartItemModel : ObservableObject
{
    [ObservableProperty]
    [property: JsonIgnore]
    private bool isSelected = true;

    [JsonPropertyName("id_sanpham")]
    public int ProductId { get; set; }

    [ObservableProperty]
    [property: JsonPropertyName("tensanpham")]
    private string productName = string.Empty;

    [JsonPropertyName("soluong")]
    public int Quantity { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIncreaseQuantity))]
    [property: JsonIgnore]
    private int stockAvailable;

    [JsonIgnore]
    public bool CanIncreaseQuantity => Quantity < StockAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnitPriceFormatted), nameof(TotalPrice), nameof(TotalPriceFormatted))]
    [property: JsonPropertyName("dongia")]
    private decimal unitPrice;

    [ObservableProperty]
    [property: JsonPropertyName("hinhanh")]
    private string? imageName;

    public decimal TotalPrice => UnitPrice * Quantity;

    public string UnitPriceFormatted => UnitPrice.ToString("N0") + " đ";
    public string TotalPriceFormatted => TotalPrice.ToString("N0") + " đ";

    public void SetQuantity(int quantity)
    {
        Quantity = Math.Clamp(quantity, 1, Math.Max(1, StockAvailable));
        OnPropertyChanged(nameof(Quantity));
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(TotalPriceFormatted));
        OnPropertyChanged(nameof(CanIncreaseQuantity));
    }
}
