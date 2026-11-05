using System.Text.Json.Serialization;

namespace CuaHangNoiThat.Models;

public sealed class AdministrativeArea
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("wards")]
    public List<AdministrativeArea> Wards { get; set; } = new();
}
