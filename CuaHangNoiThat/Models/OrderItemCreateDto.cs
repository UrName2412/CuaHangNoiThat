using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class OrderItemCreateDto
{
    [JsonPropertyName("id_sanpham")]
    public int IdSanPham { get; set; }

    [JsonPropertyName("soluong")]
    public int SoLuong { get; set; }

    [JsonPropertyName("dongia")]
    public decimal DonGia { get; set; }

    [JsonPropertyName("thanhtien")]
    public decimal ThanhTien { get; set; }

    [JsonPropertyName("id_mactkm")]
    public string? IdMaCtkM { get; set; }
}