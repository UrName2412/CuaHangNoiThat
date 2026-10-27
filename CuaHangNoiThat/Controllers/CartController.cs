using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Controllers;

public partial class CartController : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CartItemModel> items = new();

    public string Subtotal => Items.Count > 0
        ? Items.Sum(i => i.TotalPrice).ToString("N0") + " đ"
        : "0 đ";

    public string ShippingFee => Items.Count > 0
        ? Items.Sum(i => i.TotalPrice) >= 500000 ? "Miễn phí" : "250.000 đ"
        : "0 đ";

    public string Total => Items.Count > 0
        ? (Items.Sum(i => i.TotalPrice) + (Items.Sum(i => i.TotalPrice) >= 500000 ? 0 : 250000)).ToString("N0") + " đ"
        : "0 đ";

    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => !HasItems;
    public decimal SubtotalAmount => Items.Sum(i => i.TotalPrice);
    public decimal TotalAmount => SubtotalAmount + (SubtotalAmount >= 500000 ? 0 : 250000);

    public CartController()
    {
        // Giỏ hàng tạm thời, sẽ bị xóa khi thoát app
    }

    public void AddItem(CartItemModel item)
    {
        if (item is null)
            return;

        var existing = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (existing is not null)
            existing.SetQuantity(existing.Quantity + item.Quantity);
        else
            Items.Add(item);
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ShippingFee));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(SubtotalAmount));
        OnPropertyChanged(nameof(TotalAmount));
    }

    public void RemoveItem(int productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null)
        {
            Items.Remove(item);
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(ShippingFee));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SubtotalAmount));
            OnPropertyChanged(nameof(TotalAmount));
        }
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null)
        {
            item.SetQuantity(quantity);
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(ShippingFee));
            OnPropertyChanged(nameof(SubtotalAmount));
            OnPropertyChanged(nameof(TotalAmount));
        }
    }

    public void Clear()
    {
        Items.Clear();
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ShippingFee));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(SubtotalAmount));
        OnPropertyChanged(nameof(TotalAmount));
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItemModel item) => UpdateQuantity(item.ProductId, item.Quantity + 1);

    [RelayCommand]
    private void DecreaseQuantity(CartItemModel item)
    {
        if (item.Quantity <= 1)
            RemoveItem(item.ProductId);
        else
            UpdateQuantity(item.ProductId, item.Quantity - 1);
    }

    [RelayCommand]
    private void RemoveCartItem(CartItemModel item) => RemoveItem(item.ProductId);

    [RelayCommand]
    private Task CheckoutAsync() => Shell.Current.GoToAsync("CheckoutPage");

    [RelayCommand]
    private Task GoToCatalog() => Shell.Current.GoToAsync("//CatalogPage");
}
