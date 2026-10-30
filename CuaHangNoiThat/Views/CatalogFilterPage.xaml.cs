using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class CatalogFilterPage : ContentPage
{
    public CatalogFilterPage(CatalogController controller)
    {
        InitializeComponent();
        BindingContext = controller;
    }
}
