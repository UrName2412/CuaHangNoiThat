using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class HomeController : ObservableObject
{
    private readonly SupabaseService supabase;
    private List<FurnitureItem> _featuredItems = new();

    public ObservableCollection<FurnitureItem> FeaturedItems { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;

    public HomeController(SupabaseService supabase)
    {
        this.supabase = supabase;
    }

    public async Task LoadAsync()
    {
        try
        {
            if (IsBusy)
                return;
            IsBusy = true;
            ErrorMessage = string.Empty;
            var products = await supabase.GetProductsAsync();
            _featuredItems = products.Take(3).ToList();
            _featuredItems = _featuredItems.OrderBy(x => x.Price).ToList();

            FeaturedItems.Clear();
            foreach (var item in _featuredItems)
                FeaturedItems.Add(item);
        }
        catch (Exception exception)
        {
            FeaturedItems.Clear();
            ErrorMessage = $"Không thể tải sản phẩm nổi bật: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    [RelayCommand]
    private Task OpenCatalog() => Shell.Current.GoToAsync("//CatalogPage");

    [RelayCommand]
    private Task OpenDetails(FurnitureItem item) =>
        Shell.Current.GoToAsync($"ProductDetailPage?id={item.IdSanPham}");
}
