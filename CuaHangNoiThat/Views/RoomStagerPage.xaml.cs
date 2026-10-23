using CuaHangNoiThat.ViewModels;

namespace CuaHangNoiThat.Views;

public partial class RoomStagerPage : ContentPage
{
    public RoomStagerPage()
    {
        InitializeComponent();
        BindingContext = new RoomStagerViewModel();
    }
}
