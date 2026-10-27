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
    private string newAddress = string.Empty;

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
            var addresses = await supabase.GetAddressesAsync();
            Addresses = new ObservableCollection<AddressDto>(addresses);
            OnPropertyChanged(nameof(HasAddresses));
            OnPropertyChanged(nameof(HasNoAddresses));
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
    private async Task AddAddressAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(NewAddress))
        {
            Message = "Vui lòng nhập địa chỉ.";
            return;
        }

        try
        {
            IsBusy = true;
            Message = string.Empty;
            await supabase.AddAddressAsync(NewAddress.Trim());
            NewAddress = string.Empty;
            await LoadAddressesAsync();
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
            await supabase.DeleteAddressAsync(address.DiChi);
            await LoadAddressesAsync();
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

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));
}