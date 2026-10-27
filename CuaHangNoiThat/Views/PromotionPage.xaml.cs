using Microsoft.Extensions.DependencyInjection;
using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class PromotionPage : ContentPage
{
    public PromotionPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<PromotionController>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((PromotionController)BindingContext).LoadAsync();
    }
}