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
    [NotifyPropertyChangedFor(nameof(HasError), nameof(IsEmpty))]
    private string errorMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool IsEmpty => !IsBusy && Orders.Count == 0 && !HasError;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public OrdersController(SupabaseService supabase) => this.supabase = supabase;

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsEmpty));
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Orders.Clear();
            var customerId = supabase.CurrentUser?.Id;
            var orders = await supabase.GetOrdersAsync();
            if (customerId != supabase.CurrentUser?.Id)
                throw new InvalidOperationException("Tài khoản đã thay đổi. Vui lòng tải lại danh sách đơn hàng.");
            foreach (var order in orders)
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
