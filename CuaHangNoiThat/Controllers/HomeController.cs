using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class HomeController : ObservableObject
{
    private readonly SupabaseService supabase;
    private readonly CatalogController catalog;
    public ObservableCollection<FurnitureItem> FeaturedItems { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool ShowEmptyState => !IsBusy && FeaturedItems.Count == 0 && string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasError => !IsBusy && !string.IsNullOrWhiteSpace(ErrorMessage);
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(HasError));
    }

    public HomeController(SupabaseService supabase, CatalogController catalog)
    {
        this.supabase = supabase;
        this.catalog = catalog;
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
            var featuredItems = products.Take(3).OrderBy(x => x.Price).ToList();

            FeaturedItems.Clear();
            foreach (var item in featuredItems)
                FeaturedItems.Add(item);

            Categories.Clear();
            foreach (var category in products.Select(item => item.Category)
                         .Where(category => !string.IsNullOrWhiteSpace(category))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(category => category, StringComparer.CurrentCulture))
                Categories.Add(category);
        }
        catch (Exception exception)
        {
            FeaturedItems.Clear();
            Categories.Clear();
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
    private Task SearchCatalog()
    {
        catalog.SelectedCategory = "Tất cả";
        catalog.SearchText = SearchText;
        return OpenCatalog();
    }

    [RelayCommand]
    private Task OpenCategory(string category)
    {
        catalog.SearchText = string.Empty;
        catalog.SelectedCategory = category;
        return OpenCatalog();
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private Task OpenAddresses() => supabase.CurrentUser is null
        ? Shell.Current.GoToAsync("//AccountPage")
        : Shell.Current.GoToAsync("AddressPage");

    [RelayCommand]
    private Task OpenDetails(FurnitureItem item) =>
        Shell.Current.GoToAsync($"ProductDetailPage?id={item.IdSanPham}");
}
