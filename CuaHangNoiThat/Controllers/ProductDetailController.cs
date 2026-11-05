using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class ProductDetailController : ObservableObject
{
    private readonly SupabaseService supabase;
    private readonly CartController cart;
    private int requestedProductId;

    [ObservableProperty]
    private FurnitureItem? product;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCartMessage))]
    private string cartMessage = string.Empty;

    public bool HasProduct => Product is not null;
    public bool HasCartMessage => !string.IsNullOrWhiteSpace(CartMessage);
    public bool CanAddToCart => !IsBusy && Product?.IsInStock == true;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsNotBusy => !IsBusy;

    public ProductDetailController(SupabaseService supabase, CartController cart)
    {
        this.supabase = supabase;
        this.cart = cart;
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnProductChanged(FurnitureItem? value)
    {
        OnPropertyChanged(nameof(HasProduct));
        OnPropertyChanged(nameof(CanAddToCart));
    }
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanAddToCart));
    }

    public async Task LoadAsync(int id)
    {
        if (IsBusy)
            return;

        requestedProductId = id;
        CartMessage = string.Empty;
        Product = null;
        if (id <= 0)
        {
            ErrorMessage = "Thông tin sản phẩm không hợp lệ. Vui lòng quay lại danh sách sản phẩm.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Product = null;
            Product = await supabase.GetFurnitureItemByIdAsync(id);
            if (Product is null)
                ErrorMessage = "Không tìm thấy sản phẩm này.";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Không thể tải sản phẩm: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasProduct));
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(requestedProductId);

    [RelayCommand]
    private void AddToCart()
    {
        if (!CanAddToCart || Product is null)
            return;

        CartMessage = cart.AddItem(new CartItemModel
        {
            ProductId = Product.IdSanPham,
            ProductName = Product.Name,
            Quantity = 1,
            UnitPrice = Product.Price,
            ImageName = Product.ImageName,
            StockAvailable = Product.StockAvailable
        });
    }
}
