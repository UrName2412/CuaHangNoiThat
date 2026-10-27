using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class AccountController : ObservableObject
{
    private readonly SupabaseService supabase;
    private readonly CartController cart;

    [ObservableProperty] private string usernameOrEmail = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private bool isRegisterMode;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private AuthenticatedUser? user;
    [ObservableProperty] private string message = string.Empty;

    public bool IsSignedIn => User is not null;
    public bool IsSignedOut => !IsSignedIn;
    public bool IsLoginMode => !IsRegisterMode;
    public bool IsNotBusy => !IsBusy;

    public AccountController(SupabaseService supabase, CartController cart)
    {
        this.supabase = supabase;
        this.cart = cart;
    }

    public async Task InitializeAsync()
    {
        if (User is not null || IsBusy)
            return;

        try
        {
            IsBusy = true;
            Message = string.Empty;
            User = await supabase.RestoreSessionAsync();
        }
        catch (Exception exception)
        {
            Message = $"Khôi phục phiên làm việc thất bại: {exception.Message}";
            User = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!ValidateCredentials())
            return;

        await RunAsync(async () =>
        {
            User = await supabase.LoginAsync(UsernameOrEmail.Trim(), Password);
            Message = "Đăng nhập thành công.";
            Password = string.Empty;
        });
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName) || !ValidateCredentials())
            return;

        await RunAsync(async () =>
        {
            User = await supabase.RegisterAsync(UsernameOrEmail.Trim(), Password, FullName.Trim());
            Password = string.Empty;
            Message = User is null
                ? "Đăng ký thành công. Hãy kiểm tra email để xác nhận tài khoản."
                : "Đăng ký và đăng nhập thành công.";
        });
    }

    [RelayCommand]
    private void ShowLogin() => IsRegisterMode = false;

    [RelayCommand]
    private void ShowRegister() => IsRegisterMode = true;

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await supabase.SignOutAsync();
        cart.Clear();
        User = null;
        Message = "Bạn đã đăng xuất.";
    }

    [RelayCommand]
    private Task OpenOrdersAsync() => Shell.Current.GoToAsync("OrdersPage");

    partial void OnUserChanged(AuthenticatedUser? value)
    {
        OnPropertyChanged(nameof(IsSignedIn));
        OnPropertyChanged(nameof(IsSignedOut));
    }

    partial void OnIsRegisterModeChanged(bool value) => OnPropertyChanged(nameof(IsLoginMode));

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));

    private bool ValidateCredentials()
    {
        if (string.IsNullOrWhiteSpace(UsernameOrEmail) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Vui lòng nhập email và mật khẩu.";
            return false;
        }

        if (Password.Trim().Length < 6)
        {
            Message = "Mật khẩu phải có ít nhất 6 ký tự.";
            return false;
        }

        return true;
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            Message = "Yêu cầu trước đang xử lý, vui lòng đợi.";
            return;
        }

        try
        {
            IsBusy = true;
            Message = string.Empty;
            await action();
        }
        catch (OperationCanceledException)
        {
            Message = "Kết nối quá thời gian chờ. Vui lòng thử lại.";
        }
        catch (InvalidOperationException exception)
        {
            Message = exception.Message;
        }
        catch (HttpRequestException exception)
        {
            Message = exception.Message;
        }
        catch (Exception exception)
        {
            Message = $"Lỗi: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
