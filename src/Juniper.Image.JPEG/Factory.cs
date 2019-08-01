using System;
using System.IO;

using BitMiracle.LibJpeg;

using Juniper.Serialization;

namespace Juniper.Image.JPEG
{
    public class Factory : IFactory<ImageData>
    {
        public static Size ReadDimensions(byte[] data)
        {
            for (int i = 0; i < data.Length - 1; ++i)
            {
                var a = data[i];
                var b = data[i + 1];
                if (a == 0xff && b == 0xc0)
                {
                    var heightHi = data[i + 5];
                    var heightLo = data[i + 6];
                    var widthHi = data[i + 7];
                    var widthLo = data[i + 8];

                    var width = widthHi << 8 | widthLo;
                    var height = heightHi << 8 | heightLo;
                    return new Size(width, height);
                }
            }

            return default;
        }

        /// <summary>
        /// Decodes a raw file buffer of JPEG data into raw image buffer, with width and height saved.
        /// </summary>
        /// <param name="imageStream">Jpeg bytes.</param>
        public ImageData Deserialize(Stream imageStream)
        {
            var source = ImageData.DetermineSource(imageStream);
            using (var jpeg = new JpegImage(imageStream))
            {
                var stride = jpeg.Width * jpeg.ComponentsPerSample;
                int numRows = jpeg.Height;
                var data = new byte[numRows * stride];
                for (var i = 0; i < jpeg.Height; ++i)
                {
                    var rowIndex = ImageData.GetRowIndex(numRows, i, true);
                    var row = jpeg.GetRow(rowIndex);
                    Array.Copy(row.ToBytes(), 0, data, i * stride, stride);
                }

                return new ImageData(
                    source,
                    ImageFormat.JPEG,
                    jpeg.Width,
                    jpeg.Height,
                    data);
            }
        }

        /// <summary>
        /// Encodes a raw file buffer of image data into a JPEG image.
        /// </summary>
        /// <param name="outputStream">Jpeg bytes.</param>
        public void Serialize(Stream outputStream, ImageData image)
        {
            var rows = new SampleRow[image.dimensions.height];
            var rowBuffer = new byte[image.stride];
            for (int i = 0; i < image.dimensions.height; ++i)
            {
                var rowIndex = ImageData.GetRowIndex(image.dimensions.height, i, true);
                var imageDataIndex = rowIndex * image.stride;
                Array.Copy(image.data, imageDataIndex, rowBuffer, 0, rowBuffer.Length);
                rows[i] = new SampleRow(
                    rowBuffer,
                    image.dimensions.width,
                    ImageData.BitsPerComponent,
                    (byte)image.components);
            }

            using (var jpeg = new JpegImage(rows, Colorspace.RGB))
            {
                var compression = new CompressionParameters
                {
                    Quality = 100,
                    SimpleProgressive = false,
                    SmoothingFactor = 1
                };
                jpeg.WriteJpeg(outputStream, compression);
            }
        }
    }
}