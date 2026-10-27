using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<HomeController>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((HomeController)BindingContext).LoadAsync();
    }
}
