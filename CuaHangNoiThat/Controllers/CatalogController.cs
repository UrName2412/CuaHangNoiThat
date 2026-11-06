using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public sealed record FilterChoice(string Label, bool IsSelected, ProductFilterOption? Option = null, string? SortOrder = null);
public sealed record CatalogCategoryChoice(string Label, bool IsSelected);

public partial class CatalogController : ObservableObject
{
    private enum FilterChoiceKind { ProductType, WoodType, SortOrder }
    private FilterChoiceKind filterChoiceKind;
    private readonly SupabaseService supabase;
    private readonly CartController cart;
    private List<FurnitureItem> allItems = [];
    private decimal? minimumPrice, maximumPrice;
    private bool inStockOnly, discountedOnly;
    private string sortOrder = "Mặc định";
    private static readonly ProductFilterOption AllProductTypes = new(null, "Tất cả loại sản phẩm");
    private static readonly ProductFilterOption AllWoodTypes = new(null, "Tất cả loại gỗ");
    private ProductFilterOption productTypeFilter = AllProductTypes;
    private ProductFilterOption woodTypeFilter = AllWoodTypes;
    public ObservableCollection<ProductFilterOption> ProductTypeOptions { get; } = [AllProductTypes];
    public ObservableCollection<ProductFilterOption> WoodTypeOptions { get; } = [AllWoodTypes];
    public bool HasProductTypeOptions => ProductTypeOptions.Count > 1;
    public bool HasNoProductTypeOptions => !HasProductTypeOptions;
    public IReadOnlyList<string> SortOptions { get; } = ["Mặc định", "Giá tăng dần", "Giá giảm dần", "Tên A–Z"];
    [ObservableProperty] private ProductFilterOption? draftProductType = AllProductTypes;
    [ObservableProperty] private ProductFilterOption? draftWoodType = AllWoodTypes;
    [ObservableProperty] private string minimumPriceInput = string.Empty;
    [ObservableProperty] private string maximumPriceInput = string.Empty;
    [ObservableProperty] private bool draftInStockOnly;
    [ObservableProperty] private bool draftDiscountedOnly;
    [ObservableProperty] private string draftSortOrder = "Mặc định";
    [ObservableProperty] private string filterMessage = string.Empty;
    public bool HasFilterMessage => !string.IsNullOrWhiteSpace(FilterMessage);
    partial void OnFilterMessageChanged(string value) => OnPropertyChanged(nameof(HasFilterMessage));
    [ObservableProperty] private bool isFilterChoiceOpen;
    [ObservableProperty] private string filterChoiceTitle = string.Empty;
    public ObservableCollection<FilterChoice> FilterChoices { get; } = new();
    public string DraftProductTypeName => DraftProductType?.Name ?? AllProductTypes.Name;
    public string DraftWoodTypeName => DraftWoodType?.Name ?? AllWoodTypes.Name;
    public string DraftSortOrderName => DraftSortOrder;

    partial void OnDraftProductTypeChanged(ProductFilterOption? value) => OnPropertyChanged(nameof(DraftProductTypeName));
    partial void OnDraftWoodTypeChanged(ProductFilterOption? value) => OnPropertyChanged(nameof(DraftWoodTypeName));
    partial void OnDraftSortOrderChanged(string value) => OnPropertyChanged(nameof(DraftSortOrderName));
    public int ActiveFilterCount => (productTypeFilter.Id.HasValue ? 1 : 0) + (woodTypeFilter.Id.HasValue ? 1 : 0)
        + (minimumPrice.HasValue || maximumPrice.HasValue ? 1 : 0) + (inStockOnly ? 1 : 0)
        + (discountedOnly ? 1 : 0) + (sortOrder != "Mặc định" ? 1 : 0);
    public bool HasAdvancedFilters => ActiveFilterCount > 0;
    public bool HasAnyFilters => HasAdvancedFilters || SelectedCategory != "Tất cả";
    public string FilterButtonText => HasAdvancedFilters ? $"Bộ lọc ({ActiveFilterCount})" : "Bộ lọc";
    public string FilterSummary => string.Join(" · ", new[]
    {
        productTypeFilter.Id.HasValue ? $"Loại: {productTypeFilter.Name}" : null,
        woodTypeFilter.Id.HasValue ? $"Gỗ: {woodTypeFilter.Name}" : null,
        minimumPrice.HasValue ? $"Từ {minimumPrice:N0} đ" : null,
        maximumPrice.HasValue ? $"Đến {maximumPrice:N0} đ" : null,
        inStockOnly ? "Còn hàng" : null, discountedOnly ? "Đang giảm giá" : null,
        sortOrder != "Mặc định" ? sortOrder : null
    }.Where(x => x is not null));

    public RangeObservableCollection<FurnitureItem> Items { get; } = new();
    public ObservableCollection<string> Categories { get; } = ["Tất cả"];
    public ObservableCollection<CatalogCategoryChoice> CategoryChoices { get; } = new();

    [ObservableProperty]
    private string selectedCategory = "Tất cả";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCartMessage))]
    private string cartMessage = string.Empty;

    public bool HasCartMessage => !string.IsNullOrWhiteSpace(CartMessage);

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsNotBusy => !IsBusy;
    public bool ShowEmptyState => !IsBusy && Items.Count == 0 && !HasError;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    public CatalogController(SupabaseService supabase, CartController cart)
    {
        this.supabase = supabase;
        this.cart = cart;
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            CartMessage = string.Empty;
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));
            allItems = (await supabase.GetProductsAsync()).ToList();
            Categories.Clear();
            Categories.Add("Tất cả");
            foreach (var category in allItems.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase))
                Categories.Add(category);
            RefreshCategoryChoices();
            LoadAttributeOptions();
            FilterItems();
        }
        catch (InvalidOperationException exception)
        {
            ClearFailedLoad();
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException exception)
        {
            ClearFailedLoad();
            ErrorMessage = $"Không thể tải sản phẩm: {exception.Message}";
        }
        catch (Exception exception)
        {
            ClearFailedLoad();
            ErrorMessage = $"Không thể tải sản phẩm: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    private void ClearFailedLoad()
    {
        // Old inventory must not remain actionable after a failed refresh.
        allItems.Clear();
        Items.ReplaceRange([]);
        Categories.Clear();
        Categories.Add("Tất cả");
        RefreshCategoryChoices();
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    partial void OnSelectedCategoryChanged(string value)
    {
        RefreshCategoryChoices();
        OnPropertyChanged(nameof(HasAnyFilters));
        FilterItems();
    }
    partial void OnSearchTextChanged(string value) => FilterItems();

    [RelayCommand]
    private void SelectCategory(string category) => SelectedCategory = category;

    private void RefreshCategoryChoices()
    {
        CategoryChoices.Clear();
        foreach (var category in Categories)
            CategoryChoices.Add(new CatalogCategoryChoice(category, category == SelectedCategory));
    }

    private void LoadAttributeOptions()
    {
        ReplaceAttributeOptions(ProductTypeOptions, AllProductTypes,
            allItems.Select(item => new ProductFilterOption(item.ProductTypeId, item.ProductType)), productTypeFilter);
        ReplaceAttributeOptions(WoodTypeOptions, AllWoodTypes,
            allItems.Select(item => new ProductFilterOption(item.WoodTypeId, item.WoodType)), woodTypeFilter);
        productTypeFilter = ProductTypeOptions.First(option => option.Id == productTypeFilter.Id);
        woodTypeFilter = WoodTypeOptions.First(option => option.Id == woodTypeFilter.Id);
        OnPropertyChanged(nameof(HasProductTypeOptions));
        OnPropertyChanged(nameof(HasNoProductTypeOptions));
        NotifyFilters();
    }

    private static void ReplaceAttributeOptions(ObservableCollection<ProductFilterOption> options,
        ProductFilterOption all, IEnumerable<ProductFilterOption> attributes, ProductFilterOption applied)
    {
        var available = attributes.Where(option => option.Id.HasValue && !string.IsNullOrWhiteSpace(option.Name))
            .DistinctBy(option => option.Id)
            .OrderBy(option => option.Name, StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), true))
            .ToList();
        // Keep an applied option removable even if its products disappear after a reload.
        if (applied.Id.HasValue && available.All(option => option.Id != applied.Id))
            available.Add(applied);
        options.Clear();
        options.Add(all);
        foreach (var option in available)
            options.Add(option);
    }

    [RelayCommand]
    private Task OpenFiltersAsync()
    {
        DraftProductType = ProductTypeOptions.FirstOrDefault(option => option.Id == productTypeFilter.Id) ?? AllProductTypes;
        DraftWoodType = WoodTypeOptions.FirstOrDefault(option => option.Id == woodTypeFilter.Id) ?? AllWoodTypes;
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
    private void OpenProductTypeChoices() => OpenFilterChoices(FilterChoiceKind.ProductType, "Loại sản phẩm");

    [RelayCommand]
    private void OpenWoodTypeChoices() => OpenFilterChoices(FilterChoiceKind.WoodType, "Loại gỗ");

    [RelayCommand]
    private void OpenSortChoices() => OpenFilterChoices(FilterChoiceKind.SortOrder, "Sắp xếp theo");

    private void OpenFilterChoices(FilterChoiceKind kind, string title)
    {
        filterChoiceKind = kind;
        FilterChoiceTitle = title;
        FilterChoices.Clear();
        switch (kind)
        {
            case FilterChoiceKind.ProductType:
                foreach (var option in ProductTypeOptions)
                    FilterChoices.Add(new FilterChoice(option.Name, option == DraftProductType, Option: option));
                break;
            case FilterChoiceKind.WoodType:
                foreach (var option in WoodTypeOptions)
                    FilterChoices.Add(new FilterChoice(option.Name, option == DraftWoodType, Option: option));
                break;
            case FilterChoiceKind.SortOrder:
                foreach (var option in SortOptions)
                    FilterChoices.Add(new FilterChoice(option, option == DraftSortOrder, SortOrder: option));
                break;
        }
        IsFilterChoiceOpen = true;
    }

    [RelayCommand]
    private void SelectFilterChoice(FilterChoice? choice)
    {
        if (!IsFilterChoiceOpen || choice is null || !FilterChoices.Contains(choice))
            return;

        switch (filterChoiceKind)
        {
            case FilterChoiceKind.ProductType:
                DraftProductType = choice.Option;
                break;
            case FilterChoiceKind.WoodType:
                DraftWoodType = choice.Option;
                break;
            case FilterChoiceKind.SortOrder:
                DraftSortOrder = choice.SortOrder ?? "Mặc định";
                break;
        }
        IsFilterChoiceOpen = false;
    }

    [RelayCommand]
    private void CloseFilterChoices() => IsFilterChoiceOpen = false;

    [RelayCommand]
    private void ResetFilterDraft()
    {
        IsFilterChoiceOpen = false;
        DraftProductType = AllProductTypes;
        DraftWoodType = AllWoodTypes;
        MinimumPriceInput = MaximumPriceInput = FilterMessage = string.Empty;
        DraftInStockOnly = DraftDiscountedOnly = false;
        DraftSortOrder = "Mặc định";
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        IsFilterChoiceOpen = false;
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
        productTypeFilter = ProductTypeOptions.FirstOrDefault(option => option == DraftProductType) ?? AllProductTypes;
        woodTypeFilter = WoodTypeOptions.FirstOrDefault(option => option == DraftWoodType) ?? AllWoodTypes;
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
        SelectedCategory = "Tất cả";
        productTypeFilter = AllProductTypes;
        woodTypeFilter = AllWoodTypes;
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
        OnPropertyChanged(nameof(HasAnyFilters));
        OnPropertyChanged(nameof(FilterButtonText));
        OnPropertyChanged(nameof(FilterSummary));
        OnPropertyChanged(nameof(ActiveFilterCount));
    }

    private void FilterItems()
    {
        var query = SearchText.Trim();
        var filtered = allItems.Where(item =>
                     (SelectedCategory == "Tất cả" || item.Category == SelectedCategory) &&
                     (!productTypeFilter.Id.HasValue || item.ProductTypeId == productTypeFilter.Id) &&
                     (!woodTypeFilter.Id.HasValue || item.WoodTypeId == woodTypeFilter.Id) &&
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
        Items.ReplaceRange(filtered);
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    private void AddToCart(FurnitureItem item)
    {
        if (item.StockAvailable <= 0)
            return;
        CartMessage = cart.AddItem(new CartItemModel
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
