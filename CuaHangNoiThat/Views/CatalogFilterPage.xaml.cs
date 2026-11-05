using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class CatalogFilterPage : ContentPage
{
    public CatalogFilterPage(CatalogController controller)
    {
        InitializeComponent();
        BindingContext = controller;
    }

    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is CatalogController controller && controller.IsFilterChoiceOpen)
        {
            controller.CloseFilterChoicesCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }
}
