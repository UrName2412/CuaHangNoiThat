using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileController controller;

    public ProfilePage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<ProfileController>();
        BindingContext = controller;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        controller.Load();
    }
}
