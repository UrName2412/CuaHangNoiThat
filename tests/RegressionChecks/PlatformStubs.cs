// Only platform I/O is replaced; the checks compile the actual app services/models.
internal sealed class Shell
{
    public static Shell Current { get; } = new();
    public TestNavigation Navigation { get; } = new();
    public Task GoToAsync(string route) => Task.CompletedTask;
}

internal sealed class TestNavigation
{
    public int ModalDepth { get; private set; }
    public Task PushModalAsync(NavigationPage page) { ModalDepth++; return Task.CompletedTask; }
    public Task PopModalAsync()
    {
        if (ModalDepth == 0) throw new InvalidOperationException("No filter modal is open.");
        ModalDepth--;
        return Task.CompletedTask;
    }
}

internal sealed class NavigationPage
{
    public NavigationPage(object rootPage) { }
}

internal sealed class SecureStorage
{
    private readonly Dictionary<string, string> values = new();
    public static SecureStorage Default { get; } = new();
    public Task<string?> GetAsync(string key) => Task.FromResult(values.GetValueOrDefault(key));
    public Task SetAsync(string key, string value) { values[key] = value; return Task.CompletedTask; }
    public bool Remove(string key) => values.Remove(key);
}

internal static class FileSystem
{
    public static Task<Stream> OpenAppPackageFileAsync(string name) => Task.FromResult<Stream>(
        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            "{\"Url\":\"https://registration.test\",\"AnonKey\":\"test-key\"}")));
}

namespace CuaHangNoiThat.Views
{
    internal sealed class CatalogFilterPage
    {
        public CatalogFilterPage(CuaHangNoiThat.Controllers.CatalogController controller) { }
    }
}
