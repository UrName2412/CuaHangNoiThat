using CommunityToolkit.Mvvm.ComponentModel;

namespace CuaHangNoiThat.Models;

public partial class FurnitureItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string PriceFormatted => $"{Price:N0} đ";
    public string Image => "dotnet_bot.png";

    [ObservableProperty]
    private int quantity = 1;
}
