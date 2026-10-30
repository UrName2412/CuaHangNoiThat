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

    private void OnSearchFocused(object? sender, FocusEventArgs e) =>
        SearchSurface.Stroke = new SolidColorBrush((Color)Application.Current!.Resources["Primary"]);

    private void OnSearchUnfocused(object? sender, FocusEventArgs e) =>
        SearchSurface.Stroke = new SolidColorBrush((Color)Application.Current!.Resources["BorderSubtle"]);

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
#if !ANDROID
        SubmitSearch((Entry)sender!);
#endif
    }

    private void SubmitSearch(Entry entry)
    {
        entry.Unfocus();
        Dispatcher.Dispatch(async () => await ((HomeController)BindingContext).SearchCatalogCommand.ExecuteAsync(null));
    }

#if ANDROID
    private Android.Widget.EditText? nativeSearch;
    private Entry? searchEntry;
#endif
    private void OnSearchHandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        if (nativeSearch is not null) nativeSearch.EditorAction -= OnAndroidSearch;
        searchEntry = (Entry)sender!;
        nativeSearch = searchEntry.Handler?.PlatformView as Android.Widget.EditText;
        if (nativeSearch is not null) nativeSearch.EditorAction += OnAndroidSearch;
#endif
    }

#if ANDROID
    private void OnAndroidSearch(object? sender, Android.Widget.TextView.EditorActionEventArgs e)
    {
        if (e.ActionId != Android.Views.InputMethods.ImeAction.Search && e.Event?.KeyCode != Android.Views.Keycode.Enter) return;
        // Consume Enter before switching tabs, so Android does not focus a detached search view.
        e.Handled = true;
        if (e.Event is null || e.Event.Action == Android.Views.KeyEventActions.Down && e.Event.RepeatCount == 0)
            if (searchEntry is not null) SubmitSearch(searchEntry);
    }
#endif
}
