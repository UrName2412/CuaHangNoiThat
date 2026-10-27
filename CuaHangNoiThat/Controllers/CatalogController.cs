using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class CatalogController : ObservableObject
{
    private readonly SupabaseService supabase;
    private readonly CartController cart;
    private List<FurnitureItem> allItems = [];

    public ObservableCollection<FurnitureItem> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = ["Tất cả"];

    [ObservableProperty]
    private string selectedCategory = "Tất cả";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsNotBusy => !IsBusy;

    public CatalogController(SupabaseService supabase, CartController cart)
    {
        this.supabase = supabase;
        this.cart = cart;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));
            allItems = (await supabase.GetProductsAsync()).ToList();
            Categories.Clear();
            Categories.Add("Tất cả");
            foreach (var category in allItems.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase))
                Categories.Add(category);
            FilterItems();
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = $"Không thể tải sản phẩm: {exception.Message}";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Không thể tải sản phẩm: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    partial void OnSelectedCategoryChanged(string value) => FilterItems();
    partial void OnSearchTextChanged(string value) => FilterItems();

    [RelayCommand]
    private void SelectCategory(string category) => SelectedCategory = category;

    private void FilterItems()
    {
        Items.Clear();
        var query = SearchText.Trim();
        foreach (var item in allItems.Where(item =>
                     (SelectedCategory == "Tất cả" || item.Category == SelectedCategory) &&
                     (string.IsNullOrWhiteSpace(query) ||
                      item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                      item.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                      item.Description.Contains(query, StringComparison.OrdinalIgnoreCase))))
            Items.Add(item);
    }

    [RelayCommand]
    private void AddToCart(FurnitureItem item)
    {
        cart.AddItem(new CartItemModel
        {
            ProductId = item.IdSanPham,
            ProductName = item.Name,
            Quantity = 1,
            UnitPrice = item.Price,
            ImageName = item.ImageName
        });
    }

    [RelayCommand]
    private Task OpenDetails(FurnitureItem item) =>
        Shell.Current.GoToAsync($"ProductDetailPage?id={item.IdSanPham}");
}
