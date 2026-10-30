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
    private readonly MockBankService mockBank;

#if DEBUG
    public bool IsMockBankAvailable => true;
#else
    public bool IsMockBankAvailable => false;
#endif
    public IReadOnlyList<string> MockOutcomes { get; } = ["Thành công", "Thất bại", "Hủy", "Hết hạn"];
    [ObservableProperty] private string selectedMockOutcome = "Thành công";
    [ObservableProperty] private string mockPaymentMessage = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string phone = string.Empty;

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private int selectedAddressId;

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
    private decimal subtotalAmount;

    [ObservableProperty]
    private decimal shippingAmount;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isBusy = false;

    public bool HasSelectedAddress => SelectedAddressId > 0;
    public bool IsNotBusy => !IsBusy;
    public ObservableCollection<AddressDto> Addresses { get; } = new();
    public ObservableCollection<VoucherDto> EligibleVouchers { get; } = new();
    [ObservableProperty] private VoucherDto? selectedVoucher;
    public bool HasAppliedVoucher => VoucherDiscount > 0 && !string.IsNullOrWhiteSpace(VoucherCode);
    public bool HasNoEligibleVouchers => !IsBusy && EligibleVouchers.Count == 0 && string.IsNullOrWhiteSpace(VoucherMessage);
    [ObservableProperty] private string voucherMessage = string.Empty;

    public CheckoutController(SupabaseService supabase, CartController cart, MockBankService mockBank)
    {
        this.supabase = supabase;
        this.cart = cart;
        this.mockBank = mockBank;
        LoadCartData();
        FullName = supabase.CurrentUser?.FullName ?? string.Empty;
        Email = supabase.CurrentUser?.Email ?? string.Empty;
        Phone = supabase.CurrentUser?.Phone ?? string.Empty;
        OnPropertyChanged(nameof(HasSelectedAddress));
    }

    private void LoadCartData()
    {
        cartTotalAmount = cart.TotalAmount;
        SubtotalAmount = cart.SubtotalAmount;
        ShippingAmount = cart.TotalAmount - cart.SubtotalAmount;
        TotalAmount = cartTotalAmount;
    }

    partial void OnTotalAmountChanged(decimal value) => MockPaymentMessage = string.Empty;

    [RelayCommand]
    private async Task SimulatePaymentAsync()
    {
        if (IsBusy || !IsMockBankAvailable) return;
        if (cart.Items.Count == 0 || TotalAmount <= 0)
        {
            MockPaymentMessage = "Cần có đơn hàng với tổng tiền lớn hơn 0 để chạy thử.";
            return;
        }
        try
        {
            IsBusy = true;
            MockPaymentMessage = "Đang gọi API ngân hàng giả lập...";
            var outcome = SelectedMockOutcome switch
            {
                "Thành công" => "succeeded", "Thất bại" => "failed",
                "Hủy" => "cancelled", "Hết hạn" => "expired",
                _ => throw new InvalidOperationException("Hãy chọn kết quả mô phỏng.")
            };
            var payment = await mockBank.SimulateAsync(TotalAmount, outcome);
            var status = payment.Status switch
            {
                "succeeded" => "Thành công", "failed" => "Thất bại",
                "cancelled" => "Đã hủy", "expired" => "Hết hạn",
                _ => throw new HttpRequestException("API giả lập trả về trạng thái chưa hoàn tất.")
            };
            MockPaymentMessage = $"GIẢ LẬP — {status}\nSố tiền: {payment.Amount:N0} đ\nMã giao dịch: {payment.Id}";
        }
        catch (Exception)
        {
            MockPaymentMessage = "Không hoàn tất được giao dịch thử. Kiểm tra API giả lập đang chạy và kết nối Emulator, sau đó thử lại.";
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> LoadOrderAsync(bool initializeContact = true)
    {
        if (IsBusy)
            return false;

        try
        {
            IsBusy = true;
            Message = string.Empty;
            if (initializeContact)
                FullName = Email = Phone = string.Empty;
            var customer = supabase.CurrentUser ?? await supabase.RestoreSessionAsync();
            if (customer is null)
            {
                Message = "Vui lòng đăng nhập để tiếp tục thanh toán.";
                await Shell.Current.GoToAsync("//AccountPage");
                return false;
            }
            if (initializeContact)
            {
                FullName = customer.FullName;
                Email = customer.Email;
                Phone = customer.Phone;
            }
            LoadCartData();

            if (cart.Items.Count == 0)
            {
                Message = "Giỏ hàng đang trống.";
                return false;
            }

            var addresses = await supabase.GetAddressesAsync();
            Addresses.Clear();
            foreach (var item in addresses)
                Addresses.Add(item);
            // If no addresses exist, auto-create from form fields if available
            if (addresses.Count == 0)
            {
                SelectedAddressId = 0;
                Message = "Chưa có địa chỉ giao hàng. Hãy thêm địa chỉ trước khi đặt hàng.";
            }
            else
            {
                SelectedAddressId = addresses.FirstOrDefault(a => a.MacDinh == 1)?.IdDiaChi ?? addresses[0].IdDiaChi;
            }

            OnPropertyChanged(nameof(HasSelectedAddress));

            await LoadEligibleVouchersAsync();
            // Calculate voucher discount
            if (!string.IsNullOrWhiteSpace(VoucherCode) && VoucherCode.Trim().Length > 0)
            {
                try
                {
                    VoucherDiscount = await supabase.ApplyVoucherAsync(VoucherCode.Trim(), SubtotalAmount);
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
            if (VoucherDiscount <= 0) VoucherCode = string.Empty;
            SelectedVoucher = EligibleVouchers.FirstOrDefault(v => v.IdVoucher == VoucherCode);
            OnPropertyChanged(nameof(HasAppliedVoucher));

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Message));
            return true;
        }
        catch (Exception ex)
        {
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
    private void SetAddress() => _ = Shell.Current.GoToAsync("AddressPage");

    [RelayCommand]
    private void SelectAddress(AddressDto address)
    {
        if (address is not null)
            SelectedAddressId = address.IdDiaChi;
    }

    [RelayCommand]
    private async Task SelectVoucherAsync(VoucherDto voucher)
    {
        if (IsBusy)
            return;

        if (voucher is null || !EligibleVouchers.Contains(voucher))
            return;

        try
        {
            IsBusy = true;
            var discount = await supabase.ApplyVoucherAsync(voucher.IdVoucher, cart.SubtotalAmount);
            VoucherCode = discount > 0 ? voucher.IdVoucher : string.Empty;
            VoucherDiscount = discount;
            TotalAmount = Math.Max(0, cartTotalAmount - VoucherDiscount);
            Message = VoucherDiscount > 0
                ? $"Đã giảm {VoucherDiscount:N0}đ."
                : "Mã giảm giá không hợp lệ hoặc đã hết lượt.";
        }
        catch (Exception exception)
        {
            VoucherDiscount = 0;
            VoucherCode = string.Empty;
            TotalAmount = cartTotalAmount;
            Message = $"Không thể áp dụng voucher: {exception.Message}";
        }
        finally
        {
            SelectedVoucher = EligibleVouchers.FirstOrDefault(v => v.IdVoucher == VoucherCode);
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
        OnPropertyChanged(nameof(HasNoEligibleVouchers));
        SelectedVoucher = EligibleVouchers.FirstOrDefault(v => v.IdVoucher == VoucherCode);
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
        VoucherCode = string.Empty;
        SelectedVoucher = null;
        VoucherDiscount = 0;
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
            if (SelectedAddressId == address.IdDiaChi)
                SelectedAddressId = Addresses.FirstOrDefault()?.IdDiaChi ?? 0;
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

        if (SelectedAddressId <= 0)
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
                    if (VoucherDiscount <= 0) VoucherCode = string.Empty;
                    SelectedVoucher = EligibleVouchers.FirstOrDefault(v => v.IdVoucher == VoucherCode);
                    TotalAmount = Math.Max(0, cartTotalAmount - VoucherDiscount);
                    OnPropertyChanged(nameof(HasAppliedVoucher));
                    Message = "Voucher đã thay đổi hoặc không còn khả dụng. Vui lòng kiểm tra lại tổng thanh toán.";
                    await LoadEligibleVouchersAsync();
                    return;
                }
            }

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
                TongTien = TotalAmount,
                IdDiaChiGiaoHang = SelectedAddressId,
                IdKhachHang = customerId,
                IdMaCtkM = null,
                IdVoucher = string.IsNullOrWhiteSpace(VoucherCode) ? null : VoucherCode.Trim()
            };

            await supabase.UpdateProfileAsync(FullName, Email, Phone);
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

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(HasNoEligibleVouchers));
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
