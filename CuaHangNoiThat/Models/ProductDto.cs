using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class ProductDto
{
    [JsonPropertyName("id_sanpham")]
    public int IdSanPham { get; set; }

    [JsonPropertyName("tensanpham")]
    public string? TenSanPham { get; set; }

    [JsonPropertyName("dongia")]
    public decimal? DonGia { get; set; }

    [JsonPropertyName("soluong")]
    public int? SoLuong { get; set; }

    [JsonPropertyName("id_kieudang")]
    public int? IdKieuDang { get; set; }

    [JsonPropertyName("id_mago")]
    public int? IdMaGo { get; set; }

    [JsonPropertyName("hinhanh")]
    public string? HinhAnh { get; set; }

    [JsonPropertyName("mota")]
    public string? MoTa { get; set; }

    [JsonPropertyName("kmtheosanpham")]
    public List<ProductCampaignDto> Promotions { get; set; } = [];
}

public sealed class ProductCampaignDto
{
    [JsonPropertyName("phantram")]
    public decimal? PhanTram { get; set; }

    [JsonPropertyName("chuongtrinhkhuyenmai")]
    public PromotionDto? Promotion { get; set; }
}
