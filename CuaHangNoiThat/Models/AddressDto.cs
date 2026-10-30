using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class AddressDto
{
    [JsonPropertyName("id_diachi")]
    public int IdDiaChi { get; set; }

    [JsonPropertyName("id_khachhang")]
    public int IdKhachHang { get; set; }

    [JsonPropertyName("tinhthanh")]
    public int TinhThanh { get; set; }

    [JsonPropertyName("phuongxa")]
    public int PhuongXa { get; set; }

    [JsonPropertyName("duong")]
    public string Duong { get; set; } = string.Empty;

    [JsonPropertyName("macdinh")]
    public int MacDinh { get; set; }

    public bool IsDefault => MacDinh == 1;
    public bool CanSetDefault => !IsDefault;

    public string DisplayAddress => $"{Duong}, mã phường/xã {PhuongXa}, mã tỉnh/thành {TinhThanh}";
}
