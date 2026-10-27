using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class ProductDetailController : ObservableObject
{
    private readonly SupabaseService supabase;
    private readonly CartController cart;

    [ObservableProperty]
    private FurnitureItem? product;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasProduct => Product is not null;
    public bool IsNotBusy => !IsBusy;

    public ProductDetailController(SupabaseService supabase, CartController cart)
    {
        this.supabase = supabase;
        this.cart = cart;
    }

    public async Task LoadAsync(int id)
    {
        if (IsBusy || id <= 0)
            return;

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
    private void AddToCart()
    {
        if (Product is null)
            return;

        cart.AddItem(new CartItemModel
        {
            ProductId = Product.IdSanPham,
            ProductName = Product.Name,
            Quantity = 1,
            UnitPrice = Product.Price,
            ImageName = Product.ImageName
        });
    }
}
