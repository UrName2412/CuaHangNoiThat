using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    public ObservableCollection<OrderModel> Orders { get; } =
    [
        new() { Code = "MK-88219", Date = "19/10/2026", TotalAmount = 6700000, Status = OrderStatus.Shipping },
        new() { Code = "MK-87910", Date = "12/10/2026", TotalAmount = 5200000, Status = OrderStatus.Delivered }
    ];

    [RelayCommand]
    private async Task SimulateNextAsync()
    {
        var order = Orders[0];
        order.Status = order.Status == OrderStatus.Delivered ? OrderStatus.Pending : order.Status + 1;
        await Shell.Current.DisplayAlertAsync("Đã cập nhật", $"Đơn #{order.Code}: {order.StatusLabel}", "Đóng");
    }
}
