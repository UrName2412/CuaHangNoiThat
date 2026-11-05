using System.Collections.ObjectModel;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class CheckoutController : ObservableObject
{
    private readonly SupabaseService supabase;
    private readonly CartController cart;
    private readonly AccountController account;
    private readonly Dictionary<int, CartItemModel> sourceCartItems = new();
    private string? loadedCustomerId;
    private const string CashOnDelivery = "Thanh toán khi nhận hàng (COD)";

    public ObservableCollection<CartItemModel> CheckoutItems { get; } = new();
    public string SelectedAddressText => Addresses.FirstOrDefault(a => a.IdDiaChi == SelectedAddressId)?.DisplayAddress
        ?? "Chưa chọn địa chỉ giao hàng";
    public IReadOnlyList<string> PaymentMethods { get; } = [CashOnDelivery];
    [ObservableProperty] private string selectedPaymentMethod = CashOnDelivery;
    public bool IsOnlinePayment => SelectedPaymentMethod != CashOnDelivery;
    public string PaymentDescription => IsOnlinePayment
        ? "Phương thức này chưa được hỗ trợ. Vui lòng chọn thanh toán khi nhận hàng để tiếp tục."
        : "Thanh toán cho nhân viên giao hàng khi nhận được sản phẩm.";
    public string PlaceOrderText => IsOnlinePayment ? "Thanh toán online" : "Đặt hàng";
    [ObservableProperty] private bool isOrderCompleted;
    [ObservableProperty] private int completedOrderId;
    [ObservableProperty] private bool isOrderReady;
    public bool IsCheckoutVisible => !IsOrderCompleted;
    public bool CanPlaceOrder => IsOrderReady && !IsBusy && !IsOrderCompleted &&
        Addresses.Any(address => address.IdDiaChi == SelectedAddressId) && CheckoutItems.Count > 0 && !IsOnlinePayment;
    public bool CanRetryOrder => !IsOrderReady && !IsBusy && !IsOrderCompleted;

    partial void OnIsOrderReadyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPlaceOrder));
        OnPropertyChanged(nameof(CanRetryOrder));
    }

    partial void OnSelectedPaymentMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsOnlinePayment));
        OnPropertyChanged(nameof(PaymentDescription));
        OnPropertyChanged(nameof(PlaceOrderText));
        OnPropertyChanged(nameof(CanPlaceOrder));
    }

    partial void OnIsOrderCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCheckoutVisible));
        OnPropertyChanged(nameof(CanPlaceOrder));
        OnPropertyChanged(nameof(CanRetryOrder));
    }

    [RelayCommand]
    private Task ViewOrdersAsync() => Shell.Current.GoToAsync("../OrdersPage");

    [RelayCommand]
    private async Task ContinueShoppingAsync()
    {
        // Remove the completed checkout from the cart tab before switching tabs.
        await Shell.Current.GoToAsync("..");
        await Shell.Current.GoToAsync("//CatalogPage");
    }

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string phone = string.Empty;

    [ObservableProperty]
    private int selectedAddressId;

    [ObservableProperty]
    private string voucherCode = string.Empty;

    [ObservableProperty]
    private decimal voucherDiscount = 0;

    [ObservableProperty]
    private decimal totalAmount = 0;

    private decimal cartTotalAmount;

    [ObservableProperty]
    private decimal subtotalAmount;

    [ObservableProperty]
    private decimal shippingAmount;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isBusy = false;

    public bool HasSelectedAddress => SelectedAddressId > 0;
    public bool HasAddresses => Addresses.Count > 0;
    public bool HasNoAddresses => Addresses.Count == 0;
    public bool IsNotBusy => !IsBusy;
    public ObservableCollection<AddressDto> Addresses { get; } = new();
    [ObservableProperty] private bool isAddressPickerOpen;
    [ObservableProperty] private string addressMessage = string.Empty;
    public ObservableCollection<VoucherDto> EligibleVouchers { get; } = new();
    [ObservableProperty] private VoucherDto? selectedVoucher;
    [ObservableProperty] private bool isVoucherPickerOpen;
    public bool HasAppliedVoucher => VoucherDiscount > 0 && !string.IsNullOrWhiteSpace(VoucherCode);
    public bool HasEligibleVouchers => EligibleVouchers.Count > 0;
    public bool HasNoEligibleVouchers => !IsBusy && EligibleVouchers.Count == 0 && string.IsNullOrWhiteSpace(VoucherMessage);
    public string VoucherPickerText => HasAppliedVoucher
        ? $"{VoucherCode} · Đã giảm {VoucherDiscount:N0} đ"
        : EligibleVouchers.Count > 0
            ? $"Chọn voucher · {EligibleVouchers.Count} mã có sẵn"
            : "Chọn voucher";
    [ObservableProperty] private string voucherMessage = string.Empty;

    public CheckoutController(SupabaseService supabase, CartController cart, AccountController account)
    {
        this.supabase = supabase;
        this.cart = cart;
        this.account = account;
        LoadCartData();
        ApplyProfileContact(supabase.CurrentUser);
        OnPropertyChanged(nameof(HasSelectedAddress));
    }

    private void LoadCartData()
    {
        CheckoutItems.Clear();
        sourceCartItems.Clear();
        foreach (var item in cart.SelectedItems)
        {
            sourceCartItems[item.ProductId] = item;
            CheckoutItems.Add(new CartItemModel
            {
                ProductId = item.ProductId, ProductName = item.ProductName,
                Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                ImageName = item.ImageName, StockAvailable = item.StockAvailable
            });
        }
        cartTotalAmount = cart.TotalAmount;
        SubtotalAmount = cart.SubtotalAmount;
        ShippingAmount = cart.TotalAmount - cart.SubtotalAmount;
        TotalAmount = cartTotalAmount;
        OnPropertyChanged(nameof(CanPlaceOrder));
    }

    public async Task<bool> LoadOrderAsync()
    {
        if (IsBusy)
            return false;

        IsVoucherPickerOpen = false;
        IsAddressPickerOpen = false;

        // Shell can reveal the same page after navigating back from the order list.
        // Keep the receipt visible and never turn a completed order into a new draft.
        if (IsOrderCompleted && loadedCustomerId == supabase.CurrentUser?.Id)
            return true;

        var previousAddressId = loadedCustomerId == supabase.CurrentUser?.Id ? SelectedAddressId : 0;
        if (loadedCustomerId != supabase.CurrentUser?.Id)
        {
            IsOrderCompleted = false;
            CompletedOrderId = 0;
            ClearVoucherState();
        }

        try
        {
            IsBusy = true;
            IsOrderReady = false;
            Addresses.Clear();
            OnPropertyChanged(nameof(HasAddresses));
            OnPropertyChanged(nameof(HasNoAddresses));
            SelectedAddressId = 0;
            OnPropertyChanged(nameof(SelectedAddressText));
            ApplyProfileContact(null);
            CheckoutItems.Clear();
            sourceCartItems.Clear();
            TotalAmount = SubtotalAmount = ShippingAmount = cartTotalAmount = 0;
            Message = string.Empty;
            var customer = supabase.CurrentUser ?? await supabase.RestoreSessionAsync();
            if (customer is null)
            {
                Message = "Vui lòng đăng nhập để tiếp tục thanh toán.";
                account.RequestCheckoutAfterSignIn();
                await Shell.Current.GoToAsync("//AccountPage");
                return false;
            }

            loadedCustomerId = customer.Id;
            ApplyProfileContact(customer);
            LoadCartData();
            if (string.IsNullOrWhiteSpace(VoucherCode))
                VoucherDiscount = 0;

            if (CheckoutItems.Count == 0)
            {
                Message = "Giỏ hàng đang trống.";
                return false;
            }

            var addresses = await supabase.GetAddressesAsync();
            if (supabase.CurrentUser?.Id != loadedCustomerId)
                throw new InvalidOperationException("Tài khoản đã thay đổi. Vui lòng tải lại thông tin thanh toán.");
            foreach (var item in addresses)
                Addresses.Add(item);
            OnPropertyChanged(nameof(HasAddresses));
            OnPropertyChanged(nameof(HasNoAddresses));
            OnPropertyChanged(nameof(SelectedAddressText));
            if (addresses.Count == 0)
            {
                SelectedAddressId = 0;
                Message = "Chưa có địa chỉ giao hàng. Hãy thêm địa chỉ trước khi đặt hàng.";
            }
            else
            {
                SelectedAddressId = addresses.Any(item => item.IdDiaChi == previousAddressId)
                    ? previousAddressId
                    : addresses.FirstOrDefault(a => a.MacDinh == 1)?.IdDiaChi ?? addresses[0].IdDiaChi;
            }

            await LoadEligibleVouchersAsync();
            if (!string.IsNullOrWhiteSpace(VoucherCode))
            {
                try
                {
                    VoucherDiscount = await supabase.ApplyVoucherAsync(VoucherCode.Trim(), SubtotalAmount);
                    if (VoucherDiscount <= 0)
                    {
                        ClearVoucherState();
                        Message = "Mã giảm giá không hợp lệ hoặc hết hạn.";
                    }
                    else
                    {
                        Message = $"Mã giảm giá áp dụng: -{VoucherDiscount:N0}đ";
                    }
                }
                catch (Exception ex)
                {
                    ClearVoucherState();
                    Message = $"Lỗi voucher: {ex.Message}";
                }
            }

            if (supabase.CurrentUser?.Id != loadedCustomerId)
                throw new InvalidOperationException("Tài khoản đã thay đổi. Vui lòng tải lại thông tin thanh toán.");

            TotalAmount = Math.Max(0, cartTotalAmount - VoucherDiscount);
            SyncSelectedVoucher();
            OnPropertyChanged(nameof(HasAppliedVoucher));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Message));
            IsOrderReady = true;
            return true;
        }
        catch (Exception ex)
        {
            Addresses.Clear();
            OnPropertyChanged(nameof(HasAddresses));
            OnPropertyChanged(nameof(HasNoAddresses));
            SelectedAddressId = 0;
            OnPropertyChanged(nameof(SelectedAddressText));
            if (supabase.CurrentUser?.Id != loadedCustomerId)
                ApplyProfileContact(null);
            Message = ex.Message;
            OnPropertyChanged(nameof(Message));
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task RefreshOrderAsync() => LoadOrderAsync();

    [RelayCommand]
    private Task SetAddress()
    {
        IsAddressPickerOpen = false;
        return Shell.Current.GoToAsync("AddressPage");
    }

    [RelayCommand]
    private Task OpenProfileAsync() => Shell.Current.GoToAsync("ProfilePage");

    [RelayCommand]
    private void OpenAddressPicker()
    {
        if (IsBusy || IsOrderCompleted)
            return;

        AddressMessage = string.Empty;
        IsVoucherPickerOpen = false;
        IsAddressPickerOpen = true;
    }

    [RelayCommand]
    private void CloseAddressPicker()
    {
        if (!IsBusy)
            IsAddressPickerOpen = false;
    }

    [RelayCommand]
    private void SelectAddress(AddressDto address)
    {
        if (!IsBusy && address is not null && Addresses.Contains(address))
        {
            SelectedAddressId = address.IdDiaChi;
            IsAddressPickerOpen = false;
        }
    }

    [RelayCommand]
    private void OpenVoucherPicker()
    {
        if (!IsBusy && !IsOrderCompleted)
        {
            IsAddressPickerOpen = false;
            IsVoucherPickerOpen = true;
        }
    }

    [RelayCommand]
    private void CloseVoucherPicker()
    {
        if (!IsBusy)
            IsVoucherPickerOpen = false;
    }

    [RelayCommand]
    private async Task SelectVoucherAsync(VoucherDto voucher)
    {
        if (IsBusy || voucher is null)
            return;

        if (EligibleVouchers.All(item => item.IdVoucher != voucher.IdVoucher))
            return;

        if (voucher.IdVoucher == VoucherCode && VoucherDiscount > 0)
        {
            IsVoucherPickerOpen = false;
            return;
        }

        try
        {
            IsBusy = true;
            VoucherMessage = string.Empty;
            var discount = await supabase.ApplyVoucherAsync(voucher.IdVoucher, SubtotalAmount);
            VoucherCode = discount > 0 ? voucher.IdVoucher : string.Empty;
            VoucherDiscount = discount;
            TotalAmount = Math.Max(0, cartTotalAmount - VoucherDiscount);
            Message = VoucherDiscount > 0
                ? $"Đã giảm {VoucherDiscount:N0}đ."
                : "Mã giảm giá không hợp lệ hoặc đã hết lượt.";
            if (VoucherDiscount > 0)
                IsVoucherPickerOpen = false;
            else
                VoucherMessage = Message;
        }
        catch (Exception exception)
        {
            ClearVoucherState();
            TotalAmount = cartTotalAmount;
            Message = $"Không thể áp dụng voucher: {exception.Message}";
            VoucherMessage = Message;
        }
        finally
        {
            SyncSelectedVoucher();
            IsBusy = false;
            OnPropertyChanged(nameof(HasAppliedVoucher));
        }
    }

    private async Task LoadEligibleVouchersAsync()
    {
        EligibleVouchers.Clear();
        VoucherMessage = string.Empty;
        try
        {
            foreach (var voucher in await supabase.GetVouchersAsync())
                if (voucher.PhanTram > 0 && SubtotalAmount > 0)
                    EligibleVouchers.Add(voucher);
        }
        catch (Exception)
        {
            VoucherMessage = "Không thể tải voucher. Hãy thử tải lại.";
        }

        if (!string.IsNullOrWhiteSpace(VoucherCode) &&
            EligibleVouchers.All(item => item.IdVoucher != VoucherCode))
        {
            ClearVoucherState();
            TotalAmount = cartTotalAmount;
            Message = "Voucher đã chọn không còn khả dụng.";
        }

        SyncSelectedVoucher();
        OnPropertyChanged(nameof(HasEligibleVouchers));
        OnPropertyChanged(nameof(HasNoEligibleVouchers));
        OnPropertyChanged(nameof(HasAppliedVoucher));
        OnPropertyChanged(nameof(VoucherPickerText));
    }

    [RelayCommand]
    private async Task RefreshVouchersAsync()
    {
        if (IsBusy) return;
        try { IsBusy = true; await LoadEligibleVouchersAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void RemoveVoucher()
    {
        if (IsBusy) return;
        ClearVoucherState();
        TotalAmount = cartTotalAmount;
        Message = "Đã bỏ voucher.";
        OnPropertyChanged(nameof(HasAppliedVoucher));
    }

    [RelayCommand]
    private async Task DeleteAddressAsync(AddressDto address)
    {
        if (address is null || IsBusy)
            return;

        try
        {
            IsBusy = true;
            await supabase.DeleteAddressAsync(address.IdDiaChi);
            Addresses.Remove(address);
            OnPropertyChanged(nameof(HasAddresses));
            OnPropertyChanged(nameof(HasNoAddresses));
            if (SelectedAddressId == address.IdDiaChi)
                SelectedAddressId = Addresses.FirstOrDefault()?.IdDiaChi ?? 0;
            OnPropertyChanged(nameof(HasSelectedAddress));
        }
        catch (Exception exception)
        {
            Message = $"Không thể xóa địa chỉ: {exception.Message}";
            AddressMessage = Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmCheckoutAsync()
    {
        if (IsBusy || IsOrderCompleted)
            return;

        if (!IsOrderReady || loadedCustomerId != supabase.CurrentUser?.Id)
        {
            IsOrderReady = false;
            Message = "Thông tin thanh toán chưa được xác nhận. Vui lòng tải lại trước khi đặt hàng.";
            return;
        }

        if (IsOnlinePayment)
        {
            Message = PaymentDescription;
            return;
        }

        if (supabase.CurrentUser is null || !int.TryParse(supabase.CurrentUser.Id, out var customerId))
        {
            Message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.";
            return;
        }

        if (CheckoutItems.Count == 0)
        {
            Message = "Giỏ hàng đang trống.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Phone))
        {
            Message = "Hồ sơ còn thiếu họ tên, email hoặc số điện thoại. Hãy cập nhật tại Thông tin cá nhân.";
            return;
        }

        if (!IsValidEmail(Email))
        {
            Message = "Email không đúng định dạng.";
            return;
        }

        if (Phone.Any(character => !char.IsDigit(character)) || Phone.Trim().Length < 9)
        {
            Message = "Số điện thoại không hợp lệ.";
            return;
        }

        if (!Addresses.Any(a => a.IdDiaChi == SelectedAddressId))
        {
            Message = "Vui lòng chọn địa chỉ giao hàng.";
            return;
        }

        try
        {
            IsBusy = true;
            Message = "Đang xử lý đặt hàng...";

            if (HasAppliedVoucher)
            {
                var verifiedDiscount = await supabase.ApplyVoucherAsync(VoucherCode, SubtotalAmount);
                if (verifiedDiscount != VoucherDiscount || verifiedDiscount <= 0)
                {
                    VoucherDiscount = Math.Max(0, verifiedDiscount);
                    if (VoucherDiscount <= 0)
                        ClearVoucherState();
                    else
                        SyncSelectedVoucher();
                    TotalAmount = Math.Max(0, cartTotalAmount - VoucherDiscount);
                    OnPropertyChanged(nameof(HasAppliedVoucher));
                    Message = "Voucher đã thay đổi hoặc không còn khả dụng. Vui lòng kiểm tra lại tổng thanh toán.";
                    await LoadEligibleVouchersAsync();
                    return;
                }
            }

            var orderItems = CheckoutItems.Select(i => new OrderItemCreateDto
            {
                IdSanPham = i.ProductId,
                SoLuong = i.Quantity,
                DonGia = i.UnitPrice,
                ThanhTien = i.TotalPrice,
                IdMaCtkM = null
            }).ToList();

            var orderDto = new OrderCreateDto
            {
                TongTien = TotalAmount,
                IdDiaChiGiaoHang = SelectedAddressId,
                IdKhachHang = customerId,
                IdMaCtkM = null,
                IdVoucher = string.IsNullOrWhiteSpace(VoucherCode) ? null : VoucherCode.Trim()
            };

            CompletedOrderId = await supabase.CreateOrderAsync(orderDto, orderItems);

            foreach (var purchased in CheckoutItems)
                if (sourceCartItems.TryGetValue(purchased.ProductId, out var source))
                    cart.RemovePurchasedQuantity(source, purchased.Quantity);
            ClearVoucherState();
            SelectedAddressId = 0;
            IsOrderCompleted = true;
            Message = "Đặt hàng thành công. Bạn sẽ thanh toán khi nhận hàng.";
            OnPropertyChanged(nameof(Message));

        }
        catch (Exception ex)
        {
            Message = $"Lỗi đặt hàng: {ex.Message}";
            OnPropertyChanged(nameof(Message));
        }
        finally
        {
            IsBusy = false;
        }

    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(HasNoEligibleVouchers));
        OnPropertyChanged(nameof(CanPlaceOrder));
        OnPropertyChanged(nameof(CanRetryOrder));
    }

    partial void OnSelectedAddressIdChanged(int value)
    {
        OnPropertyChanged(nameof(HasSelectedAddress));
        OnPropertyChanged(nameof(SelectedAddressText));
        OnPropertyChanged(nameof(CanPlaceOrder));
    }

    private void ApplyProfileContact(AuthenticatedUser? customer)
    {
        FullName = customer?.FullName ?? string.Empty;
        Email = customer?.Email ?? string.Empty;
        Phone = customer?.Phone ?? string.Empty;
    }

    private void SyncSelectedVoucher()
    {
        SelectedVoucher = EligibleVouchers.FirstOrDefault(item => item.IdVoucher == VoucherCode);
        OnPropertyChanged(nameof(VoucherPickerText));
    }

    private void ClearVoucherState()
    {
        VoucherCode = string.Empty;
        SelectedVoucher = null;
        VoucherDiscount = 0;
        OnPropertyChanged(nameof(VoucherPickerText));
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            var address = new MailAddress(value.Trim());
            return address.Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
