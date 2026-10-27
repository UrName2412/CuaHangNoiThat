using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class PromotionController : ObservableObject
{
    private readonly SupabaseService supabase;

    public ObservableCollection<PromotionDto> Promotions { get; } = new();
    public ObservableCollection<VoucherDto> Vouchers { get; } = new();

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public bool IsNotBusy => !IsBusy;
    public bool HasPromotions => Promotions.Count > 0;
    public bool HasVouchers => Vouchers.Count > 0;
    public bool HasNoPromotions => !HasPromotions;
    public bool HasNoVouchers => !HasVouchers;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public PromotionController(SupabaseService supabase) => this.supabase = supabase;

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Message = string.Empty;
            ErrorMessage = string.Empty;
            Promotions.Clear();
            foreach (var promotion in await supabase.GetPromotionsAsync())
                Promotions.Add(promotion);

            Vouchers.Clear();
            foreach (var voucher in await supabase.GetVouchersAsync())
                Vouchers.Add(voucher);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(HasPromotions));
            OnPropertyChanged(nameof(HasVouchers));
            OnPropertyChanged(nameof(HasNoPromotions));
            OnPropertyChanged(nameof(HasNoVouchers));
            OnPropertyChanged(nameof(HasError));
        }
    }

    [RelayCommand]
    private async Task CopyVoucherAsync(VoucherDto voucher)
    {
        if (voucher is null || string.IsNullOrWhiteSpace(voucher.IdVoucher))
            return;

        await Clipboard.Default.SetTextAsync(voucher.IdVoucher);
        Message = $"Đã sao chép mã {voucher.IdVoucher}.";
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));
}
