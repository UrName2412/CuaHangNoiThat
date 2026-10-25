using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class CatalogPage : ContentPage
{
    public CatalogPage()
    {
        InitializeComponent();
        BindingContext = new CatalogController();
    }
}
