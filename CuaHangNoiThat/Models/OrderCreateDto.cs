using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class OrderCreateDto
{
    [JsonPropertyName("diachigiaohang")]
    public string DiaChiGiaoHang { get; set; } = string.Empty;

    [JsonPropertyName("tongtien")]
    public decimal TongTien { get; set; }

    [JsonPropertyName("id_khachhang")]
    public int IdKhachHang { get; set; }

    [JsonPropertyName("id_mactkm")]
    public string? IdMaCtkM { get; set; }

    [JsonPropertyName("id_voucher")]
    public string? IdVoucher { get; set; }
}