using System;
using System.IO;
using System.Threading.Tasks;

using Juniper.Progress;

namespace Juniper.Image
{
    /// <summary>
    /// The raw bytes and dimensions of an image that has been loaded either off disk or across the 'net.
    /// </summary>
    public class RawImage : ICloneable
    {
        public enum ImageSource
        {
            None,
            File,
            Network
        }

        public const int BytesPerComponent = sizeof(byte);
        public const int BitsPerComponent = 8 * BytesPerComponent;

        public readonly ImageSource source;
        public readonly byte[] data;
        public readonly Size dimensions;
        public readonly int stride;
        public readonly int components;
        public readonly int bytesPerSample;
        public readonly int bitsPerSample;

        public RawImage(ImageSource source, Size dimensions, byte[] data)
        {
            this.source = source;
            this.dimensions = dimensions;
            this.data = data;
            stride = data.Length / dimensions.height;
            components = stride / dimensions.width;
            bytesPerSample = BytesPerComponent * components;
            bitsPerSample = 8 * bytesPerSample;
        }

        public RawImage(ImageSource source, int width, int height, byte[] data)
            : this(source, new Size(width, height), data)
        {
        }

        public object Clone()
        {
            return new RawImage(source, dimensions, (byte[])data.Clone());
        }

        public static ImageSource DetermineSource(Stream imageStream)
        {
            var source = RawImage.ImageSource.None;
            if (imageStream is FileStream)
            {
                source = ImageSource.File;
            }
            else if (imageStream is CachingStream)
            {
                source = ImageSource.Network;
            }

            return source;
        }

        public static string GetContentType(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.JPEG: return "image/jpeg";
                case ImageFormat.PNG: return "image/png";
                default: return "application/unknown";
            }
        }

        public static string GetExtension(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.JPEG: return "jpeg";
                case ImageFormat.PNG: return "png";
                default: return "raw";
            }
        }

        public static int GetRowIndex(int numRows, int i, bool flipImage)
        {
            int rowIndex = i;
            if (flipImage)
            {
                rowIndex = numRows - i - 1;
            }

            return rowIndex;
        }

        private static Task<RawImage> CombineTilesAsync(int columns, int rows, params RawImage[] images)
        {
            return Task.Run(() => CombineTiles(columns, rows, images));
        }

        private static RawImage CombineTiles(int columns, int rows, params RawImage[] images)
        {
            if (images == null)
            {
                throw new ArgumentNullException($"Parameter {nameof(images)} must not be null.");
            }

            if (images.Length == 0)
            {
                throw new ArgumentException($"Parameter {nameof(images)} must have at least one image.");
            }

            var numTiles = columns * rows;
            if (images.Length != numTiles)
            {
                throw new ArgumentException($"Expected {nameof(images)} parameter to be {numTiles} long, but it was {images.Length} long.");
            }

            bool anyNotNull = false;
            RawImage firstImage = default;
            for (int i = 0; i < images.Length; ++i)
            {
                var img = images[i];
                if (img != null)
                {
                    if (!anyNotNull)
                    {
                        firstImage = images[i];
                    }

                    anyNotNull = true;
                    if (img?.dimensions.width != firstImage.dimensions.width || img?.dimensions.height != firstImage.dimensions.height)
                    {
                        throw new ArgumentException($"All elements of {nameof(images)} must be the same width and height. Image {i} did not match image 0.");
                    }
                }
            }

            if (!anyNotNull)
            {
                throw new ArgumentNullException($"Expected at least one image in {nameof(images)} to be not null");
            }

            var imageStride = firstImage.stride;
            var imageHeight = firstImage.dimensions.height;
            var bufferStride = columns * imageStride;
            var bufferHeight = rows * imageHeight;
            var bufferLength = bufferStride * bufferHeight;
            var buffer = new byte[bufferLength];
            for (
                int bufferI = 0;
                bufferI < bufferLength;
                bufferI += imageStride)
            {
                var bufferX = bufferI % bufferStride;
                var bufferY = bufferI / bufferStride;
                var tileX = bufferX / imageStride;
                var tileY = bufferY / imageHeight;
                var tileI = tileY * columns + tileX;
                var tile = images[tileI];
                if (tile != null)
                {
                    var imageY = bufferY % imageHeight;
                    var imageI = imageY * imageStride;
                    Array.Copy(tile.data, imageI, buffer, bufferI, imageStride);
                }
            }

            return new RawImage(
                ImageSource.None,
                columns * firstImage.dimensions.width,
                rows * firstImage.dimensions.height,
                buffer);
        }

        public static Task<RawImage> Combine6Squares(RawImage north, RawImage east, RawImage west, RawImage south, RawImage up, RawImage down)
        {
            return CombineTilesAsync(
                3, 2,
                west, south, east,
                down, up, north);
        }

        public static Task<RawImage> CombineCross(RawImage north, RawImage east, RawImage west, RawImage south, RawImage up, RawImage down)
        {
            return CombineTilesAsync(
                4, 3,
                null, up, null, null,
                west, north, east, south,
                null, down, null, null);
        }
    }
}