using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CuaHangNoiThat.Models;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class OrdersController : ObservableObject
{
    private readonly SupabaseService supabase;
    private const int PageSize = 10;
    private int currentPage = 1;
    private bool hasNextPage;
    private DateTime appliedDate = DateTime.Today;
    private int appliedMode;
    public ObservableCollection<OrderModel> Orders { get; } = new();
    public IReadOnlyList<string> FilterModes { get; } = ["Theo tháng", "Theo ngày", "Theo năm"];

    [ObservableProperty]
    private int selectedFilterMode;

    [ObservableProperty]
    private DateTime selectedDate = DateTime.Today;

    public string SelectedFilterModeText => FilterModes[SelectedFilterMode];
    public string SelectedDateText => SelectedFilterMode switch
    {
        1 => SelectedDate.ToString("dd/MM/yyyy"),
        2 => SelectedDate.ToString("yyyy"),
        _ => SelectedDate.ToString("MM/yyyy")
    };

    partial void OnSelectedFilterModeChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedFilterModeText));
        OnPropertyChanged(nameof(SelectedDateText));
    }

    partial void OnSelectedDateChanged(DateTime value) => OnPropertyChanged(nameof(SelectedDateText));

    public bool CanGoPrevious => IsNotBusy && currentPage > 1;
    public bool CanGoNext => IsNotBusy && hasNextPage;
    public string PageSummary => $"Trang {currentPage} · {Orders.Count} đơn";
    public string PeriodSummary => appliedMode switch
    {
        1 => $"Ngày {appliedDate:dd/MM/yyyy}",
        2 => $"Năm {appliedDate:yyyy}",
        _ => $"Tháng {appliedDate:MM/yyyy}"
    };

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
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
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
            hasNextPage = false;
            var from = appliedMode switch
            {
                1 => appliedDate.Date,
                2 => new DateTime(appliedDate.Year, 1, 1),
                _ => new DateTime(appliedDate.Year, appliedDate.Month, 1)
            };
            var to = appliedMode switch
            {
                1 => from.AddDays(1),
                2 => from.AddYears(1),
                _ => from.AddMonths(1)
            };
            var customerId = supabase.CurrentUser?.Id;
            var orders = await supabase.GetOrdersAsync(from, to, currentPage, PageSize);
            if (customerId != supabase.CurrentUser?.Id)
                throw new InvalidOperationException("Tài khoản đã thay đổi. Vui lòng tải lại danh sách đơn hàng.");
            hasNextPage = orders.Count > PageSize;
            foreach (var order in orders.Take(PageSize))
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
            OnPropertyChanged(nameof(PageSummary));
            OnPropertyChanged(nameof(PeriodSummary));
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task ApplyFilterAsync()
    {
        if (IsBusy) return;
        appliedDate = SelectedDate;
        appliedMode = SelectedFilterMode;
        currentPage = 1;
        await LoadAsync();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious) return;
        currentPage--;
        await LoadAsync();
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task NextPageAsync()
    {
        if (!CanGoNext) return;
        currentPage++;
        await LoadAsync();
    }
}
