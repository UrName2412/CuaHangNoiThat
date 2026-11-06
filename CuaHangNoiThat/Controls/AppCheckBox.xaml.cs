using System.Windows.Input;

namespace CuaHangNoiThat.Controls;

public partial class AppCheckBox : ContentView
{
    public static readonly BindableProperty IsCheckedProperty = BindableProperty.Create(
        nameof(IsChecked), typeof(bool), typeof(AppCheckBox), false, BindingMode.TwoWay);

    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(
        nameof(AccessibleName), typeof(string), typeof(AppCheckBox), string.Empty);

    public static readonly BindableProperty ToggleCommandProperty = BindableProperty.Create(
        nameof(ToggleCommand), typeof(ICommand), typeof(AppCheckBox));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public string AccessibleName
    {
        get => (string)GetValue(AccessibleNameProperty);
        set => SetValue(AccessibleNameProperty, value);
    }

    public ICommand? ToggleCommand
    {
        get => (ICommand?)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    public AppCheckBox() => InitializeComponent();

    private void OnToggleClicked(object? sender, EventArgs e)
    {
        if (ToggleCommand is { } command)
        {
            if (command.CanExecute(null)) command.Execute(null);
        }
        else
            IsChecked = !IsChecked;
    }
}
