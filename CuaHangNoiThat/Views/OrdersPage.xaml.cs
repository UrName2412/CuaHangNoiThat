using CuaHangNoiThat.ViewModels;

namespace CuaHangNoiThat.Views;

public partial class OrdersPage : ContentPage
{
    public OrdersPage()
    {
        InitializeComponent();
        BindingContext = new OrdersViewModel();
    }
}
