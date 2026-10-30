using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class VoucherDto
{
    [JsonIgnore]
    public string SelectionLabel => $"{IdVoucher} · Giảm {PhanTram:0.##}%";

    [JsonPropertyName("id_voucher")]
    public string IdVoucher { get; set; } = string.Empty;

    [JsonPropertyName("id_mactkm")]
    public string? IdMaCtkM { get; set; }

    [JsonPropertyName("phantram")]
    public decimal? PhanTram { get; set; }

    [JsonPropertyName("soluong")]
    public int? SoLuong { get; set; }

    [JsonPropertyName("chuongtrinhkhuyenmai")]
    public PromotionDto? Promotion { get; set; }
}
