using CuaHangNoiThat.ViewModels;

namespace CuaHangNoiThat.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        BindingContext = new HomeViewModel();
    }
}
