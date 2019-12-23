using System.Globalization;

namespace Hjg.Pngcs
{
    /// <summary>
    /// Simple immutable wrapper for basic image info
    /// </summary>
    /// <remarks>
    /// Some parameters are clearly redundant
    /// The constructor requires an 'ortogonal' subset
    /// http://www.w3.org/TR/PNG/#11IHDR
    /// </remarks>
    public class ImageInfo
    {
        private const int MAX_COLS_ROWS_VAL = 400000; // very big value, but no so ridiculous as 2^32

        /// <summary>
        /// Image width, in pixels
        /// </summary>
        public readonly int Cols;

        /// <summary>
        /// Image height, in pixels
        /// </summary>
        public readonly int Rows;

        /// <summary>
        /// Bits per sample (per channel) in the buffer.
        /// </summary>
        /// <remarks>
        /// This is 8 or 16 for RGB/ARGB images.
        /// For grayscale, it's 8 (or 1 2 4 ).
        /// For indexed images, number of bits per palette index (1 2 4 8).
        ///</remarks>
        public readonly int BitDepth;

        /// <summary>
        /// Number of channels, used in the buffer
        /// </summary>
        /// <remarks>
        /// WARNING: This is 3-4 for rgb/rgba, but 1 for palette/gray !
        ///</remarks>
        public readonly int Channels;

        /// <summary>
        /// Bits used for each pixel in the buffer
        /// </summary>
        /// <remarks>equals <c>channels * bitDepth</c>
        /// </remarks>
        public readonly int BitspPixel;

        /// <summary>
        /// Bytes per pixel, rounded up
        /// </summary>
        /// <remarks>This is mainly for internal use (filter)</remarks>
        public readonly int BytesPixel;

        /// <summary>
        /// Bytes per row, rounded up
        /// </summary>
        /// <remarks>equals <c>ceil(bitspp*cols/8)</c></remarks>
        public readonly int BytesPerRow;

        /// <summary>
        /// Samples (scalar values) per row
        /// </summary>
        /// <remarks>
        /// Equals <c>cols * channels</c>
        /// </remarks>
        public readonly int SamplesPerRow;

        /// <summary>
        /// Number of values in our scanline, which might be packed.
        /// </summary>
        /// <remarks>
        /// Equals samplesPerRow if not packed. Elsewhere, it's lower
        /// For internal use, mostly.
        /// </remarks>
        public readonly int SamplesPerRowPacked;

        /// <summary>
        /// flag: has alpha channel
        /// </summary>
        public readonly bool Alpha;

        /// <summary>
        /// flag: is grayscale (G/GA)
        /// </summary>
        public readonly bool Greyscale;

        /// <summary>
        /// flag: has palette
        /// </summary>
        public readonly bool Indexed;

        /// <summary>
        /// flag: less than one byte per sample (bit depth 1-2-4)
        /// </summary>
        public readonly bool Packed;

        /// <summary>
        /// Simple constructor: only for RGB/RGBA
        /// </summary>
        public ImageInfo(int cols, int rows, int bitdepth, bool alpha)
            : this(cols, rows, bitdepth, alpha, false, false)
        {
        }

        /// <summary>
        /// General Constructor
        /// </summary>
        /// <param name="cols">Width in pixels</param>
        /// <param name="rows">Height in pixels</param>
        /// <param name="bitdepth">Bits per sample per channel</param>
        /// <param name="alpha">Has alpha channel</param>
        /// <param name="grayscale">Is grayscale</param>
        /// <param name="palette">Has palette</param>
        public ImageInfo(int cols, int rows, int bitdepth, bool alpha, bool grayscale,
                bool palette)
        {
            Cols = cols;
            Rows = rows;
            Alpha = alpha;
            Indexed = palette;
            Greyscale = grayscale;
            if (Greyscale && palette)
            {
                throw new PngjException("palette and greyscale are exclusive");
            }

            if (grayscale || palette)
            {
                if (alpha)
                {
                    Channels = 2;
                }
                else
                {
                    Channels = 1;
                }
            }
            else if (alpha)
            {
                Channels = 4;
            }
            else
            {
                Channels = 3;
            }

            // http://www.w3.org/TR/PNG/#11IHDR
            BitDepth = bitdepth;
            Packed = bitdepth < 8;
            BitspPixel = (Channels * BitDepth);
            BytesPixel = (BitspPixel + 7) / 8;
            BytesPerRow = ((BitspPixel * cols) + 7) / 8;
            SamplesPerRow = Channels * Cols;
            SamplesPerRowPacked = (Packed) ? BytesPerRow : SamplesPerRow;
            // checks
            switch (BitDepth)
            {
                case 1:
                case 2:
                case 4:
                if (!(Indexed || Greyscale))
                {
                    throw new PngjException("only indexed or grayscale can have bitdepth="
                            + BitDepth.ToString(CultureInfo.CurrentCulture));
                }

                break;

                case 8:
                break;

                case 16:
                if (Indexed)
                {
                    throw new PngjException("indexed can't have bitdepth=" + BitDepth.ToString(CultureInfo.CurrentCulture));
                }

                break;

                default:
                throw new PngjException("invalid bitdepth=" + BitDepth.ToString(CultureInfo.CurrentCulture));
            }

            if (cols < 1 || cols > MAX_COLS_ROWS_VAL)
            {
                throw new PngjException("invalid cols=" + cols.ToString(CultureInfo.CurrentCulture) + " ???");
            }

            if (rows < 1 || rows > MAX_COLS_ROWS_VAL)
            {
                throw new PngjException("invalid rows=" + rows.ToString(CultureInfo.CurrentCulture) + " ???");
            }
        }

        /// <summary>
        /// General information, for debugging
        /// </summary>
        /// <returns>Summary</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "ImageInfo [cols={0}, rows={1}, bitDepth={2}, channels={3}, bitspPixel={4}, bytesPixel={5}, bytesPerRow={6}, samplesPerRow={7}, samplesPerRowP={8}, alpha={9}, greyscale={10}, indexed={11}, packed={12}]", Cols, Rows, BitDepth, Channels, BitspPixel, BytesPixel, BytesPerRow, SamplesPerRow, SamplesPerRowPacked, Alpha, Greyscale, Indexed, Packed);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            var result = 1;
            result = (prime * result) + ((Alpha) ? 1231 : 1237);
            result = (prime * result) + BitDepth;
            result = (prime * result) + Channels;
            result = (prime * result) + Cols;
            result = (prime * result) + ((Greyscale) ? 1231 : 1237);
            result = (prime * result) + ((Indexed) ? 1231 : 1237);
            result = (prime * result) + Rows;
            return result;
        }

        public override bool Equals(object obj)
        {
            if ((object)this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (GetType() != (object)obj.GetType())
            {
                return false;
            }

            var other = (ImageInfo)obj;
            if (Alpha != other.Alpha)
            {
                return false;
            }

            if (BitDepth != other.BitDepth)
            {
                return false;
            }

            if (Channels != other.Channels)
            {
                return false;
            }

            if (Cols != other.Cols)
            {
                return false;
            }

            if (Greyscale != other.Greyscale)
            {
                return false;
            }

            if (Indexed != other.Indexed)
            {
                return false;
            }

            if (Rows != other.Rows)
            {
                return false;
            }

            return true;
        }
    }
}