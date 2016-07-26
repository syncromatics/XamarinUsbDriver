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
        public override bool Cts { get; }
        public override bool Dsr { get; }
        public override bool Dtr { get; set; }
        public override bool Ri { get; }
        public override bool Rts { get; set; }

        private static int USB_READ_TIMEOUT_MILLIS = 1000;
        private static int USB_WRITE_TIMEOUT_MILLIS = 5000;

        private static int USB_RECIP_INTERFACE = 0x00;

        private static int PROLIFIC_VENDOR_READ_REQUEST = 0x01;
        private static int PROLIFIC_VENDOR_WRITE_REQUEST = 0x01;

        private static UsbAddressing PROLIFIC_VENDOR_OUT_REQTYPE = (UsbAddressing)(64);

        private static UsbAddressing PROLIFIC_VENDOR_IN_REQTYPE = (UsbAddressing)(192);

        private static UsbAddressing PROLIFIC_CTRL_OUT_REQTYPE = (UsbAddressing)(33);

        private const UsbAddressing WRITE_ENDPOINT = (UsbAddressing) 0x02;
        private const UsbAddressing READ_ENDPOINT = (UsbAddressing) 0x83;
        private const UsbAddressing INTERRUPT_ENDPOINT = (UsbAddressing) 0x81;

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

        private int mDeviceType = DEVICE_TYPE_HX;

        private UsbEndpoint mReadEndpoint;
        private UsbEndpoint mWriteEndpoint;
        private UsbEndpoint mInterruptEndpoint;

        private int mControlLinesValue = 0;

        private int mBaudRate = -1, mDataBits = -1, mStopBits = -1, mParity = -1;

        private int mStatus = 0;
        //private volatile Thread mReadStatusThread = null;
        //private Object mReadStatusThreadLock = new Object();
        //bool mStopReadStatusThread = false;
        //private IOException mReadStatusException = null;

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

                if (Device.DeviceClass == (UsbClass) 0x02)
                {
                    mDeviceType = DEVICE_TYPE_0;
                }
                else
                {
                    if ((Device.DeviceClass == 0x00)
                        || (Device.DeviceClass == (UsbClass) 0xff))
                    {
                        mDeviceType = DEVICE_TYPE_1;
                    }
                    else
                    {
                        mDeviceType = DEVICE_TYPE_HX;
                    }
                }

                SetControlLines(mControlLinesValue);

                DoBlackMagic();
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

        public override void Close()
        {
            if (Connection == null)
            {
                throw new IOException("Already closed");
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
            if ((mBaudRate == baudRate) && (mDataBits == (int) dataBits)
                && (mStopBits == (int) stopBits) && (mParity == (int) parity))
            {
                // Make sure no action is performed if there is nothing to change
                return;
            }

            byte[] lineRequestData = new byte[7];

            lineRequestData[0] = (byte) (baudRate & 0xff);
            lineRequestData[1] = (byte) ((baudRate >> 8) & 0xff);
            lineRequestData[2] = (byte) ((baudRate >> 16) & 0xff);
            lineRequestData[3] = (byte) ((baudRate >> 24) & 0xff);

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

            lineRequestData[6] = (byte) dataBits;

            CtrlOut(SET_LINE_REQUEST, 0, 0, lineRequestData);

            ResetDevice();

            mBaudRate = baudRate;
            mDataBits = (int) dataBits;
            mStopBits = (int) stopBits;
            mParity = (int) parity;
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

        private void DoBlackMagic()
        {
            var buffer = new byte[1];

            VendorIn(33924, 0, 1);

            VendorOut(1028, 0, null);

            VendorIn(33924, 0, 1);
            VendorIn(33667, 0, 1);
            VendorIn(33924, 0, 1);

            VendorOut(1028, 1, null);

            VendorIn(33924, 0, 1);
            VendorIn(33924, 0, 1);

            VendorOut(0, 1, null);
            VendorOut(1, 0, null);
            VendorOut(2, 68, null);
        }

        private void SetControlLines(int newControlLinesValue)
        {
            CtrlOut(SET_CONTROL_REQUEST, newControlLinesValue, 0, null);
            mControlLinesValue = newControlLinesValue;
        }
    }
}
