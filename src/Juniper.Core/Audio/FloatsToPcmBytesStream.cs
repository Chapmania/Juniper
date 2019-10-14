using System.IO;

namespace Juniper.Audio
{
    public class FloatsToPcmBytesStream : AbstractPcmConversionStream
    {
        /// <summary>
        /// A small buffer for reading in a single sample at a time.
        /// </summary>
        private byte[] tempBuffer;

        /// <summary>
        /// Creates a wrapper around a stream to convert floating point samples
        /// to PCM data.
        /// </summary>
        /// <param name="sourceStream">The floating point data stream</param>
        /// <param name="bytesPerFloat">The number of bytes per sample (1, 2, 3, or 4)</param>
        public FloatsToPcmBytesStream(Stream sourceStream, int bytesPerFloat)
            : base(sourceStream, bytesPerFloat)
        {
            tempBuffer = new byte[sizeof(float)];
        }

        /// <summary>
        /// The length of the stream, after conversion.
        /// </summary>
        /// <remarks>The length of the underlying stream will be divided if
        /// the bytesPerFloat value is smaller than 4. For example, a 2-byte-per-float
        /// PCM stream of length 10 will show a length of 5, as there are 2 times
        /// as many bytes in the input floating-point sample stream.</remarks>
        public override long Length
        {
            get
            {
                return ToPCMSpace(sourceStream.Length);
            }
        }


        /// <summary>
        /// Gets or sets the position of the stream read head relative to the
        /// floating-point input length.
        /// </summary>
        public override long Position
        {
            get
            {
                return ToPCMSpace(sourceStream.Position);
            }

            set
            {
                sourceStream.Position = ToFloatSpace(value);
            }
        }

        /// <summary>
        /// Sets the length of the underlying stream according to the converted
        /// length of float bytes.
        /// </summary>
        /// <param name="value"></param>
        protected override void InternalSetLength(long value)
        {
            sourceStream.SetLength(ToFloatSpace(value));
        }

        /// <summary>
        /// Reads 4 floating-point bytes at a time, outputting <see cref="AbstractPcmConversionStream.bytesPerFloat"/>
        /// PCM bytes per sample read.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected override int InternalRead(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count && sourceStream.Position < sourceStream.Length)
            {
                sourceStream.Read(tempBuffer, 0, sizeof(float));
                FloatToPCM(tempBuffer, 0, buffer, offset + read);
                read += bytesPerFloat;
            }

            return read;
        }

        /// <summary>
        /// Writes 4 floating point bytes at a time, inputting from <see cref="AbstractPcmConversionStream.bytesPerFloat"/>
        /// bytes per PCM sample write.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        protected override void InternalWrite(byte[] buffer, int offset, int count)
        {
            int wrote = 0;
            while(wrote < count)
            {
                PCMToFloat(buffer, offset + wrote, tempBuffer, 0);
                sourceStream.Write(tempBuffer, 0, sizeof(float));
                wrote += bytesPerFloat;
            }
        }
    }
}
