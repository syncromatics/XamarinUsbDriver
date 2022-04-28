using System;
using System.IO;
using System.Threading.Tasks;
using Android.Hardware.Usb;
using Java.Lang;
using Math = System.Math;

namespace XamarinUsbDriver.UsbSerial
{
    public class ProlificSerialPort : CommonUsbSerialPort
    {
        public override bool Cd { get; }
        public override bool Cts { get; set; }
        public override bool Dsr { get; }
        public override bool Dtr { get; set; }
        public override bool Ri { get; }
        public override bool Rts { get; set; }

        private static int USB_READ_TIMEOUT_MILLIS = 1000;
        private static int USB_WRITE_TIMEOUT_MILLIS = 5000;

        private static int USB_RECIP_INTERFACE = 0x00;

        private static int PROLIFIC_VENDOR_READ_REQUEST = 0x01;
        private static int PROLIFIC_VENDOR_WRITE_REQUEST = 0x01;

        private static int PL2303_READ_TYPE_HX_STATUS = 0x8080;

        private static UsbAddressing PROLIFIC_VENDOR_OUT_REQTYPE = (UsbAddressing)(64);

        private static UsbAddressing PROLIFIC_VENDOR_IN_REQTYPE = (UsbAddressing)(192);

        private static UsbAddressing PROLIFIC_CTRL_OUT_REQTYPE = (UsbAddressing)(33);

        private const UsbAddressing WRITE_ENDPOINT = (UsbAddressing)0x02;
        private const UsbAddressing READ_ENDPOINT = (UsbAddressing)0x83;
        private const UsbAddressing INTERRUPT_ENDPOINT = (UsbAddressing)0x81;

        private static int FLUSH_RX_REQUEST = 0x08;
        private static int FLUSH_TX_REQUEST = 0x09;

        private static int SET_LINE_REQUEST = 0x20;
        private static int SET_CONTROL_REQUEST = 0x22;

        private static int CONTROL_DTR = 0x01;
        private static int CONTROL_RTS = 0x02;

        private static int STATUS_FLAG_CD = 0x01;
        private static int STATUS_FLAG_DSR = 0x02;
        private static int STATUS_FLAG_RI = 0x08;
        private static int STATUS_FLAG_CTS = 0x80;

        private static int STATUS_BUFFER_SIZE = 10;
        private static int STATUS_BYTE_IDX = 8;

        private static int DEVICE_TYPE_HX = 0;
        private static int DEVICE_TYPE_0 = 1;
        private static int DEVICE_TYPE_1 = 2;

        /// <summary>
        /// Types of PL2303 protocols
        /// </summary>
        /// <remarks>
        /// Modeled after Linux driver https://github.com/torvalds/linux/blob/8f4dd16603ce834d1c5c4da67803ea82dd282511/drivers/usb/serial/pl2303.c#L177-L185
        /// </remarks>
        private enum PL2303Type
        {
            TYPE_H,
            TYPE_HX,
            TYPE_TA,
            TYPE_TB,
            TYPE_HXD,
            TYPE_HXN,
            TYPE_COUNT
        }

        /// <summary>
        /// USB_DT_DEVICE: Device descriptor
        /// </summary>
        /// <remarks>
        /// Modeled after Linux USB device descriptor: https://github.com/torvalds/linux/blob/8f4dd16603ce834d1c5c4da67803ea82dd282511/include/uapi/linux/usb/ch9.h#L288-L305
        /// </remarks>
        struct UsbDeviceDescriptor
        {
            public UsbDeviceDescriptor(byte[] descriptors)
            {
                if (descriptors.Length < 18)
                    throw new ArgumentOutOfRangeException(nameof(descriptors));

                bLength = descriptors[0];
                bDescriptorType = descriptors[1];

                bcdUSB = (ushort)(descriptors[2] | (descriptors[3] << 8));
                bDeviceClass = descriptors[4];
                bDeviceSubClass = descriptors[5];
                bDeviceProtocol = descriptors[6];
                bMaxPacketSize0 = descriptors[7];
                idVendor = (ushort)(descriptors[8] | (descriptors[9] << 8));
                idProduct = (ushort)(descriptors[10] | (descriptors[11] << 8));
                bcdDevice = (ushort)(descriptors[12] | (descriptors[13] << 8));
                iManufacturer = descriptors[14];
                iProduct = descriptors[15];
                iSerialNumber = descriptors[16];
                bNumConfigurations = descriptors[17];
            }

            public readonly byte bLength;
            public readonly byte bDescriptorType;

            public readonly ushort bcdUSB;
            public readonly byte bDeviceClass;
            public readonly byte bDeviceSubClass;
            public readonly byte bDeviceProtocol;
            public readonly byte bMaxPacketSize0;
            public readonly ushort idVendor;
            public readonly ushort idProduct;
            public readonly ushort bcdDevice;
            public readonly byte iManufacturer;
            public readonly byte iProduct;
            public readonly byte iSerialNumber;
            public readonly byte bNumConfigurations;
        }

        private UsbEndpoint mReadEndpoint;
        private UsbEndpoint mWriteEndpoint;
        private UsbEndpoint mInterruptEndpoint;

        private int mControlLinesValue = 0;

        private int mBaudRate = -1, mDataBits = -1, mStopBits = -1, mParity = -1;

        public ProlificSerialPort(UsbDevice device, int portNumber) : base(device, portNumber)
        {

        }

        public override void Open(UsbDeviceConnection connection)
        {
            if (Connection != null)
            {
                throw new IOException("Already open");
            }

            Connection = connection;

            var type = pl2303_detect_type();

            UsbInterface usbInterface = Device.GetInterface(0);

            if (!connection.ClaimInterface(usbInterface, true))
            {
                throw new IOException("Error claiming Prolific interface 0");
            }

            var opened = false;
            try
            {
                for (int i = 0; i < usbInterface.EndpointCount; ++i)
                {
                    UsbEndpoint currentEndpoint = usbInterface.GetEndpoint(i);

                    switch (currentEndpoint.Address)
                    {
                        case READ_ENDPOINT:
                            mReadEndpoint = currentEndpoint;
                            break;

                        case WRITE_ENDPOINT:
                            mWriteEndpoint = currentEndpoint;
                            break;

                        case INTERRUPT_ENDPOINT:
                            mInterruptEndpoint = currentEndpoint;
                            break;
                    }
                }

                SetControlLines(mControlLinesValue);

                if (type != PL2303Type.TYPE_HXN)
                {
                    VendorIn(0x8484, 0, 1);

                    VendorOut(0x0404, 0, null);

                    VendorIn(0x8484, 0, 1);
                    VendorIn(0x8383, 0, 1);
                    VendorIn(0x8484, 0, 1);

                    VendorOut(0x0404, 1, null);

                    VendorIn(0x8484, 0, 1);
                    VendorIn(0x8383, 0, 1);

                    VendorOut(0, 1, null);
                    VendorOut(1, 0, null);
                    if (type == PL2303Type.TYPE_H)
                    {
                        VendorOut(2, 0x24, null);
                    }
                    else
                    {
                        VendorOut(2, 0x44, null);
                    }
                }
                ResetDevice();
                opened = true;
            }
            finally
            {
                if (!opened)
                {
                    Connection = null;
                    connection.ReleaseInterface(usbInterface);
                }
            }
        }

        /// <summary>
        /// Detect the type of protocol to use based on the device
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Based on the Linux driver details: https://github.com/torvalds/linux/blob/8f4dd16603ce834d1c5c4da67803ea82dd282511/drivers/usb/serial/pl2303.c#L401
        /// </remarks>
        private PL2303Type pl2303_detect_type()
        {
            var desc = new UsbDeviceDescriptor(Connection.GetRawDescriptors());

            /*
             * Legacy PL2303H, variants 0 and 1 (difference unknown).
             */
            if (desc.bDeviceClass == 0x02)
                return PL2303Type.TYPE_H; /* variant 0 */

            if (desc.bMaxPacketSize0 != 0x40)
            {
                if (desc.bDeviceClass == 0x00 || desc.bDeviceClass == 0xff)
                    return PL2303Type.TYPE_H;  /* variant 1 */

                return PL2303Type.TYPE_H;      /* variant 0 */
            }

            switch (desc.bcdUSB)
            {
                case 0x110:
                    switch (desc.bcdDevice)
                    {
                        case 0x300:
                            return PL2303Type.TYPE_HX;
                        case 0x400:
                            return PL2303Type.TYPE_HXD;
                        default:
                            return PL2303Type.TYPE_HX;
                    }
                case 0x200:
                    switch (desc.bcdDevice)
                    {
                        case 0x100:
                        case 0x105:
                        case 0x305:
                        case 0x405:
                        case 0x605:
                            /*
                             * Assume it's an HXN-type if the device doesn't
                             * support the old read request value.
                             */
                            if (!pl2303_supports_hx_status())
                                return PL2303Type.TYPE_HXN;
                            break;
                        case 0x300:
                            return PL2303Type.TYPE_TA;
                        case 0x500:
                            return PL2303Type.TYPE_TB;
                    }
                    break;
            }

            throw new InvalidOperationException($"failed to determine type of protocol supported by this device (bDeviceClass {desc.bDeviceClass}, bMaxPacketSize0 {desc.bMaxPacketSize0}, bcdUSB {desc.bcdUSB}, bcdDevice {desc.bcdDevice})");
        }

        private bool pl2303_supports_hx_status()
        {
            try
            {
                VendorIn(PL2303_READ_TYPE_HX_STATUS, 0, 1);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public override void Close()
        {
            if (Connection == null)
            {
                return;
            }
            try
            {
                ResetDevice();
            }
            finally
            {
                try
                {
                    Connection.ReleaseInterface(Device.GetInterface(0));
                }
                finally
                {
                    Connection = null;
                }
            }
        }

        public override int Read(byte[] dest, int timeoutMillis)
        {
            lock (ReadBufferLock)
            {
                int readAmt = Math.Min(dest.Length, ReadBuffer.Length);
                int numBytesRead = Connection.BulkTransfer(mReadEndpoint, ReadBuffer, readAmt, timeoutMillis);
                if (numBytesRead < 0)
                {
                    return 0;
                }

                Buffer.BlockCopy(ReadBuffer, 0, dest, 0, numBytesRead);

                return numBytesRead;
            }
        }

        public override Task<int> ReadAsync(byte[] dest, int timeoutMillis)
        {
            throw new System.NotImplementedException();
        }

        public override int Write(byte[] src, int timeoutMillis)
        {
            int offset = 0;

            while (offset < src.Length)
            {
                int amtWritten;

                lock (WriteBufferLock)
                {
                    byte[] writeBuffer;

                    var writeLength = Math.Min(src.Length - offset, WriteBuffer.Length);
                    if (offset == 0)
                    {
                        writeBuffer = src;
                    }
                    else
                    {
                        // bulkTransfer does not support offsets, make a copy.
                        Buffer.BlockCopy(src, offset, WriteBuffer, 0, writeLength);
                        writeBuffer = WriteBuffer;
                    }

                    amtWritten = Connection.BulkTransfer(mWriteEndpoint, writeBuffer, writeLength, timeoutMillis);
                }

                if (amtWritten <= 0)
                {
                    return amtWritten;
                }

                offset += amtWritten;
            }
            return offset;
        }

        public override Task<int> WriteAsync(byte[] src, int timeoutMillis)
        {
            throw new System.NotImplementedException();
        }

        public override void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity)
        {
            if ((mBaudRate == baudRate) && (mDataBits == (int)dataBits)
                && (mStopBits == (int)stopBits) && (mParity == (int)parity))
            {
                // Make sure no action is performed if there is nothing to change
                return;
            }

            byte[] lineRequestData = new byte[7];

            lineRequestData[0] = (byte)(baudRate & 0xff);
            lineRequestData[1] = (byte)((baudRate >> 8) & 0xff);
            lineRequestData[2] = (byte)((baudRate >> 16) & 0xff);
            lineRequestData[3] = (byte)((baudRate >> 24) & 0xff);

            switch (stopBits)
            {
                case StopBits._1:
                    lineRequestData[4] = 0;
                    break;

                case StopBits._1_5:
                    lineRequestData[4] = 1;
                    break;

                case StopBits._2:
                    lineRequestData[4] = 2;
                    break;

                default:
                    throw new IllegalArgumentException("Unknown stopBits value: " + stopBits);
            }

            switch (parity)
            {
                case Parity.None:
                    lineRequestData[5] = 0;
                    break;

                case Parity.Odd:
                    lineRequestData[5] = 1;
                    break;

                case Parity.Even:
                    lineRequestData[5] = 2;
                    break;

                case Parity.Mark:
                    lineRequestData[5] = 3;
                    break;

                case Parity.Space:
                    lineRequestData[5] = 4;
                    break;

                default:
                    throw new IllegalArgumentException("Unknown parity value: " + parity);
            }

            lineRequestData[6] = (byte)dataBits;

            CtrlOut(SET_LINE_REQUEST, 0, 0, lineRequestData);

            ResetDevice();

            mBaudRate = baudRate;
            mDataBits = (int)dataBits;
            mStopBits = (int)stopBits;
            mParity = (int)parity;
        }

        private byte[] InControlTransfer(UsbAddressing requestType, int request, int value, int index, int length)
        {
            byte[] buffer = new byte[length];
            int result = Connection.ControlTransfer(requestType, request, value,
                index, buffer, length, USB_READ_TIMEOUT_MILLIS);

            if (result != length)
            {
                throw new IOException($"ControlTransfer with value {value} failed: {result}");
            }
            return buffer;
        }

        private void OutControlTransfer(UsbAddressing requestType, int request,
            int value, int index, byte[] data)
        {
            int length = data?.Length ?? 0;
            int result = Connection.ControlTransfer(requestType, request, value,
                index, data, length, USB_WRITE_TIMEOUT_MILLIS);

            if (result != length)
            {
                throw new IOException($"ControlTransfer with value {value} failed: {result}");
            }
        }

        private byte[] VendorIn(int value, int index, int length)
        {
            return InControlTransfer(PROLIFIC_VENDOR_IN_REQTYPE,
                PROLIFIC_VENDOR_READ_REQUEST, value, index, length);
        }

        private void VendorOut(int value, int index, byte[] data)
        {
            OutControlTransfer(PROLIFIC_VENDOR_OUT_REQTYPE,
                PROLIFIC_VENDOR_WRITE_REQUEST, value, index, data);
        }

        private void ResetDevice()
        {
            PurgeHwBuffers(true, true);
        }

        private void CtrlOut(int request, int value, int index, byte[] data)
        {
            OutControlTransfer(PROLIFIC_CTRL_OUT_REQTYPE, request, value, index,
                data);
        }

        private void SetControlLines(int newControlLinesValue)
        {
            CtrlOut(SET_CONTROL_REQUEST, newControlLinesValue, 0, null);
            mControlLinesValue = newControlLinesValue;
        }

        public override event EventHandler<bool> CtsChanged;
    }
}
