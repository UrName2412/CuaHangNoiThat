using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class OrdersController : ObservableObject
{
    private readonly SupabaseService supabase;
    public ObservableCollection<OrderModel> Orders { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool IsEmpty => !IsBusy && Orders.Count == 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public OrdersController(SupabaseService supabase) => this.supabase = supabase;

    public async Task LoadAsync()
    {
        try
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;
            Orders.Clear();
            foreach (var order in await supabase.GetOrdersAsync())
                Orders.Add(order);
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Không thể tải đơn hàng: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasError));
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}