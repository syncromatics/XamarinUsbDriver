using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    public class CdcAcmSerialDriver : IUsbSerialDriver
    {
        public UsbDevice Device { get; }
        public List<IUsbSerialPort> Ports { get; }

        public CdcAcmSerialDriver(UsbDevice device)
        {
            Device = device;
            Ports = new List<IUsbSerialPort> {new CdcAcmSerialPort(device, 0)};
        }

        public static Dictionary<int, int[]> GetSupportedDevices()
        {
            return new Dictionary<int, int[]>
            {
                {
                    0x1cbe, new[] {0x9652}
                },
                {
                    0x2a19, new[] {0x800}
                }
            };
        }
    }

    public class CdcAcmSerialPort : CommonUsbSerialPort
    {
        private static int USB_RECIP_INTERFACE = 0x01;
        private static readonly int UsbRtAcm = UsbConstants.UsbTypeClass | USB_RECIP_INTERFACE;

        private static int SET_LINE_CODING = 0x20;  // USB CDC 1.1 section 6.2
        private static int GET_LINE_CODING = 0x21;
        private static int SET_CONTROL_LINE_STATE = 0x22;
        private static int SEND_BREAK = 0x23;

        public override bool Cd { get; }
        public override bool Cts { get; }
        public override bool Dsr { get; }
        public override bool Dtr { get; set; }
        public override bool Ri { get; }
        public override bool Rts { get; set; }

        private UsbDeviceConnection _connection;

        private UsbInterface _controlInterface;

        private UsbEndpoint _controlEndpoint;

        private UsbInterface _dataInterface;
        private UsbEndpoint _readEndpoint;
        private UsbEndpoint _writeEndpoint;

        public CdcAcmSerialPort(UsbDevice device, int interfaceNumber) : base(device, interfaceNumber)
        {
            
        }

        public override void Open(UsbDeviceConnection connection)
        {
            _connection = connection;

            _controlInterface = Device.GetInterface(0);
            if (!connection.ClaimInterface(_controlInterface, true))
            {
                throw new Exception("Could not claim control interface.");
            }

            _controlEndpoint = _controlInterface.GetEndpoint(0);

            _dataInterface = Device.GetInterface(1);
            if (!connection.ClaimInterface(_dataInterface, true))
            {
                throw new Exception("Could not claim data interface.");
            }

            _writeEndpoint = _dataInterface.GetEndpoint(0);
            _readEndpoint = _dataInterface.GetEndpoint(1);

            var isInit = Init();
            var buadSet = SetBaudrate(960000);
        }

        private UsbEndpoint GetEndpoint(UsbInterface interf, UsbAddressing direction)
        {
            for (var i = 0; i < interf.EndpointCount; i++)
            {
                var endpoint = interf.GetEndpoint(i);
                if (endpoint.Direction.HasFlag(direction))
                    return endpoint;
            }

            return null;
        }

        private bool Init()
        {
            if (_connection == null) return false;
            
            int ret = SendAcmControlMessage(SET_CONTROL_LINE_STATE, 0, null);

            return ret >= 0;
        }

        public override void Close()
        {
            _connection.Close();
        }

        public override int Read(byte[] dest, int timeoutMillis)
        {
            lock (ReadBufferLock)
            {
                var numBytesRead = _connection.BulkTransfer(_readEndpoint, dest, dest.Length, timeoutMillis);
                if (numBytesRead >= 0)
                    return numBytesRead;

                // This sucks: we get -1 on timeout, not 0 as preferred.
                // We *should* use UsbRequest, except it has a bug/api oversight
                // where there is no way to determine the number of bytes read
                // in response :\ -- http://b.android.com/28023
                if (timeoutMillis == int.MaxValue)
                {
                    // Hack: Special case "~infinite timeout" as an error.
                    return -1;
                }
                return 0;
            }
        }

        public override Task<int> ReadAsync(byte[] dest, int timeoutMillis)
        {
            throw new NotImplementedException();
        }

        public override int Write(byte[] src, int timeoutMillis)
        {
            int offset = 0;

            //while (offset < src.Length)
            {
                int writeLength;
                int amtWritten;

                lock(WriteBufferLock) {

                    writeLength = src.Length - offset;

                    amtWritten = _connection.BulkTransfer(_writeEndpoint, src, src.Length, timeoutMillis);
                }

                if (amtWritten <= 0)
                {
                    throw new Exception("Error writing " + writeLength
                            + " bytes at offset " + offset + " length=" + src.Length);
                }


                offset += amtWritten;
            }

            return offset;
        }

        public override Task<int> WriteAsync(byte[] src, int timeoutMillis)
        {
            throw new NotImplementedException();
        }

        private bool SetBaudrate(int baudrate)
        {
            byte[] baudByte = new byte[4];

            baudByte[0] = (byte)(baudrate & 0x000000FF);
            baudByte[1] = (byte)((baudrate & 0x0000FF00) >> 8);
            baudByte[2] = (byte)((baudrate & 0x00FF0000) >> 16);
            baudByte[3] = (byte)((baudrate & 0xFF000000) >> 24);
            
            int ret = SendAcmControlMessage(0x20, 0, new byte[] {
                baudByte[0], baudByte[1], baudByte[2], baudByte[3], 0x00, 0x00,
                0x08});

            if (ret < 0)
            {
                return false;
            }

            return true;
        }

        public override void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity)
        {
            byte stopBitsByte;
            switch (stopBits)
            {
                case StopBits._1: stopBitsByte = 0; break;
                case StopBits._1_5: stopBitsByte = 1; break;
                case StopBits._2: stopBitsByte = 2; break;
                default: throw new Exception("Bad value for stopBits: " + stopBits);
            }

            byte parityBitesByte;
            switch (parity)
            {
                case Parity.None: parityBitesByte = 0; break;
                case Parity.Odd: parityBitesByte = 1; break;
                case Parity.Even: parityBitesByte = 2; break;
                case Parity.Mark: parityBitesByte = 3; break;
                case Parity.Space: parityBitesByte = 4; break;
                default: throw new Exception("Bad value for parity: " + parity);
            }

            byte[] msg = {
                    (byte) ( baudRate & 0xff),
                    (byte) ((baudRate >> 8 ) & 0xff),
                    (byte) ((baudRate >> 16) & 0xff),
                    (byte) ((baudRate >> 24) & 0xff),
                    stopBitsByte,
                    parityBitesByte,
                    (byte) dataBits};

            var response = SendAcmControlMessage(SET_LINE_CODING, 0, msg);
        }

        private int SendAcmControlMessage(int request, int value, byte[] buf) => 
            _connection.ControlTransfer((UsbAddressing)UsbRtAcm, request, value, 1, buf, buf?.Length ?? 0, 5000);
    }
}
