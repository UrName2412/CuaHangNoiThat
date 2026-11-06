using System.Text.RegularExpressions;

namespace CuaHangNoiThat.Services;

public static class CustomerInputValidator
{
    private static readonly Regex FullNamePattern = new(@"^\p{L}+(?: +\p{L}+)*$", RegexOptions.CultureInvariant);
    private static readonly Regex MobilePhonePattern = new(@"^0(?:3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$", RegexOptions.CultureInvariant);

    public static bool IsValidFullName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && FullNamePattern.IsMatch(value.Trim());

    public static bool IsValidMobilePhone(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MobilePhonePattern.IsMatch(value.Trim());
}
