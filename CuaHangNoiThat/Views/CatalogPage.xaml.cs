using CuaHangNoiThat.ViewModels;

namespace CuaHangNoiThat.Views;

public partial class CatalogPage : ContentPage
{
    public CatalogPage()
    {
        InitializeComponent();
        BindingContext = new CatalogViewModel();
    }
}
