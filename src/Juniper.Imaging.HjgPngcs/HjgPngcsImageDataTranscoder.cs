using System;

using Hjg.Pngcs;

using Juniper.Progress;

namespace Juniper.Imaging.HjgPngcs
{
    public class HjgPngcsImageDataTranscoder : IImageTranscoder<ImageLines, ImageData>
    {
        /// <summary>
        /// Decodes a raw file buffer of PNG data into raw image buffer, with width and height saved.
        /// </summary>
        /// <param name="imageStream">Png bytes.</param>
        public ImageData TranslateTo(ImageLines rows, IProgress prog = null)
        {
            var numRows = rows.Nrows;
            var data = new byte[numRows * rows.elementsPerRow];
            for (var i = 0; i < numRows; ++i)
            {
                prog?.Report(i, numRows);
                var row = rows.ScanlinesB[i];
                Array.Copy(row, 0, data, i * rows.elementsPerRow, row.Length);
                prog?.Report(i + 1, numRows);
            }

            return new ImageData(
                rows.ImgInfo.BytesPerRow / rows.ImgInfo.BytesPixel,
                rows.Nrows,
                rows.channels,
                HTTP.MediaType.Image.Png,
                data);
        }

        /// <summary>
        /// Encodes a raw file buffer of image data into a PNG image.
        /// </summary>
        /// <param name="outputStream">Png bytes.</param>
        public ImageLines TranslateFrom(ImageData image, IProgress prog)
        {
            var imageInfo = new Hjg.Pngcs.ImageInfo(
                image.info.dimensions.width,
                image.info.dimensions.height,
                image.info.bitsPerSample / image.info.components,
                image.info.components == 4);

            var imageLines = new ImageLines(
                imageInfo,
                ImageLine.ESampleType.BYTE,
                true,
                0,
                image.info.dimensions.height,
                image.info.stride);

            for (var i = 0; i < image.info.dimensions.height; ++i)
            {
                prog?.Report(i, image.info.dimensions.height);
                var dataIndex = i * image.info.stride;
                var line = imageLines.ScanlinesB[i];
                Array.Copy(image.data, dataIndex, line, 0, image.info.stride);
                prog?.Report(i + 1, image.info.dimensions.height);
            }

            return imageLines;
        }
    }
}