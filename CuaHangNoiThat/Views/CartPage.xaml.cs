using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class CartPage : ContentPage
{
    private readonly CartController controller;

    public CartPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<CartController>();
        BindingContext = controller;
    }

    protected override bool OnBackButtonPressed()
    {
        if (controller.IsRemoveConfirmationOpen)
        {
            controller.CancelRemoveCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }
}
