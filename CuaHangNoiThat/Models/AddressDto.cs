using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class AddressDto
{
    [JsonPropertyName("diachi")]
    public string DiChi { get; set; } = string.Empty;

    [JsonPropertyName("id_khachhang")]
    public int IdKhachHang { get; set; }
}