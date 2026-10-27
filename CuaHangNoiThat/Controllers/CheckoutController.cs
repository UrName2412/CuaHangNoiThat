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

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string phone = string.Empty;

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private string selectedAddress = string.Empty;

    [ObservableProperty]
    private bool addressMode = false;

    [ObservableProperty]
    private bool applyingVoucher = false;

    [ObservableProperty]
    private string voucherCode = string.Empty;

    [ObservableProperty]
    private decimal voucherDiscount = 0;

    [ObservableProperty]
    private decimal totalAmount = 0;

    private decimal cartTotalAmount;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isBusy = false;

    public bool HasSelectedAddress => !string.IsNullOrWhiteSpace(SelectedAddress);
    public bool IsNotBusy => !IsBusy;
    public ObservableCollection<AddressDto> Addresses { get; } = new();

    public CheckoutController(SupabaseService supabase, CartController cart)
    {
        this.supabase = supabase;
        this.cart = cart;
        LoadCartData();
        OnPropertyChanged(nameof(HasSelectedAddress));
    }

    private void LoadCartData()
    {
        cartTotalAmount = cart.TotalAmount;
        TotalAmount = cartTotalAmount;
    }

    public async Task LoadOrderAsync()
    {
        if (IsBusy)
            return;

        if (supabase.CurrentUser is null)
        {
            Message = "Vui lòng đăng nhập để tiếp tục thanh toán.";
            await Shell.Current.GoToAsync("//AccountPage");
            return;
        }

        try
        {
            IsBusy = true;
            Message = string.Empty;
            LoadCartData();

            if (cart.Items.Count == 0)
            {
                Message = "Giỏ hàng đang trống.";
                return;
            }

            var addresses = await supabase.GetAddressesAsync();
            Addresses.Clear();
            foreach (var item in addresses)
                Addresses.Add(item);
            // If no addresses exist, auto-create from form fields if available
            if (addresses.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(Address))
                {
                    await supabase.AddAddressAsync(Address);
                    var newAddresses = await supabase.GetAddressesAsync();
                    if (newAddresses.Count > 0)
                    {
                        Addresses.Clear();
                        foreach (var item in newAddresses)
                            Addresses.Add(item);
                        SelectedAddress = newAddresses[0].DiChi;
                    }
                }
            }
            else
            {
                SelectedAddress = addresses[0].DiChi;
            }

            OnPropertyChanged(nameof(HasSelectedAddress));

            // Calculate voucher discount
            if (!string.IsNullOrWhiteSpace(VoucherCode) && VoucherCode.Trim().Length > 0)
            {
                try
                {
                    VoucherDiscount = await supabase.ApplyVoucherAsync(VoucherCode.Trim(), TotalAmount);
                    Message = VoucherDiscount > 0
                        ? $"Mã giảm giá áp dụng: -{VoucherDiscount:N0}đ"
                        : "Mã giảm giá không hợp lệ hoặc hết hạn.";
                }
                catch (Exception ex)
                {
                    Message = $"Lỗi voucher: {ex.Message}";
                    VoucherDiscount = 0;
                }
            }

            TotalAmount = Math.Max(0, cartTotalAmount - VoucherDiscount);

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Message));
        }
        catch (Exception ex)
        {
            Message = ex.Message;
            OnPropertyChanged(nameof(Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetAddress() => SelectedAddress = Address.Trim();

    [RelayCommand]
    private void SelectAddress(AddressDto address)
    {
        if (address is not null)
            SelectedAddress = address.DiChi;
    }

    [RelayCommand]
    private async Task ApplyVoucherAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(VoucherCode))
        {
            Message = "Vui lòng nhập mã giảm giá.";
            return;
        }

        try
        {
            IsBusy = true;
            VoucherDiscount = await supabase.ApplyVoucherAsync(VoucherCode.Trim(), cart.SubtotalAmount);
            TotalAmount = Math.Max(0, cartTotalAmount - VoucherDiscount);
            Message = VoucherDiscount > 0
                ? $"Đã giảm {VoucherDiscount:N0}đ."
                : "Mã giảm giá không hợp lệ hoặc đã hết lượt.";
        }
        catch (Exception exception)
        {
            VoucherDiscount = 0;
            TotalAmount = cartTotalAmount;
            Message = $"Không thể áp dụng voucher: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAddressAsync(AddressDto address)
    {
        if (address is null || IsBusy)
            return;

        try
        {
            IsBusy = true;
            await supabase.DeleteAddressAsync(address.DiChi);
            Addresses.Remove(address);
            if (SelectedAddress == address.DiChi)
                SelectedAddress = Addresses.FirstOrDefault()?.DiChi ?? string.Empty;
            OnPropertyChanged(nameof(HasSelectedAddress));
        }
        catch (Exception exception)
        {
            Message = $"Không thể xóa địa chỉ: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmCheckoutAsync()
    {
        if (IsBusy)
            return;

        if (supabase.CurrentUser is null || !int.TryParse(supabase.CurrentUser.Id, out var customerId))
        {
            Message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.";
            return;
        }

        if (cart.Items.Count == 0)
        {
            Message = "Giỏ hàng đang trống.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Phone))
        {
            Message = "Vui lòng nhập Họ tên, Email và Số điện thoại.";
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

        if (string.IsNullOrWhiteSpace(SelectedAddress))
        {
            Message = "Vui lòng chọn địa chỉ giao hàng.";
            return;
        }

        try
        {
            IsBusy = true;
            Message = "Đang xử lý đặt hàng...";

            var orderItems = cart.Items.Select(i => new OrderItemCreateDto
            {
                IdSanPham = i.ProductId,
                SoLuong = i.Quantity,
                DonGia = i.UnitPrice,
                ThanhTien = i.TotalPrice,
                IdMaCtkM = null
            }).ToList();

            var orderDto = new OrderCreateDto
            {
                DiaChiGiaoHang = SelectedAddress,
                TongTien = TotalAmount,
                IdKhachHang = customerId,
                IdMaCtkM = null,
                IdVoucher = string.IsNullOrWhiteSpace(VoucherCode) ? null : VoucherCode.Trim()
            };

            await supabase.CreateOrderAsync(orderDto, orderItems);

            // Clear cart after successful order
            cart.Clear();

            Message = "Đặt hàng thành công! Đội ngũ MOKHA sẽ liên hệ để xác nhận chi tiết.";
            OnPropertyChanged(nameof(Message));

            // Navigate back after delay
            await Shell.Current.GoToAsync("..");
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

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));

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