using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class PromotionDto
{
    [JsonPropertyName("id_mactkm")]
    public string IdMaCtkM { get; set; } = string.Empty;

    [JsonPropertyName("tenctkm")]
    public string? TenCtkM { get; set; }

    [JsonPropertyName("ngaybd")]
    public DateTime? NgayBd { get; set; }

    [JsonPropertyName("ngaykt")]
    public DateTime? NgayKt { get; set; }

    [JsonPropertyName("ghichu")]
    public string? GhiChu { get; set; }
}