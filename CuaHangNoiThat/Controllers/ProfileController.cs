using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CuaHangNoiThat.Services;

namespace CuaHangNoiThat.Controllers;

public partial class ProfileController : ObservableObject
{
    private readonly SupabaseService supabase;

    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string currentPassword = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string passwordMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public bool IsNotBusy => !IsBusy;

    public ProfileController(SupabaseService supabase) => this.supabase = supabase;

    public void Load()
    {
        ClearPasswords();
        Message = string.Empty;
        PasswordMessage = string.Empty;
        var user = supabase.CurrentUser;
        FullName = user?.FullName ?? string.Empty;
        Email = user?.Email ?? string.Empty;
        Phone = user?.Phone ?? string.Empty;
    }

    public void ClearPasswords() => CurrentPassword = NewPassword = ConfirmPassword = string.Empty;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
        {
            Message = "Vui lòng nhập họ tên và email.";
            return;
        }

        if (!CustomerInputValidator.IsValidFullName(FullName))
        {
            Message = "Họ và tên chỉ được chứa chữ cái và khoảng trắng.";
            return;
        }
        if (!CustomerInputValidator.IsValidMobilePhone(Phone))
        {
            Message = "Số điện thoại phải có đúng 10 chữ số và bắt đầu bằng đầu số di động Việt Nam hợp lệ.";
            return;
        }

        try { _ = new MailAddress(Email.Trim()); }
        catch (FormatException) { Message = "Email không đúng định dạng."; return; }

        try
        {
            IsBusy = true;
            Message = string.Empty;
            await supabase.UpdateProfileAsync(FullName, Email, Phone);
            Message = "Đã lưu thông tin tài khoản.";
        }
        catch (Exception exception) { Message = exception.Message; }
        finally { IsBusy = false; OnPropertyChanged(nameof(IsNotBusy)); }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (IsBusy) return;
        if (NewPassword != ConfirmPassword)
        {
            PasswordMessage = "Mật khẩu mới và xác nhận chưa khớp.";
            return;
        }
        try
        {
            IsBusy = true;
            PasswordMessage = string.Empty;
            await supabase.ChangePasswordAsync(CurrentPassword, NewPassword);
            CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
            PasswordMessage = "Đã đổi mật khẩu.";
        }
        catch (Exception exception) { PasswordMessage = exception.Message; }
        finally { IsBusy = false; OnPropertyChanged(nameof(IsNotBusy)); }
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsNotBusy));
}
