using CuaHangNoiThat.ViewModels;

namespace CuaHangNoiThat.Views;

public partial class CartPage : ContentPage
{
    public CartPage()
    {
        InitializeComponent();
        BindingContext = new CartViewModel();
    }
}
