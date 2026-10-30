using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class OrderCreateDto
{
    [JsonPropertyName("id_diachigiaohang")]
    public int IdDiaChiGiaoHang { get; set; }

    [JsonPropertyName("tongtien")]
    public decimal TongTien { get; set; }

    [JsonPropertyName("id_khachhang")]
    public int IdKhachHang { get; set; }

    [JsonPropertyName("id_mactkm")]
    public string? IdMaCtkM { get; set; }

    [JsonPropertyName("id_voucher")]
    public string? IdVoucher { get; set; }
}
