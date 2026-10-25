using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Controllers;

public partial class HomeController : ObservableObject
{
    public ObservableCollection<FurnitureItem> FeaturedItems { get; } = new()
    {
        new() { Name = "Ghế Lounge Sồi Bắc Âu", Category = "Ghế thư giãn", Description = "Da tự nhiên · Gỗ sồi", Price = 8950000 },
        new() { Name = "Bàn ăn Oak Haven", Category = "Bàn ăn", Description = "Gỗ sồi nguyên khối", Price = 12900000 },
        new() { Name = "Đèn Nordica", Category = "Đèn decor", Description = "Ánh sáng ấm 2700K", Price = 1890000 }
    };

    [RelayCommand]
    private Task OpenCatalogAsync() => Shell.Current.GoToAsync("//CatalogPage");
}
