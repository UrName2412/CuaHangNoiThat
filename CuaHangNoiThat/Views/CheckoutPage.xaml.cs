using CuaHangNoiThat.Controllers;
using CuaHangNoiThat.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class CheckoutPage : ContentPage
{
    private readonly CheckoutController controller;
    private bool contactInitialized;

    public CheckoutPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<CheckoutController>();
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (await controller.LoadOrderAsync(initializeContact: !contactInitialized))
            contactInitialized = true;
    }

    private async void OnVoucherSelected(object? sender, EventArgs e)
    {
        if (BindingContext is CheckoutController { IsBusy: false } checkout &&
            sender is Picker { SelectedItem: VoucherDto voucher })
            await checkout.SelectVoucherCommand.ExecuteAsync(voucher);
    }
}
