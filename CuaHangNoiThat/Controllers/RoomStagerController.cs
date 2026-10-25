using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CuaHangNoiThat.Controllers;

public partial class RoomStagerController : ObservableObject
{
    [ObservableProperty]
    private bool sofaSelected = true;
    [ObservableProperty]
    private bool tableSelected = true;
    [ObservableProperty]
    private bool lampSelected;

    public decimal SetTotal => (SofaSelected ? 24500000 : 0) + (TableSelected ? 12900000 : 0) + (LampSelected ? 1890000 : 0);
    public string SetTotalFormatted => $"{SetTotal:N0} đ";

    partial void OnSofaSelectedChanged(bool value) => OnSelectionChanged();
    partial void OnTableSelectedChanged(bool value) => OnSelectionChanged();
    partial void OnLampSelectedChanged(bool value) => OnSelectionChanged();
    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SetTotal));
        OnPropertyChanged(nameof(SetTotalFormatted));
    }

    [RelayCommand]
    private Task AddSetToCartAsync() => Shell.Current.DisplayAlertAsync("Đã thêm vào giỏ", $"Bộ phòng khách: {SetTotalFormatted}", "Đóng");
}
