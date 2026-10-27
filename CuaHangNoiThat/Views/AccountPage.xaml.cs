using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class AccountPage : ContentPage
{
    private readonly AccountController controller;

    public AccountPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<AccountController>();
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await controller.InitializeAsync();
    }
}
