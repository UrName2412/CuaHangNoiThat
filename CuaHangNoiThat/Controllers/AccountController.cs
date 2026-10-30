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

    private bool isPasswordHidden = true;
    private string passwordVisibilityText = "Hiện";
    private string messageColor = "#DC2626";

    public bool IsSignedIn => User is not null;
    public bool IsSignedOut => !IsSignedIn;
    public bool IsLoginMode => !IsRegisterMode;
    public bool IsNotBusy => !IsBusy;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public bool IsPasswordHidden
    {
        get => isPasswordHidden;
        set => SetProperty(ref isPasswordHidden, value);
    }

    public string PasswordVisibilityText
    {
        get => passwordVisibilityText;
        set => SetProperty(ref passwordVisibilityText, value);
    }

    public string MessageColor
    {
        get => messageColor;
        set => SetProperty(ref messageColor, value);
    }

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
            ClearMessage();
            User = await supabase.RestoreSessionAsync();
        }
        catch (Exception exception)
        {
            SetErrorMessage($"Khôi phục phiên làm việc thất bại: {exception.Message}");
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
            SetSuccessMessage("Đăng nhập thành công.");
            Password = string.Empty;
        });
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            SetErrorMessage("Vui lòng nhập họ và tên.");
            return;
        }

        if (!ValidateCredentials())
            return;

        await RunAsync(async () =>
        {
            User = await supabase.RegisterAsync(UsernameOrEmail.Trim(), Password, FullName.Trim());
            Password = string.Empty;
            SetSuccessMessage(User is null
                ? "Đăng ký thành công. Hãy kiểm tra email để xác nhận tài khoản."
                : "Đăng ký và đăng nhập thành công.");
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
        SetSuccessMessage("Bạn đã đăng xuất.");
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
        PasswordVisibilityText = IsPasswordHidden ? "Hiện" : "Ẩn";
    }

    [RelayCommand]
    private Task OpenOrdersAsync() => Shell.Current.GoToAsync("OrdersPage");

    [RelayCommand]
    private Task OpenAddressesAsync() => Shell.Current.GoToAsync("AddressPage");

    [RelayCommand]
    private Task OpenProfileAsync() => Shell.Current.GoToAsync("ProfilePage");

    [RelayCommand]
    private Task OpenCartAsync() => Shell.Current.GoToAsync("//CartPage");

    partial void OnUserChanged(AuthenticatedUser? value)
    {
        OnPropertyChanged(nameof(IsSignedIn));
        OnPropertyChanged(nameof(IsSignedOut));
    }

    partial void OnIsRegisterModeChanged(bool value) => OnPropertyChanged(nameof(IsLoginMode));

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));

    partial void OnMessageChanged(string value) => OnPropertyChanged(nameof(HasMessage));

    private bool ValidateCredentials()
    {
        if (string.IsNullOrWhiteSpace(UsernameOrEmail) || string.IsNullOrWhiteSpace(Password))
        {
            SetErrorMessage("Vui lòng nhập email và mật khẩu.");
            return false;
        }

        if (Password.Trim().Length < 6)
        {
            SetErrorMessage("Mật khẩu phải có ít nhất 6 ký tự.");
            return false;
        }

        return true;
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            SetErrorMessage("Yêu cầu trước đang xử lý, vui lòng đợi.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearMessage();
            await action();
        }
        catch (OperationCanceledException)
        {
            SetErrorMessage("Kết nối quá thời gian chờ. Vui lòng thử lại.");
        }
        catch (InvalidOperationException exception)
        {
            SetErrorMessage(exception.Message);
        }
        catch (HttpRequestException exception)
        {
            SetErrorMessage(exception.Message);
        }
        catch (Exception exception)
        {
            SetErrorMessage($"Lỗi: {exception.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearMessage()
    {
        Message = string.Empty;
        MessageColor = "#DC2626";
    }

    private void SetErrorMessage(string value)
    {
        Message = value;
        MessageColor = "#DC2626";
    }

    private void SetSuccessMessage(string value)
    {
        Message = value;
        MessageColor = "#16A34A";
    }
}
