namespace CuaHangNoiThat.Services;

internal static class ImageDecodePolicy
{
    internal const int MaxDecodedDimension = 1024;
    private const long MaxSourcePixels = 32_000_000;
    private const int MaxSourceDimension = 16_384;

    internal static int GetSampleSize(int width, int height)
    {
        if (width <= 0 || height <= 0 || width > MaxSourceDimension ||
            height > MaxSourceDimension || (long)width * height > MaxSourcePixels)
            throw new InvalidDataException("Image dimensions exceed the supported limit.");

        var sampleSize = 1;
        while ((width + sampleSize - 1) / sampleSize > MaxDecodedDimension ||
               (height + sampleSize - 1) / sampleSize > MaxDecodedDimension)
            sampleSize *= 2;
        return sampleSize;
    }
}
