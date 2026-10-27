using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class CartPage : ContentPage
{
    public CartPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<CartController>();
    }
}
