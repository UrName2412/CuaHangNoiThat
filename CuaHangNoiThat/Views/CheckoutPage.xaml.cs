using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class CheckoutPage : ContentPage
{
    private readonly CheckoutController controller;

    public CheckoutPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<CheckoutController>();
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await controller.LoadOrderAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        if (controller.IsAddressPickerOpen)
        {
            controller.CloseAddressPickerCommand.Execute(null);
            return true;
        }

        if (controller.IsVoucherPickerOpen)
        {
            controller.CloseVoucherPickerCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }
}
