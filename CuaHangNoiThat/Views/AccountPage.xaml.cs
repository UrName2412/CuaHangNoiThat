using System.ComponentModel;
using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class AccountPage : ContentPage
{
    private readonly AccountController controller;
    private bool isAnimatingModeSwitch;

    public AccountPage()
    {
        InitializeComponent();
        controller = App.Services.GetRequiredService<AccountController>();
        controller.PropertyChanged += OnControllerPropertyChanged;
        BindingContext = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await controller.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        AuthFormContent.AbortAnimation(nameof(AnimateModeSwitchAsync));
    }

    private void OnControllerPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName != nameof(AccountController.IsRegisterMode) || !controller.IsSignedOut)
            return;

        MainThread.BeginInvokeOnMainThread(async () => await AnimateModeSwitchAsync());
    }

    private async Task AnimateModeSwitchAsync()
    {
        if (isAnimatingModeSwitch || AuthFormContent is null)
            return;

        try
        {
            isAnimatingModeSwitch = true;
            await AuthFormContent.FadeTo(0.75, 80, Easing.CubicIn);
            await AuthFormContent.TranslateTo(0, 2, 0);
            await AuthFormContent.FadeTo(1, 140, Easing.CubicOut);
            await AuthFormContent.TranslateTo(0, 0, 120, Easing.CubicOut);
        }
        finally
        {
            isAnimatingModeSwitch = false;
        }
    }
}
