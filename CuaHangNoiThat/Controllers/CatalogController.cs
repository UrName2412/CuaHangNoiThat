using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
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
    private decimal? minimumPrice, maximumPrice;
    private bool inStockOnly, discountedOnly;
    private string sortOrder = "Mặc định";
    public IReadOnlyList<string> SortOptions { get; } = ["Mặc định", "Giá tăng dần", "Giá giảm dần", "Tên A–Z"];
    [ObservableProperty] private string minimumPriceInput = string.Empty;
    [ObservableProperty] private string maximumPriceInput = string.Empty;
    [ObservableProperty] private bool draftInStockOnly;
    [ObservableProperty] private bool draftDiscountedOnly;
    [ObservableProperty] private string draftSortOrder = "Mặc định";
    [ObservableProperty] private string filterMessage = string.Empty;
    public int ActiveFilterCount => (minimumPrice.HasValue || maximumPrice.HasValue ? 1 : 0) + (inStockOnly ? 1 : 0) + (discountedOnly ? 1 : 0) + (sortOrder != "Mặc định" ? 1 : 0);
    public bool HasAdvancedFilters => ActiveFilterCount > 0;
    public string FilterButtonText => HasAdvancedFilters ? $"Bộ lọc ({ActiveFilterCount})" : "Bộ lọc";
    public string FilterSummary => string.Join(" · ", new[]
    {
        minimumPrice.HasValue ? $"Từ {minimumPrice:N0} đ" : null,
        maximumPrice.HasValue ? $"Đến {maximumPrice:N0} đ" : null,
        inStockOnly ? "Còn hàng" : null, discountedOnly ? "Đang giảm giá" : null,
        sortOrder != "Mặc định" ? sortOrder : null
    }.Where(x => x is not null));

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
    public bool ShowEmptyState => !IsBusy && Items.Count == 0 && !HasError;
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(ShowEmptyState));

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

    [RelayCommand]
    private Task OpenFiltersAsync()
    {
        MinimumPriceInput = minimumPrice?.ToString("0", CultureInfo.InvariantCulture) ?? string.Empty;
        MaximumPriceInput = maximumPrice?.ToString("0", CultureInfo.InvariantCulture) ?? string.Empty;
        DraftInStockOnly = inStockOnly;
        DraftDiscountedOnly = discountedOnly;
        DraftSortOrder = sortOrder;
        FilterMessage = string.Empty;
        return Shell.Current.Navigation.PushModalAsync(new NavigationPage(new Views.CatalogFilterPage(this)));
    }

    [RelayCommand]
    private Task CloseFiltersAsync() => Shell.Current.Navigation.PopModalAsync();

    [RelayCommand]
    private void ResetFilterDraft()
    {
        MinimumPriceInput = MaximumPriceInput = FilterMessage = string.Empty;
        DraftInStockOnly = DraftDiscountedOnly = false;
        DraftSortOrder = "Mặc định";
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        if (!TryReadPrice(MinimumPriceInput, out var min) || !TryReadPrice(MaximumPriceInput, out var max))
        {
            FilterMessage = "Giá phải là số nguyên VND không âm, ví dụ 5000000 hoặc 5.000.000.";
            return;
        }
        if (min.HasValue && max.HasValue && min > max)
        {
            FilterMessage = "Giá tối thiểu không được lớn hơn giá tối đa.";
            return;
        }
        minimumPrice = min;
        maximumPrice = max;
        inStockOnly = DraftInStockOnly;
        discountedOnly = DraftDiscountedOnly;
        sortOrder = SortOptions.Contains(DraftSortOrder) ? DraftSortOrder : "Mặc định";
        NotifyFilters();
        FilterItems();
        await CloseFiltersAsync();
    }

    private static bool TryReadPrice(string input, out decimal? value)
    {
        value = null;
        var text = input.Trim();
        if (text.Length == 0) return true;
        if (!Regex.IsMatch(text, @"^(?:[0-9]+|[0-9]{1,3}(?:\.[0-9]{3})+)$")) return false;
        if (!decimal.TryParse(text.Replace(".", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var amount)) return false;
        value = amount;
        return true;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        minimumPrice = maximumPrice = null;
        inStockOnly = discountedOnly = false;
        sortOrder = "Mặc định";
        ResetFilterDraft();
        NotifyFilters();
        FilterItems();
    }

    private void NotifyFilters()
    {
        OnPropertyChanged(nameof(HasAdvancedFilters));
        OnPropertyChanged(nameof(FilterButtonText));
        OnPropertyChanged(nameof(FilterSummary));
        OnPropertyChanged(nameof(ActiveFilterCount));
    }

    private void FilterItems()
    {
        Items.Clear();
        var query = SearchText.Trim();
        var filtered = allItems.Where(item =>
                     (SelectedCategory == "Tất cả" || item.Category == SelectedCategory) &&
                     (!minimumPrice.HasValue || item.Price >= minimumPrice.Value) &&
                     (!maximumPrice.HasValue || item.Price <= maximumPrice.Value) &&
                     (!inStockOnly || item.IsInStock) &&
                     (!discountedOnly || item.HasDiscount) &&
                     (string.IsNullOrWhiteSpace(query) ||
                      item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                      item.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                      item.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        filtered = sortOrder switch
        {
            "Giá tăng dần" => filtered.OrderBy(x => x.Price).ThenBy(x => x.IdSanPham),
            "Giá giảm dần" => filtered.OrderByDescending(x => x.Price).ThenBy(x => x.IdSanPham),
            "Tên A–Z" => filtered.OrderBy(x => x.Name, StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), true)).ThenBy(x => x.IdSanPham),
            _ => filtered
        };
        foreach (var item in filtered)
            Items.Add(item);
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    private void AddToCart(FurnitureItem item)
    {
        if (item.StockAvailable <= 0)
            return;
        cart.AddItem(new CartItemModel
        {
            ProductId = item.IdSanPham,
            ProductName = item.Name,
            Quantity = 1,
            UnitPrice = item.Price,
            ImageName = item.ImageName,
            StockAvailable = item.StockAvailable
        });
    }

    [RelayCommand]
    private Task OpenDetails(FurnitureItem item) =>
        Shell.Current.GoToAsync($"ProductDetailPage?id={item.IdSanPham}");
}
