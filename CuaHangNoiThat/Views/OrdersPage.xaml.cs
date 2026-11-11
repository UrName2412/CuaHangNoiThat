using CuaHangNoiThat.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CuaHangNoiThat.Views;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersController controller;
    private DateTime visibleMonth;
    private int visibleYearStart;

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

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    protected override bool OnBackButtonPressed()
    {
        if (!FilterOverlay.IsVisible)
            return base.OnBackButtonPressed();

        FilterOverlay.IsVisible = false;
        return true;
    }

    private void OnFilterModeClicked(object? sender, EventArgs e)
    {
        FilterOverlayTitle.Text = "Xem đơn hàng";
        FilterOptions.Children.Clear();
        for (var index = 0; index < controller.FilterModes.Count; index++)
        {
            var mode = index;
            var button = OptionButton(controller.FilterModes[index], index == controller.SelectedFilterMode);
            button.Clicked += (_, _) =>
            {
                controller.SelectedFilterMode = mode;
                FilterOverlay.IsVisible = false;
            };
            FilterOptions.Children.Add(button);
        }
        FilterOverlay.IsVisible = true;
    }

    private void OnDateClicked(object? sender, EventArgs e)
    {
        visibleMonth = new DateTime(controller.SelectedDate.Year, controller.SelectedDate.Month, 1);
        visibleYearStart = 1900 + (controller.SelectedDate.Year - 1900) / 12 * 12;
        RenderDateOptions();
        FilterOverlay.IsVisible = true;
    }

    private void OnCloseFilterClicked(object? sender, EventArgs e) => FilterOverlay.IsVisible = false;

    private void RenderDateOptions()
    {
        FilterOverlayTitle.Text = controller.SelectedFilterMode switch
        {
            1 => "Chọn ngày",
            2 => "Chọn năm",
            _ => "Chọn tháng"
        };
        FilterOptions.Children.Clear();

        if (controller.SelectedFilterMode == 2)
        {
            AddNavigation($"{visibleYearStart} – {Math.Min(visibleYearStart + 11, 2100)}",
                visibleYearStart > 1900,
                visibleYearStart + 12 <= 2100,
                () => visibleYearStart -= 12,
                () => visibleYearStart += 12);
            var years = OptionGrid(3);
            for (var year = visibleYearStart; year <= Math.Min(visibleYearStart + 11, 2100); year++)
            {
                var chosenYear = year;
                AddOption(years, year.ToString(), year == controller.SelectedDate.Year,
                    () => SelectDate(new DateTime(chosenYear, controller.SelectedDate.Month,
                        Math.Min(controller.SelectedDate.Day, DateTime.DaysInMonth(chosenYear, controller.SelectedDate.Month)))));
            }
            FilterOptions.Children.Add(years);
            return;
        }

        var selectingMonth = controller.SelectedFilterMode == 0;
        AddNavigation(selectingMonth ? $"Năm {visibleMonth:yyyy}" : $"Tháng {visibleMonth:MM/yyyy}",
            selectingMonth ? visibleMonth.Year > 1900 : visibleMonth > new DateTime(1900, 1, 1),
            selectingMonth ? visibleMonth.Year < 2100 : visibleMonth < new DateTime(2100, 12, 1),
            () => visibleMonth = visibleMonth.AddMonths(selectingMonth ? -12 : -1),
            () => visibleMonth = visibleMonth.AddMonths(selectingMonth ? 12 : 1));

        if (controller.SelectedFilterMode == 0)
        {
            var months = OptionGrid(3);
            for (var month = 1; month <= 12; month++)
            {
                var chosenMonth = month;
                AddOption(months, $"Tháng {month}",
                    chosenMonth == controller.SelectedDate.Month && visibleMonth.Year == controller.SelectedDate.Year,
                    () => SelectDate(new DateTime(visibleMonth.Year, chosenMonth,
                        Math.Min(controller.SelectedDate.Day, DateTime.DaysInMonth(visibleMonth.Year, chosenMonth)))));
            }
            FilterOptions.Children.Add(months);
            return;
        }

        var calendar = OptionGrid(7);
        calendar.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var weekdayIndex = 0;
        foreach (var weekday in new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" })
        {
            calendar.Add(new Label
            {
                Text = weekday,
                TextColor = ResourceColor("TextSecondary"),
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center
            }, weekdayIndex++, 0);
        }
        var firstWeekday = ((int)visibleMonth.DayOfWeek + 6) % 7;
        for (var gap = 0; gap < firstWeekday; gap++)
        {
            if (calendar.RowDefinitions.Count == 1)
                calendar.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            calendar.Add(new BoxView { Color = Colors.Transparent, HeightRequest = 44 }, gap, 1);
        }

        for (var day = 1; day <= DateTime.DaysInMonth(visibleMonth.Year, visibleMonth.Month); day++)
        {
            var chosenDay = day;
            AddOption(calendar, day.ToString(),
                day == controller.SelectedDate.Day && visibleMonth.Month == controller.SelectedDate.Month && visibleMonth.Year == controller.SelectedDate.Year,
                () => SelectDate(new DateTime(visibleMonth.Year, visibleMonth.Month, chosenDay)));
        }
        FilterOptions.Children.Add(calendar);
    }

    private void AddNavigation(string title, bool canGoBack, bool canGoForward, Action goBack, Action goForward)
    {
        var navigation = new Grid { ColumnDefinitions = new ColumnDefinitionCollection
        {
            new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)
        }, ColumnSpacing = 8 };
        var back = OptionButton("‹", false);
        back.IsEnabled = canGoBack;
        back.WidthRequest = 44;
        back.Clicked += (_, _) => { goBack(); RenderDateOptions(); };
        navigation.Add(back, 0);
        navigation.Add(new Label
        {
            Text = title,
            FontFamily = "OpenSansSemibold",
            TextColor = ResourceColor("TextPrimary"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 1);
        var forward = OptionButton("›", false);
        forward.IsEnabled = canGoForward;
        forward.WidthRequest = 44;
        forward.Clicked += (_, _) => { goForward(); RenderDateOptions(); };
        navigation.Add(forward, 2);
        FilterOptions.Children.Add(navigation);
    }

    private static Grid OptionGrid(int columns)
    {
        var grid = new Grid { ColumnSpacing = columns == 7 ? 3 : 6, RowSpacing = 6 };
        for (var column = 0; column < columns; column++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        return grid;
    }

    private static void AddOption(Grid grid, string text, bool selected, Action choose)
    {
        var index = grid.Children.Count;
        var columns = grid.ColumnDefinitions.Count;
        var row = index / columns;
        if (grid.RowDefinitions.Count <= row)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var button = OptionButton(text, selected);
        if (columns == 7)
        {
            button.MinimumWidthRequest = 32;
            button.Padding = new Thickness(0);
            button.FontSize = 12;
        }
        button.Clicked += (_, _) => choose();
        grid.Add(button, index % columns, row);
    }

    private static Button OptionButton(string text, bool selected) => new()
    {
        Text = text,
        FontFamily = "OpenSansSemibold",
        FontSize = 14,
        CornerRadius = 12,
        MinimumHeightRequest = 44,
        Padding = new Thickness(6, 0),
        BackgroundColor = ResourceColor(selected ? "Primary" : "Secondary"),
        TextColor = ResourceColor(selected ? "White" : "SecondaryDarkText")
    };

    private static Color ResourceColor(string key) => (Color)Application.Current!.Resources[key];

    private void SelectDate(DateTime date)
    {
        controller.SelectedDate = date;
        FilterOverlay.IsVisible = false;
    }
}
