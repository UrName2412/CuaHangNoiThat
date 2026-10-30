using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class AddressController : ObservableObject
{
    private readonly SupabaseService supabase;

    [ObservableProperty]
    private ObservableCollection<AddressDto> addresses = new();

    [ObservableProperty]
    private string provinceCode = string.Empty;

    [ObservableProperty]
    private string wardCode = string.Empty;

    [ObservableProperty]
    private string street = string.Empty;

    [ObservableProperty]
    private bool makeDefault;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isBusy = false;

    public bool HasAddresses => Addresses.Count > 0;
    public bool HasNoAddresses => !HasAddresses;
    public bool IsNotBusy => !IsBusy;

    public AddressController(SupabaseService supabase)
    {
        this.supabase = supabase;
    }

    public async Task LoadAddressesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Message = string.Empty;
            await RefreshAddressesAsync();
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    private async Task RefreshAddressesAsync()
    {
        var addresses = await supabase.GetAddressesAsync();
        Addresses = new ObservableCollection<AddressDto>(addresses);
        OnPropertyChanged(nameof(HasAddresses));
        OnPropertyChanged(nameof(HasNoAddresses));
    }

    [RelayCommand]
    private async Task AddAddressAsync()
    {
        if (IsBusy)
            return;

        if (!int.TryParse(ProvinceCode, out var province) || province <= 0 ||
            !int.TryParse(WardCode, out var ward) || ward <= 0 || string.IsNullOrWhiteSpace(Street))
        {
            Message = "Vui lòng nhập mã tỉnh/thành, mã phường/xã và số nhà, tên đường.";
            return;
        }

        try
        {
            IsBusy = true;
            Message = string.Empty;
            await supabase.AddAddressAsync(province, ward, Street.Trim(), MakeDefault || !HasAddresses);
            ProvinceCode = string.Empty;
            WardCode = string.Empty;
            Street = string.Empty;
            await RefreshAddressesAsync();
            Message = "Địa chỉ đã được thêm.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
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
            Message = string.Empty;
            await supabase.DeleteAddressAsync(address.IdDiaChi);
            await RefreshAddressesAsync();
            Message = "Địa chỉ đã bị xóa.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    [RelayCommand]
    private async Task SetDefaultAsync(AddressDto address)
    {
        if (address is null || address.IsDefault || IsBusy) return;
        try
        {
            IsBusy = true;
            await supabase.SetDefaultAddressAsync(address.IdDiaChi);
            await RefreshAddressesAsync();
            Message = "Đã cập nhật địa chỉ mặc định.";
        }
        catch (Exception exception) { Message = exception.Message; }
        finally { IsBusy = false; OnPropertyChanged(nameof(IsNotBusy)); }
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));
}
