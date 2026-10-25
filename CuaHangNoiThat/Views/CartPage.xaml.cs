using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class CartPage : ContentPage
{
    public CartPage()
    {
        InitializeComponent();
        BindingContext = new CartController();
    }
}
