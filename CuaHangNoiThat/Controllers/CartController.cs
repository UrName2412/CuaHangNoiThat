using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Controllers;

public partial class CartController : ObservableObject
{
    public ObservableCollection<CartItemModel> Items { get; } = new();
    public IReadOnlyList<CartItemModel> SelectedItems => Items.Where(i => i.IsSelected).ToList();
    public int SelectedCount => SelectedItems.Count;
    public bool HasSelectedItems => SelectedCount > 0;
    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => !HasItems;
    public decimal SubtotalAmount => SelectedItems.Sum(i => i.TotalPrice);
    public decimal TotalAmount => HasSelectedItems ? SubtotalAmount + (SubtotalAmount >= 500000 ? 0 : 250000) : 0;
    public string Subtotal => SubtotalAmount.ToString("N0") + " đ";
    public string Total => TotalAmount.ToString("N0") + " đ";
    public string ShippingFee => !HasSelectedItems ? "0 đ" : SubtotalAmount >= 500000 ? "Miễn phí" : "250.000 đ";
    public string CheckoutLabel => $"Mua hàng ({SelectedCount})";
    private bool changingSelection;
    public bool IsAllSelected
    {
        get => HasItems && Items.All(i => i.IsSelected);
        set
        {
            if (changingSelection) return;
            changingSelection = true;
            try { foreach (var item in Items) item.IsSelected = value; }
            finally { changingSelection = false; }
            NotifyTotals();
        }
    }

    public CartController()
    {
        Items.CollectionChanged += (_, e) =>
        {
            if (e.OldItems is not null)
                foreach (CartItemModel item in e.OldItems) item.PropertyChanged -= ItemChanged;
            if (e.NewItems is not null)
                foreach (CartItemModel item in e.NewItems) item.PropertyChanged += ItemChanged;
            NotifyTotals();
        };
    }

    private void ItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!changingSelection && e.PropertyName is nameof(CartItemModel.IsSelected) or nameof(CartItemModel.Quantity))
            NotifyTotals();
    }

    private void NotifyTotals()
    {
        foreach (var name in new[] { nameof(Subtotal), nameof(Total), nameof(ShippingFee), nameof(HasItems),
            nameof(IsEmpty), nameof(SubtotalAmount), nameof(TotalAmount), nameof(SelectedItems), nameof(SelectedCount),
            nameof(HasSelectedItems), nameof(IsAllSelected), nameof(CheckoutLabel) })
            OnPropertyChanged(name);
    }

    public void AddItem(CartItemModel item)
    {
        if (item is null)
            return;

        var existing = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (item.StockAvailable <= 0)
        {
            // A fresh catalog result can invalidate an older cart snapshot.
            if (existing is not null)
                RemoveItem(existing.ProductId);
            return;
        }

        if (existing is not null)
        {
            existing.StockAvailable = item.StockAvailable;
            existing.SetQuantity((int)Math.Min(item.StockAvailable, (long)existing.Quantity + item.Quantity));
        }
        else
        {
            item.SetQuantity(item.Quantity);
            Items.Add(item);
        }
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
        foreach (var item in Items) item.PropertyChanged -= ItemChanged;
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
    private void IncreaseQuantity(CartItemModel item)
    {
        if (item.CanIncreaseQuantity)
            UpdateQuantity(item.ProductId, item.Quantity + 1);
    }

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
    private Task CheckoutAsync() => HasSelectedItems ? Shell.Current.GoToAsync("CheckoutPage") : Task.CompletedTask;

    [RelayCommand]
    private Task GoToCatalog() => Shell.Current.GoToAsync("//CatalogPage");
}
