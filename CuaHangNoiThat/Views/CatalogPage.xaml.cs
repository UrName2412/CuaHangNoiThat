using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class CatalogPage : ContentPage
{
    private readonly CatalogController controller;

    public CatalogPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<CatalogController>();
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await controller.LoadAsync();
    }
}
