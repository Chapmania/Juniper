#if !NETSTANDARD
using System;
using System.IO;
using System.IO.Ports;

namespace Juniper.Serial
{
    public class SafeSerialPort : SerialPort, ISerialPort
    {
        public SafeSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
            : base(portName, baudRate, parity, dataBits, stopBits)
        {
        }

        public SafeSerialPort(string portName, int baudRate)
            : base(portName, baudRate)
        {
        }

        public SafeSerialPort()
            : base()
        {
        }

        new public void Dispose()
        {
            Dispose(true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "This fixes a long-standing issue with .NET's serial port implementation not releasing ports when the application crashes.")]
        new public void Open()
        {
            try
            {
                base.Open();
                theBaseStream = BaseStream;
                GC.SuppressFinalize(BaseStream);
            }
            catch
            {
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (base.Container != null))
            {
                base.Container.Dispose();
            }

            try
            {
                if (theBaseStream.CanRead)
                {
                    theBaseStream.Close();
                    GC.ReRegisterForFinalize(theBaseStream);
                }
            }
            catch
            {
                // ignore exception - bug with USB - serial adapters.
            }

            base.Dispose(disposing);
        }

        private Stream theBaseStream;
    }
}
#endif