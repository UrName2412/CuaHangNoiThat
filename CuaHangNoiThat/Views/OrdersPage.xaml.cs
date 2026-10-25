using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class OrdersPage : ContentPage
{
    public OrdersPage()
    {
        InitializeComponent();
        BindingContext = new OrdersController();
    }
}
