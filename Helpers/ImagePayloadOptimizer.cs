using System.IO.Compression;
using System.Text;

namespace GroceryOrderingApp.Backend.Helpers
{
    public static class ImagePayloadOptimizer
    {
        public const int MaxUploadBytes = 50 * 1024;
        private const string CompressedPrefix = "gzdata:";
        private const int MinCompressLength = 100_000;

        public static string? CompressForStorage(string? imagePayload)
        {
            if (string.IsNullOrWhiteSpace(imagePayload))
            {
                return imagePayload;
            }

            var value = imagePayload.Trim();
            if (!value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) || value.Length < MinCompressLength)
            {
                return value;
            }

            try
            {
                var sourceBytes = Encoding.UTF8.GetBytes(value);
                using var output = new MemoryStream();
                using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
                {
                    gzip.Write(sourceBytes, 0, sourceBytes.Length);
                }

                var compressed = CompressedPrefix + Convert.ToBase64String(output.ToArray());
                return compressed.Length < value.Length ? compressed : value;
            }
            catch
            {
                return value;
            }
        }

        public static bool IsWithinUploadLimit(string? imagePayload)
        {
            if (string.IsNullOrWhiteSpace(imagePayload))
            {
                return true;
            }

            var value = ExpandForResponse(imagePayload)?.Trim();
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var commaIndex = value.IndexOf(',');
            if (commaIndex < 0 || commaIndex >= value.Length - 1)
            {
                return true;
            }

            try
            {
                var bytes = Convert.FromBase64String(value[(commaIndex + 1)..]);
                return bytes.Length <= MaxUploadBytes;
            }
            catch
            {
                return false;
            }
        }

        public static string? ExpandForResponse(string? imagePayload)
        {
            if (string.IsNullOrWhiteSpace(imagePayload))
            {
                return imagePayload;
            }

            var value = imagePayload.Trim();
            if (!value.StartsWith(CompressedPrefix, StringComparison.Ordinal))
            {
                return value;
            }

            try
            {
                var base64 = value[CompressedPrefix.Length..];
                var compressedBytes = Convert.FromBase64String(base64);
                using var input = new MemoryStream(compressedBytes);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
            catch
            {
                return imagePayload;
            }
        }
    }
}
