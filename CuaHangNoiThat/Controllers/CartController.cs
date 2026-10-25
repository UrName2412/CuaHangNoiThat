using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Controllers;

public partial class CartController : ObservableObject
{
    public FurnitureItem Item { get; } = new()
    {
        Name = "Ghế Thư Giãn Nordic Bouclé",
        Category = "Ghế thư giãn",
        Description = "Gỗ sồi tự nhiên · Vải Bouclé kem tuyết",
        Price = 4850000
    };

    public string Subtotal => Item.PriceFormatted;
    public string ShippingFee => "250.000 đ";
    public string Total => $"{Item.Price + 250000:N0} đ";

    [ObservableProperty]
    private string message = "Giỏ hàng của bạn đang sẵn sàng.";

    [RelayCommand]
    private Task CheckoutAsync() => Shell.Current.DisplayAlertAsync("Xác nhận đơn hàng", "Đội ngũ MOKHA sẽ liên hệ để chốt lịch giao.", "Đóng");
}
