using System.Net.Http.Json;
using CuaHangNoiThat.Models;

namespace CuaHangNoiThat.Services;

public sealed class ProvinceService(HttpClient httpClient)
{
    private const string ApiUrl = "https://provinces.open-api.vn/api/v2/";
    private IReadOnlyList<AdministrativeArea>? provinces;

    public async Task<IReadOnlyList<AdministrativeArea>> GetProvincesAsync()
    {
        if (provinces is not null) return provinces;
        var items = await httpClient.GetFromJsonAsync<List<AdministrativeArea>>(ApiUrl)
            ?? throw new HttpRequestException("Danh sách tỉnh/thành không hợp lệ.");
        if (items.Count > 0) provinces = items;
        return items;
    }

    public async Task<IReadOnlyList<AdministrativeArea>> GetWardsAsync(int provinceCode)
    {
        var province = await httpClient.GetFromJsonAsync<AdministrativeArea>($"{ApiUrl}p/{provinceCode}/?depth=2");
        if (province is null || province.Code != provinceCode)
            throw new HttpRequestException("Danh sách phường/xã không hợp lệ.");
        return province.Wards;
    }
}
