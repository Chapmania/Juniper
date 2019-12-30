using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Juniper.Compression.Zip
{
    public class ZipArchiveEntryStream : Stream
    {
        private readonly ZipArchive zip;
        private readonly Stream entryStream;

        public ZipArchiveEntryStream(ZipArchive zip, ZipArchiveEntry entry)
        {
            this.zip = zip;
            entryStream = entry.Open();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                entryStream?.Dispose();
                zip?.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool CanRead
        {
            get { return entryStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return entryStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return entryStream.CanWrite; }
        }

        public override long Length
        {
            get { return entryStream.Length; }
        }

        public override long Position
        {
            get { return entryStream.Position; }

            set { entryStream.Position = value; }
        }

        public override void Flush()
        {
            entryStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return entryStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return entryStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            entryStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            entryStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return entryStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return entryStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override bool CanTimeout
        {
            get { return entryStream.CanTimeout; }
        }

        public override void Close()
        {
            entryStream.Close();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return entryStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return entryStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            entryStream.EndWrite(asyncResult);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return entryStream.FlushAsync(cancellationToken);
        }

        public override object InitializeLifetimeService()
        {
            return entryStream.InitializeLifetimeService();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return entryStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return entryStream.ReadByte();
        }

        public override int ReadTimeout
        {
            get { return entryStream.ReadTimeout; }

            set { entryStream.ReadTimeout = value; }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return entryStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            entryStream.WriteByte(value);
        }

        public override int WriteTimeout
        {
            get { return entryStream.WriteTimeout; }

            set { entryStream.WriteTimeout = value; }
        }
    }
}
