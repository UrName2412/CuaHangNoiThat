using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Controllers;

public partial class CatalogController : ObservableObject
{
    private readonly List<FurnitureItem> allItems =
    [
        new() { Name = "Sofa Oslo", Category = "Phòng khách", Description = "Vải linen · 3 chỗ ngồi", Price = 24500000 },
        new() { Name = "Ghế Lounge Sồi", Category = "Ghế thư giãn", Description = "Da tự nhiên · Gỗ sồi", Price = 8950000 },
        new() { Name = "Bàn Oak Haven", Category = "Bàn ăn", Description = "Gỗ sồi nguyên khối", Price = 12900000 },
        new() { Name = "Giường Fjord", Category = "Phòng ngủ", Description = "Óc chó Bắc Mỹ", Price = 31500000 },
        new() { Name = "Đèn Nordica", Category = "Đèn decor", Description = "Ánh sáng ấm 2700K", Price = 1890000 }
    ];

    public ObservableCollection<FurnitureItem> Items { get; } = new();
    public string[] Categories { get; } = ["Tất cả", "Phòng khách", "Ghế thư giãn", "Bàn ăn", "Phòng ngủ", "Đèn decor"];

    [ObservableProperty]
    private string selectedCategory = "Tất cả";

    public CatalogController() => FilterItems();

    partial void OnSelectedCategoryChanged(string value) => FilterItems();

    [RelayCommand]
    private void SelectCategory(string category) => SelectedCategory = category;

    private void FilterItems()
    {
        Items.Clear();
        foreach (var item in allItems.Where(item => SelectedCategory == "Tất cả" || item.Category == SelectedCategory))
            Items.Add(item);
    }

    [RelayCommand]
    private void AddToCart(FurnitureItem item) => item.Quantity++;
}
