using CommunityToolkit.Mvvm.ComponentModel;

namespace CuaHangNoiThat.Models;

public partial class FurnitureItem : ObservableObject
{
    public int IdSanPham { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int? ProductTypeId { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public int? WoodTypeId { get; init; }
    public string WoodType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public bool HasDiscount => DiscountPercent > 0;
    public int StockAvailable { get; init; }
    public bool IsInStock => StockAvailable > 0;
    public string? ImageName { get; init; }
    public string PriceFormatted => $"{Price:N0} đ";
    public string OriginalPriceFormatted => $"{OriginalPrice:N0} đ";
    public string DiscountFormatted => $"-{DiscountPercent:N0}%";
    public string Image => "dotnet_bot.png";

    [ObservableProperty]
    private int quantity = 1;
}
