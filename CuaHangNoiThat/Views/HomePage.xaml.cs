using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        BindingContext = new HomeController();
    }
}
