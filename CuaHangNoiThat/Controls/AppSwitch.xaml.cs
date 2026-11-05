namespace CuaHangNoiThat.Controls;

public partial class AppSwitch : ContentView
{
    public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
        nameof(IsToggled), typeof(bool), typeof(AppSwitch), false, BindingMode.TwoWay);

    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(
        nameof(AccessibleName), typeof(string), typeof(AppSwitch), string.Empty);

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public string AccessibleName
    {
        get => (string)GetValue(AccessibleNameProperty);
        set => SetValue(AccessibleNameProperty, value);
    }

    public AppSwitch() => InitializeComponent();

    private void OnToggleClicked(object? sender, EventArgs e) => IsToggled = !IsToggled;
}
