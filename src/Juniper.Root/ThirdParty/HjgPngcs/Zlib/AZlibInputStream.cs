using System;
using System.IO;

namespace Hjg.Pngcs.Zlib
{
    public abstract class AZlibInputStream : Stream
    {
        protected readonly Stream rawStream;
        protected readonly bool leaveOpen;

        protected AZlibInputStream(Stream st, bool leaveOpen)
        {
            rawStream = st;
            this.leaveOpen = leaveOpen;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !leaveOpen)
            {
                rawStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <summary>
        /// mainly for debugging
        /// </summary>
        /// <returns></returns>
        public abstract string GetImplementationId();
    }
}