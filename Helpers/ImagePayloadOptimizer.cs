using System.IO.Compression;
using System.Text;

namespace GroceryOrderingApp.Backend.Helpers
{
    public static class ImagePayloadOptimizer
    {
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
