using System.Text.Json;

namespace CuaHangNoiThat.Services;

public sealed record SupabaseConfiguration(string Url, string AnonKey)
{
    public static async Task<SupabaseConfiguration> LoadAsync()
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync("supabase.json");
        var configuration = await JsonSerializer.DeserializeAsync<SupabaseConfiguration>(stream);

        if (configuration is null ||
            string.IsNullOrWhiteSpace(configuration.Url) ||
            string.IsNullOrWhiteSpace(configuration.AnonKey) ||
            configuration.Url.Contains("YOUR_PROJECT", StringComparison.OrdinalIgnoreCase) ||
            configuration.AnonKey.Contains("YOUR_ANON", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Supabase chưa được cấu hình. Hãy điền Url và AnonKey trong Resources/Raw/supabase.json.");
        }

        return configuration with { Url = configuration.Url.TrimEnd('/') };
    }
}
