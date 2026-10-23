using CommunityToolkit.Mvvm.ComponentModel;

namespace CuaHangNoiThat.Models;

public enum OrderStatus { Pending, Preparing, Shipping, Delivered }

public partial class OrderModel : ObservableObject
{
    public string Code { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string TotalAmountFormatted => $"{TotalAmount:N0} đ";
    public string Date { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsShipping))]
    [NotifyPropertyChangedFor(nameof(Step2Color))]
    [NotifyPropertyChangedFor(nameof(Step3Color))]
    [NotifyPropertyChangedFor(nameof(Step4Color))]
    private OrderStatus status;

    public string StatusLabel => Status switch
    {
        OrderStatus.Pending => "Tiếp nhận đơn",
        OrderStatus.Preparing => "Đang chế tác",
        OrderStatus.Shipping => "Đang giao & lắp đặt",
        OrderStatus.Delivered => "Đã bàn giao",
        _ => "Đang xử lý"
    };
    public bool IsShipping => Status == OrderStatus.Shipping;
    public Color Step1Color => Color.FromArgb("#171717");
    public Color Step2Color => Status >= OrderStatus.Preparing ? Color.FromArgb("#171717") : Color.FromArgb("#E5E5E5");
    public Color Step3Color => Status >= OrderStatus.Shipping ? Color.FromArgb("#171717") : Color.FromArgb("#E5E5E5");
    public Color Step4Color => Status == OrderStatus.Delivered ? Color.FromArgb("#059669") : Color.FromArgb("#E5E5E5");
}
