using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class AddressPage : ContentPage
{
    private readonly AddressController controller;

    public AddressPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<AddressController>();
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await controller.LoadAddressesAsync();
    }
}