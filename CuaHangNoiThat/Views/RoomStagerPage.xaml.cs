using CuaHangNoiThat.Controllers;

namespace CuaHangNoiThat.Views;

public partial class RoomStagerPage : ContentPage
{
    public RoomStagerPage()
    {
        InitializeComponent();
        BindingContext = new RoomStagerController();
    }
}
