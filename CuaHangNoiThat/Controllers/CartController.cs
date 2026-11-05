using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Controllers;

public partial class CartController : ObservableObject
{
    private CartItemModel? pendingRemoval;
    [ObservableProperty] private bool isRemoveConfirmationOpen;
    public string PendingRemovalName => pendingRemoval?.ProductName ?? string.Empty;
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
        if (!changingSelection && e.PropertyName is nameof(CartItemModel.IsSelected) or nameof(CartItemModel.Quantity) or nameof(CartItemModel.UnitPrice))
            NotifyTotals();
    }

    private void NotifyTotals()
    {
        foreach (var name in new[] { nameof(Subtotal), nameof(Total), nameof(ShippingFee), nameof(HasItems),
            nameof(IsEmpty), nameof(SubtotalAmount), nameof(TotalAmount), nameof(SelectedItems), nameof(SelectedCount),
            nameof(HasSelectedItems), nameof(IsAllSelected), nameof(CheckoutLabel) })
            OnPropertyChanged(name);
    }

    public string AddItem(CartItemModel item)
    {
        if (item is null || item.ProductId <= 0 || item.Quantity <= 0 || item.UnitPrice < 0)
            return "Không thể thêm sản phẩm có thông tin không hợp lệ.";

        var existing = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (item.StockAvailable <= 0)
        {
            // A fresh catalog result can invalidate an older cart snapshot.
            if (existing is not null)
                RemoveItem(existing.ProductId);
            return "Sản phẩm hiện đã hết hàng.";
        }

        if (existing is not null)
        {
            var previousQuantity = existing.Quantity;
            existing.ProductName = item.ProductName;
            existing.UnitPrice = item.UnitPrice;
            existing.ImageName = item.ImageName;
            existing.StockAvailable = item.StockAvailable;
            existing.SetQuantity((int)Math.Min(item.StockAvailable, (long)existing.Quantity + item.Quantity));
            if (existing.Quantity <= previousQuantity)
                return $"Giỏ hàng đã được cập nhật. Bạn có thể mua tối đa {item.StockAvailable} sản phẩm này.";
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
        return $"Đã thêm {item.ProductName} vào giỏ hàng.";
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

    public void RemovePurchasedQuantity(CartItemModel source, int purchasedQuantity)
    {
        // A user can add more items or start a new cart while an order is in flight.
        // Consume only the purchased quantity from the original cart entry.
        if (purchasedQuantity <= 0 || !Items.Contains(source))
            return;
        if (source.Quantity <= purchasedQuantity)
            RemoveItem(source.ProductId);
        else
            UpdateQuantity(source.ProductId, source.Quantity - purchasedQuantity);
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
        {
            pendingRemoval = item;
            OnPropertyChanged(nameof(PendingRemovalName));
            IsRemoveConfirmationOpen = true;
        }
        else
            UpdateQuantity(item.ProductId, item.Quantity - 1);
    }

    [RelayCommand]
    private void CancelRemove()
    {
        IsRemoveConfirmationOpen = false;
        pendingRemoval = null;
        OnPropertyChanged(nameof(PendingRemovalName));
    }

    [RelayCommand]
    private void ConfirmRemove()
    {
        var item = pendingRemoval;
        CancelRemove();
        if (item is not null && Items.Contains(item) && item.Quantity == 1)
            RemoveItem(item.ProductId);
    }

    [RelayCommand]
    private void RemoveCartItem(CartItemModel item) => RemoveItem(item.ProductId);

    [RelayCommand]
    private Task CheckoutAsync() => HasSelectedItems ? Shell.Current.GoToAsync("CheckoutPage") : Task.CompletedTask;

    [RelayCommand]
    private Task GoToCatalog() => Shell.Current.GoToAsync("//CatalogPage");
}
