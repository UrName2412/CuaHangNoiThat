using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersController controller;

    public OrdersPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<OrdersController>();
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await controller.LoadAsync();
    }
}
