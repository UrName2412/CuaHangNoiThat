namespace CuaHangNoiThat.Controls;

/// <summary>Bounded presentation cache and explicit HTTP/decode failure fallback.</summary>
public partial class ProductImageView : ContentView
{
    public static readonly BindableProperty UrlProperty = BindableProperty.Create(nameof(Url), typeof(string), typeof(ProductImageView), null,
        propertyChanged: (view, _, _) => { if (((ProductImageView)view).IsLoaded) _ = ((ProductImageView)view).LoadPhotoAsync(); });
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(ProductImageView), string.Empty,
        propertyChanged: (view, _, value) => SemanticProperties.SetDescription(((ProductImageView)view).Photo, $"Ảnh {value}"));
    public string? Url { get => (string?)GetValue(UrlProperty); set => SetValue(UrlProperty, value); }
    public string AccessibleName { get => (string)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(12) };
    private static readonly Dictionary<string, byte[]> Cache = new(StringComparer.Ordinal);
    private static readonly Queue<string> CacheOrder = new();
    private static readonly object CacheLock = new();
    private static int cacheBytes;
    private CancellationTokenSource? request;
    private const int MaxImageBytes = 4 * 1024 * 1024;
    private const int CacheBudget = 12 * 1024 * 1024;

    public ProductImageView()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = LoadPhotoAsync();
        Unloaded += (_, _) => request?.Cancel();
        SizeChanged += (_, _) =>
        {
            if (Width > 0)
            {
                var height = Math.Clamp(Width * 0.9, 120, 280);
                if (Math.Abs(HeightRequest - height) > 1) HeightRequest = height;
            }
        };
    }

    private async Task LoadPhotoAsync()
    {
        request?.Cancel();
        request?.Dispose();
        var current = request = new CancellationTokenSource();
        current.CancelAfter(TimeSpan.FromSeconds(12));
        Photo.Source = null;
        Loading.IsVisible = Loading.IsRunning = false;
        Fallback.IsVisible = true;
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")) return;
        Fallback.IsVisible = false;
        Loading.IsVisible = Loading.IsRunning = true;
        try
        {
            byte[]? bytes;
            lock (CacheLock) Cache.TryGetValue(uri.AbsoluteUri, out bytes);
            if (bytes is null)
            {
                using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, current.Token);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength > MaxImageBytes) throw new InvalidDataException();
                await using var stream = await response.Content.ReadAsStreamAsync(current.Token);
                using var buffer = new MemoryStream();
                var chunk = new byte[16384];
                int count;
                while ((count = await stream.ReadAsync(chunk, current.Token)) > 0)
                {
                    if (buffer.Length + count > MaxImageBytes) throw new InvalidDataException();
                    buffer.Write(chunk, 0, count);
                }
                bytes = buffer.ToArray();
#if ANDROID
                bytes = await Task.Run(() => PrepareAndroidImage(bytes, current.Token), current.Token);
#endif
                current.Token.ThrowIfCancellationRequested();
                lock (CacheLock)
                {
                    if (!Cache.ContainsKey(uri.AbsoluteUri))
                    {
                        while (cacheBytes + bytes.Length > CacheBudget && CacheOrder.TryDequeue(out var oldest))
                            if (Cache.Remove(oldest, out var oldBytes)) cacheBytes -= oldBytes.Length;
                        Cache[uri.AbsoluteUri] = bytes;
                        CacheOrder.Enqueue(uri.AbsoluteUri);
                        cacheBytes += bytes.Length;
                    }
                }
            }
            if (current.IsCancellationRequested || request != current)
            {
                if (request == current) Fallback.IsVisible = true;
                return;
            }
            var data = bytes;
            Photo.Source = ImageSource.FromStream(() => new MemoryStream(data, writable: false));
            Fallback.IsVisible = false;
        }
        catch (Exception)
        {
            if (request == current) Fallback.IsVisible = true;
        }
        finally
        {
            if (request == current) Loading.IsVisible = Loading.IsRunning = false;
        }
    }

#if ANDROID
    private static byte[] PrepareAndroidImage(byte[] bytes, CancellationToken cancellationToken)
    {
        using var options = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
        using var bounds = Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
        var sampleSize = Services.ImageDecodePolicy.GetSampleSize(options.OutWidth, options.OutHeight);
        // Preserve the original metadata for images that already fit the limit.
        if (sampleSize == 1) return bytes;

        cancellationToken.ThrowIfCancellationRequested();
        options.InJustDecodeBounds = false;
        options.InSampleSize = sampleSize;
        options.InScaled = false;
        using var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options)
            ?? throw new InvalidDataException("Image decoding failed.");
        if (bitmap.Width > Services.ImageDecodePolicy.MaxDecodedDimension ||
            bitmap.Height > Services.ImageDecodePolicy.MaxDecodedDimension)
            throw new InvalidDataException("Decoded image exceeds the supported limit.");

        cancellationToken.ThrowIfCancellationRequested();
        // BitmapFactory does not apply EXIF orientation. Preserve it before stripping
        // metadata by re-encoding the sampled bitmap (including mirrored photos).
        using var original = new MemoryStream(bytes, writable: false);
        using var exif = new AndroidX.ExifInterface.Media.ExifInterface(original);
        var orientation = exif.GetAttributeInt(AndroidX.ExifInterface.Media.ExifInterface.TagOrientation, 1);
        using var matrix = new Android.Graphics.Matrix();
        switch (orientation)
        {
            case 2: matrix.SetScale(-1, 1); break;
            case 3: matrix.SetRotate(180); break;
            case 4: matrix.SetRotate(180); matrix.PostScale(-1, 1); break;
            case 5: matrix.SetRotate(90); matrix.PostScale(-1, 1); break;
            case 6: matrix.SetRotate(90); break;
            case 7: matrix.SetRotate(-90); matrix.PostScale(-1, 1); break;
            case 8: matrix.SetRotate(-90); break;
        }
        using var oriented = orientation is >= 2 and <= 8
            ? Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true)
            : null;
        using var output = new MemoryStream();
        // Cache the bounded bitmap, so the UI never decodes the original oversized image.
        if (!(oriented ?? bitmap).Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, output))
            throw new InvalidDataException("Image encoding failed.");
        cancellationToken.ThrowIfCancellationRequested();
        return output.ToArray();
    }
#endif
}
