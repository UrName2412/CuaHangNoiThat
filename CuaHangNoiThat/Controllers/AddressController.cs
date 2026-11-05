using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public sealed record LocationChoice(AdministrativeArea Area, bool IsSelected)
{
    public string Label => Area.Name;
}

public partial class AddressController : ObservableObject
{
    private readonly SupabaseService supabase;
    private readonly ProvinceService provinceService;
    private int wardRequestVersion;
    private string? loadedCustomerId;

    [ObservableProperty]
    private ObservableCollection<AddressDto> addresses = new();

    [ObservableProperty]
    private AdministrativeArea? selectedProvince;

    [ObservableProperty]
    private AdministrativeArea? selectedWard;

    public ObservableCollection<AdministrativeArea> Provinces { get; } = new();
    public ObservableCollection<AdministrativeArea> Wards { get; } = new();

    [ObservableProperty] private bool isLoadingProvinces;
    [ObservableProperty] private bool isLoadingWards;
    [ObservableProperty] private string locationMessage = string.Empty;
    [ObservableProperty] private bool isLocationPickerOpen;
    [ObservableProperty] private string locationPickerTitle = string.Empty;
    public ObservableCollection<LocationChoice> LocationChoices { get; } = new();
    private bool choosingProvince;
    public string SelectedProvinceName => SelectedProvince?.Name ?? "Chọn tỉnh/thành phố";
    public string SelectedWardName => SelectedWard?.Name ?? "Chọn phường/xã";

    public bool CanChooseProvince => !IsBusy && !IsLoadingProvinces && Provinces.Count > 0;
    public bool CanChooseWard => !IsBusy && !IsLoadingWards && Wards.Count > 0;
    public bool CanSaveAddress => !IsBusy && IsAddressListLoaded && loadedCustomerId == supabase.CurrentUser?.Id &&
        !IsLoadingWards && SelectedProvince is not null && SelectedWard is not null;
    public bool CanRetryProvinces => !IsBusy && !IsLoadingProvinces && Provinces.Count == 0;
    public bool CanRetryWards => !IsBusy && !IsLoadingWards && SelectedProvince is not null && Wards.Count == 0;

    [ObservableProperty]
    private string street = string.Empty;

    [ObservableProperty]
    private bool makeDefault;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(HasNoAddresses))]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isAddressListLoaded;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanRetryAddresses => !IsBusy && !IsAddressListLoaded;

    [ObservableProperty]
    private bool isBusy = false;

    public bool HasAddresses => Addresses.Count > 0;
    public bool HasNoAddresses => IsAddressListLoaded && !IsBusy && !HasAddresses && !HasError;
    public bool IsNotBusy => !IsBusy;

    public AddressController(SupabaseService supabase, ProvinceService provinceService)
    {
        this.supabase = supabase;
        this.provinceService = provinceService;
    }

    public async Task LoadAddressesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Message = string.Empty;
            ErrorMessage = string.Empty;
            Addresses = new();
            if (loadedCustomerId != supabase.CurrentUser?.Id)
            {
                SelectedProvince = null;
                Street = string.Empty;
                MakeDefault = false;
            }
            await RefreshAddressesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
        if (Provinces.Count == 0)
            await LoadProvincesAsync();
    }

    [RelayCommand]
    private Task RetryAddressesAsync() => LoadAddressesAsync();

    partial void OnAddressesChanged(ObservableCollection<AddressDto> value)
    {
        OnPropertyChanged(nameof(HasAddresses));
        OnPropertyChanged(nameof(HasNoAddresses));
    }

    partial void OnIsAddressListLoadedChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoAddresses));
        OnPropertyChanged(nameof(CanRetryAddresses));
        NotifyLocationState();
    }

    [RelayCommand]
    private async Task LoadProvincesAsync()
    {
        if (IsBusy || IsLoadingProvinces) return;
        try
        {
            IsLoadingProvinces = true;
            LocationMessage = string.Empty;
            var items = await provinceService.GetProvincesAsync();
            SelectedProvince = null;
            Provinces.Clear();
            foreach (var item in items) Provinces.Add(item);
            if (Provinces.Count == 0) LocationMessage = "Chưa có danh sách tỉnh/thành. Vui lòng thử lại.";
        }
        catch (Exception)
        {
            LocationMessage = "Không tải được tỉnh/thành. Kiểm tra kết nối và thử lại.";
        }
        finally { IsLoadingProvinces = false; NotifyLocationState(); }
    }

    partial void OnSelectedProvinceChanged(AdministrativeArea? value)
    {
        OnPropertyChanged(nameof(SelectedProvinceName));
        // Clear the old ward immediately; a late response must not replace a newer selection.
        wardRequestVersion++;
        SelectedWard = null;
        Wards.Clear();
        LocationMessage = string.Empty;
        IsLoadingWards = false;
        NotifyLocationState();
        if (value is not null) _ = LoadWardsAsync();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadWardsAsync()
    {
        var province = SelectedProvince;
        if (province is null || IsBusy) return;
        var version = ++wardRequestVersion;
        SelectedWard = null;
        Wards.Clear();
        try
        {
            IsLoadingWards = true;
            LocationMessage = string.Empty;
            var items = await provinceService.GetWardsAsync(province.Code);
            if (version != wardRequestVersion) return;
            foreach (var item in items) Wards.Add(item);
            if (Wards.Count == 0) LocationMessage = "Chưa có phường/xã cho tỉnh/thành này. Vui lòng thử lại.";
        }
        catch (Exception)
        {
            if (version == wardRequestVersion)
                LocationMessage = "Không tải được phường/xã. Kiểm tra kết nối và thử lại.";
        }
        finally
        {
            if (version == wardRequestVersion) { IsLoadingWards = false; NotifyLocationState(); }
        }
    }

    partial void OnSelectedWardChanged(AdministrativeArea? value)
    {
        OnPropertyChanged(nameof(SelectedWardName));
        NotifyLocationState();
    }

    [RelayCommand]
    private void OpenProvinceChoices()
    {
        if (!CanChooseProvince) return;
        choosingProvince = true;
        LocationPickerTitle = "Tỉnh/thành phố";
        LocationChoices.Clear();
        foreach (var item in Provinces) LocationChoices.Add(new LocationChoice(item, item.Code == SelectedProvince?.Code));
        IsLocationPickerOpen = true;
    }

    [RelayCommand]
    private void OpenWardChoices()
    {
        if (!CanChooseWard) return;
        choosingProvince = false;
        LocationPickerTitle = "Phường/xã";
        LocationChoices.Clear();
        foreach (var item in Wards) LocationChoices.Add(new LocationChoice(item, item.Code == SelectedWard?.Code));
        IsLocationPickerOpen = true;
    }

    [RelayCommand]
    private void SelectLocation(LocationChoice? choice)
    {
        if (!IsLocationPickerOpen || choice is null || !LocationChoices.Contains(choice)) return;
        IsLocationPickerOpen = false;
        if (choosingProvince)
            SelectedProvince = choice.Area;
        else
            SelectedWard = choice.Area;
    }

    [RelayCommand]
    private void CloseLocationChoices() => IsLocationPickerOpen = false;
    partial void OnIsLoadingProvincesChanged(bool value) => NotifyLocationState();
    partial void OnIsLoadingWardsChanged(bool value) => NotifyLocationState();

    private void NotifyLocationState()
    {
        OnPropertyChanged(nameof(CanChooseProvince));
        OnPropertyChanged(nameof(CanChooseWard));
        OnPropertyChanged(nameof(CanSaveAddress));
        OnPropertyChanged(nameof(CanRetryProvinces));
        OnPropertyChanged(nameof(CanRetryWards));
    }

    private async Task RefreshAddressesAsync()
    {
        IsAddressListLoaded = false;
        var customerId = supabase.CurrentUser?.Id;
        var addresses = await supabase.GetAddressesAsync();
        if (customerId != supabase.CurrentUser?.Id)
            throw new InvalidOperationException("Tài khoản đã thay đổi. Vui lòng tải lại danh sách địa chỉ.");
        Addresses = new ObservableCollection<AddressDto>(addresses);
        loadedCustomerId = customerId;
        IsAddressListLoaded = true;
    }

    [RelayCommand]
    private async Task AddAddressAsync()
    {
        if (IsBusy)
            return;

        Message = ErrorMessage = string.Empty;
        if (!IsAddressListLoaded || loadedCustomerId != supabase.CurrentUser?.Id)
        {
            ErrorMessage = "Vui lòng tải lại danh sách địa chỉ trước khi lưu.";
            return;
        }

        var province = SelectedProvince;
        var ward = SelectedWard;
        if (IsLoadingWards || province is null || ward is null || !Provinces.Contains(province) ||
            !Wards.Contains(ward) || string.IsNullOrWhiteSpace(Street))
        {
            ErrorMessage = "Vui lòng chọn tỉnh/thành, phường/xã và nhập số nhà, tên đường.";
            return;
        }

        try
        {
            IsBusy = true;
            Message = string.Empty;
            ErrorMessage = string.Empty;
            await supabase.AddAddressAsync(province.Code, ward.Code, Street.Trim(), MakeDefault || !HasAddresses);
            SelectedProvince = null;
            Street = string.Empty;
            await RefreshAddressesAsync();
            Message = "Địa chỉ đã được thêm.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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
        if (address is null || IsBusy || !Addresses.Contains(address) || loadedCustomerId != supabase.CurrentUser?.Id)
            return;

        try
        {
            IsBusy = true;
            Message = string.Empty;
            ErrorMessage = string.Empty;
            await supabase.DeleteAddressAsync(address.IdDiaChi);
            await RefreshAddressesAsync();
            Message = "Địa chỉ đã bị xóa.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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
        if (address is null || address.IsDefault || IsBusy || !Addresses.Contains(address) || loadedCustomerId != supabase.CurrentUser?.Id) return;
        try
        {
            IsBusy = true;
            Message = ErrorMessage = string.Empty;
            await supabase.SetDefaultAddressAsync(address.IdDiaChi);
            await RefreshAddressesAsync();
            Message = "Đã cập nhật địa chỉ mặc định.";
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; OnPropertyChanged(nameof(IsNotBusy)); }
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(HasNoAddresses));
        OnPropertyChanged(nameof(CanRetryAddresses));
        NotifyLocationState();
    }
}
