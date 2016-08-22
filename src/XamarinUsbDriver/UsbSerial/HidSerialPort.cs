using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    public class HidSerialPort : CommonUsbSerialPort
    {
        public override bool Cd { get; }
        public override bool Cts { get; }
        public override bool Dsr { get; }
        public override bool Dtr { get; set; }
        public override bool Ri { get; }
        public override bool Rts { get; set; }

        private readonly UsbDevice _device;

        private UsbDeviceConnection _connection;

        private UsbEndpoint _writeEndpoint;
        private UsbEndpoint _readEndpoint;

        private byte[] _buffer;

        public HidSerialPort(UsbDevice device, int portNumber) : base(device, portNumber)
        {
            _device = device;
        }

        public override void Open(UsbDeviceConnection connection)
        {
            _connection = connection;

            var intf = _device.GetInterface(0);
            if (!connection.ClaimInterface(intf, true))
            {
                throw new Exception("Could not claim control interface.");
            }

            if (intf.EndpointCount > 1)
            {
                _writeEndpoint = intf.GetEndpoint(1);
            }

            _readEndpoint = intf.GetEndpoint(0);
            _buffer = new byte[_readEndpoint.MaxPacketSize];
        }

        public override void Close()
        {
            _connection.Close();
        }

        public override int Read(byte[] dest, int timeoutMillis)
        {
            var totalRead = 0;
            var watch = new Stopwatch();
            watch.Start();

            while (totalRead < dest.Length)
            {
                var max = Math.Min(_buffer.Length, dest.Length - totalRead);

                var read = _connection.BulkTransfer(_readEndpoint, _buffer, max, timeoutMillis);
                if (read == -1)
                    return totalRead;

                if (read > 0)
                {
                    Buffer.BlockCopy(_buffer, 0, dest, totalRead, read);
                    totalRead += read;
                }
                else if(watch.ElapsedMilliseconds > timeoutMillis)
                {
                    return totalRead;
                }
            }

            return totalRead;
        }

        public override Task<int> ReadAsync(byte[] dest, int timeoutMillis)
        {
            throw new NotImplementedException();
        }

        public override int Write(byte[] src, int timeoutMillis)
        {
            if (_writeEndpoint == null)
            {
                throw new Exception("Device does not have a write endpoint.");
            }

            int amtWritten;

            lock (WriteBufferLock)
            {
                amtWritten = _connection.BulkTransfer(_writeEndpoint, src, src.Length, timeoutMillis);
            }

            return amtWritten;
        }

        public override Task<int> WriteAsync(byte[] src, int timeoutMillis)
        {
            throw new NotImplementedException();
        }

        public override void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity)
        {
        }
    }
}
