/* Copyright 2011-2013 Google Inc.
 * Copyright 2013 mike wakerly <opensource@hoho.com>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301,
 * USA.
 *
 * Project home page: https://github.com/mik3y/usb-serial-for-android
 *
 * This driver is based on http://www.intra2net.com/en/developer/libftdi, and is
 * copyright and subject to the following terms:
 *
 *   Copyright (C) 2003 by Intra2net AG
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License
 *   version 2.1 as published by the Free Software Foundation;
 *
 *   opensource@intra2net.com
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Android.Hardware.Usb;
using Android.Util;
using Java.Lang;
using Java.Nio;
using Buffer = System.Buffer;
using Math = System.Math;

namespace XamarinUsbDriver.UsbSerial
{
    internal class FtdiSerialPort : CommonUsbSerialPort
    {

        public static int USB_TYPE_STANDARD = 0x00 << 5;
        public static int USB_TYPE_CLASS = 0x00 << 5;
        public static int USB_TYPE_VENDOR = 0x00 << 5;
        public static int USB_TYPE_RESERVED = 0x00 << 5;

        public static int USB_RECIP_DEVICE = 0x00;
        public static int USB_RECIP_INTERFACE = 0x01;
        public static int USB_RECIP_ENDPOINT = 0x02;
        public static int USB_RECIP_OTHER = 0x03;

        public static int USB_ENDPOINT_IN = 0x80;
        public static int USB_ENDPOINT_OUT = 0x00;

        public static int USB_WRITE_TIMEOUT_MILLIS = 5000;
        public static int USB_READ_TIMEOUT_MILLIS = 5000;

        //    // From ftdi.h
        //    /**
        //     * Reset the port.
        //     */
        private static int _sioResetRequest = 0;

        //    /**
        //     * Set the modem control register.
        //     */
        private static int _sioModemCtrlRequest = 1;

        //    /**
        //     * Set flow control register.
        //     */
        private static int SIO_SET_FLOW_CTRL_REQUEST = 2;

        private static int SIO_SET_LATENCY_TIMER_REQUEST = 9;

        //    /**
        //     * Set baud rate.
        //     */
        private const int SioSetBaudRateRequest = 3;

        //    /**
        //     * Set the data characteristics of the port.
        //     */
        private const int SioSetDataRequest = 4;

        private const int SioResetSio = 0;
        private const int SioResetPurgeRx = 1;
        private const int SioResetPurgeTx = 2;

        public static readonly UsbAddressing FtdiDeviceOutReqtype =
            (UsbAddressing)(UsbConstants.UsbTypeVendor | USB_RECIP_DEVICE | USB_ENDPOINT_OUT);

        public static int FTDI_DEVICE_IN_REQTYPE = UsbConstants.UsbTypeVendor | USB_RECIP_DEVICE | USB_ENDPOINT_IN;

        private const int ModemStatusHeaderLength = 2;

        private string TAG = typeof(FtdiSerialDriver).Name;

        private DeviceType _type;

        private readonly FtdiSerialDriver _driver;

        public FtdiSerialPort(UsbDevice device, int portNumber, FtdiSerialDriver driver) : base(device, portNumber)
        {
            _driver = driver;
        }

        /// <summary>
        /// Filter FTDI status bytes from buffer
        /// </summary>
        /// <param name="src">The source buffer (which contains status bytes)</param>
        /// <param name="dest">The destination buffer to write the status bytes into (can be src)</param>
        /// <param name="totalBytesRead">Number of bytes read to src</param>
        /// <param name="maxPacketSize">The USB endpoint max packet size</param>
        /// <returns>The number of payload bytes</returns>
        private int filterStatusBytes(byte[] src, byte[] dest, int totalBytesRead, int maxPacketSize)
        {
            int packetsCount = totalBytesRead / maxPacketSize + (totalBytesRead % maxPacketSize == 0 ? 0 : 1);
            for (int packetIdx = 0; packetIdx < packetsCount; ++packetIdx)
            {
                int count = (packetIdx == (packetsCount - 1))
                    ? (totalBytesRead % maxPacketSize) - ModemStatusHeaderLength
                    : maxPacketSize - ModemStatusHeaderLength;
                if (count > 0)
                {
                    Buffer.BlockCopy(src,
                        packetIdx * maxPacketSize + ModemStatusHeaderLength,
                        dest,
                        packetIdx * (maxPacketSize - ModemStatusHeaderLength),
                        count);
                }
            }

            return totalBytesRead - (packetsCount * 2);
        }

        //    /**
        //     * Filter FTDI status bytes from buffer
        //     * @param src The source buffer (which contains status bytes)
        //     * @param dest The destination buffer to write the status bytes into (can be src)
        //     * @param totalBytesRead Number of bytes read to src
        //     * @param maxPacketSize The USB endpoint max packet size
        //     * @return The number of payload bytes
        //     */

        public void Reset()
        {
            int result = Connection.ControlTransfer(FtdiDeviceOutReqtype, _sioResetRequest,
                SioResetSio, PortNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Reset failed: result=" + result);
            }

            // TODO(mikey): autodetect.
            _type = DeviceType.TYPE_R;
        }

        public override void Open(UsbDeviceConnection connection)
        {
            if (Connection != null)
            {
                throw new IOException("Already open");
            }
            Connection = connection;

            bool opened = false;
            try
            {
                if (connection.ClaimInterface(Device.GetInterface(PortNumber), true))
                {
                    Log.Debug(TAG, "claimInterface " + PortNumber + " SUCCESS");
                }
                else
                {
                    throw new IOException("Error claiming interface " + PortNumber);
                }
                Reset();
                opened = true;
            }
            finally
            {
                if (!opened)
                {
                    Close();
                    Connection = null;
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
                Connection.Close();
            }
            finally
            {
                Connection = null;
            }
        }

        public override int Read(byte[] dest, int timeoutMillis)
        {
            int result = Connection.ControlTransfer(
                FtdiDeviceOutReqtype, 
                SIO_SET_LATENCY_TIMER_REQUEST, 
                255, 
                PortNumber + 1, 
                null, 
                0, 
                USB_WRITE_TIMEOUT_MILLIS);

            UsbEndpoint endpoint = Device.GetInterface(PortNumber).GetEndpoint(0);
            var direction = endpoint.Direction == UsbAddressing.In;
            lock (ReadBufferLock)
            {
                int readAmt = Math.Min(dest.Length, ReadBuffer.Length);

                var totalBytesRead = Connection.BulkTransfer(endpoint, ReadBuffer, readAmt, timeoutMillis);

                if (totalBytesRead < ModemStatusHeaderLength)
                {
                    throw new IOException("Expected at least " + ModemStatusHeaderLength + " bytes");
                }

                return filterStatusBytes(ReadBuffer, dest, totalBytesRead, endpoint.MaxPacketSize);
            }
        }

        public override async Task<int> ReadAsync(byte[] dest, int timeoutMillis)
        {
            UsbEndpoint endpoint = Device.GetInterface(PortNumber).GetEndpoint(0);

            int readAmt;
            lock (ReadBufferLock)
            {
                // mReadBuffer is only used for maximum read size.
                readAmt = Math.Min(dest.Length, ReadBuffer.Length);
            }

            UsbRequest request = new UsbRequest();
            request.Initialize(Connection, endpoint);

            ByteBuffer buf = ByteBuffer.Wrap(dest);
            if (!request.Queue(buf, readAmt))
            {
                throw new IOException("Error queueing request.");
            }

            UsbRequest response = Connection.RequestWait();
            if (response == null)
            {
                throw new IOException("Null response");
            }

            int payloadBytesRead = buf.Position() - ModemStatusHeaderLength;
            if (payloadBytesRead <= 0)
                return 0;

            Log.Debug(TAG, BitConverter.ToString(dest, 0, Math.Min(32, dest.Length)));

            return filterStatusBytes(dest, dest, buf.Position(), endpoint.MaxPacketSize);
        }

        public override int Write(byte[] src, int timeoutMillis)
        {
            UsbEndpoint endpoint = Device.GetInterface(PortNumber).GetEndpoint(1);
            int offset = 0;
            using (WriteBufferLock.Lock())
            {
                while (offset < src.Length)
                {
                    var writeLength = Math.Min(src.Length - offset, WriteBuffer.Length);

                    var amtWritten = Connection.BulkTransfer(endpoint, src, offset, writeLength,
                        timeoutMillis);

                    if (amtWritten <= 0)
                    {
                        throw new IOException(
                            $"Error writing {writeLength} bytes at offset {offset} length={src.Length}");
                    }

                    Log.Debug(TAG, $"Wrote amtWritten={amtWritten} attempted={writeLength}");
                    offset += amtWritten;
                }
            }
            return offset;
        }

        public override async Task<int> WriteAsync(byte[] src, int timeoutMillis)
        {
            UsbEndpoint endpoint = Device.GetInterface(PortNumber).GetEndpoint(1);
            int offset = 0;

            using (await WriteBufferLock.LockAsync())
            {
                while (offset < src.Length)
                {
                    var writeLength = Math.Min(src.Length - offset, WriteBuffer.Length);

                    var amtWritten = await Connection.BulkTransferAsync(endpoint, src, offset, writeLength,
                        timeoutMillis);

                    if (amtWritten <= 0)
                    {
                        throw new IOException(
                            $"Error writing {writeLength} bytes at offset {offset} length={src.Length}");
                    }

                    Log.Debug(TAG, $"Wrote amtWritten={amtWritten} attempted={writeLength}");
                    offset += amtWritten;
                }

            }
            return offset;
        }

        private int SetBaudRate(int baudRate)
        {
            long[] vals = ConvertBaudrate(baudRate);
            long actualBaudrate = vals[0];
            long value = vals[2];

            int result = Connection.ControlTransfer(FtdiDeviceOutReqtype,
                SioSetBaudRateRequest, (int)value, PortNumber + 1,
                null, 0, USB_WRITE_TIMEOUT_MILLIS);

            if (result != 0)
            {
                throw new IOException($"Setting baudrate failed: result={result}");
            }
            return (int)actualBaudrate;
        }

        public override void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity)
        {
            SetBaudRate(baudRate);

            int config = (int)dataBits;

            switch (parity)
            {
                case Parity.None:
                    config |= (0x00 << 8);
                    break;
                case Parity.Odd:
                    config |= (0x01 << 8);
                    break;
                case Parity.Even:
                    config |= (0x02 << 8);
                    break;
                case Parity.Mark:
                    config |= (0x03 << 8);
                    break;
                case Parity.Space:
                    config |= (0x04 << 8);
                    break;
                default:
                    throw new IllegalArgumentException("Unknown parity value: " + parity);
            }

            switch (stopBits)
            {
                case StopBits._1:
                    config |= (0x00 << 11);
                    break;
                case StopBits._1_5:
                    config |= (0x01 << 11);
                    break;
                case StopBits._2:
                    config |= (0x02 << 11);
                    break;
                default:
                    throw new IllegalArgumentException("Unknown stopBits value: " + stopBits);
            }

            int result = Connection.ControlTransfer(FtdiDeviceOutReqtype,
                SioSetDataRequest, config, PortNumber + 1,
                null, 0, USB_WRITE_TIMEOUT_MILLIS);

            if (result != 0)
            {
                throw new IOException("Setting parameters failed: result=" + result);
            }
        }

        private long[] ConvertBaudrate(int baudrate)
        {
            // TODO(mikey): Braindead transcription of libfti method.  Clean up,
            // using more idiomatic Java where possible.
            int divisor = 24000000 / baudrate;
            int bestDivisor = 0;
            int bestBaud = 0;
            int bestBaudDiff = 0;
            int[] fracCode = { 0, 3, 2, 4, 1, 5, 6, 7 };

            for (int i = 0; i < 2; i++)
            {
                int tryDivisor = divisor + i;
                int baudDiff;

                if (tryDivisor <= 8)
                {
                    // Round up to minimum supported divisor
                    tryDivisor = 8;
                }
                else if (_type != DeviceType.TYPE_AM && tryDivisor < 12)
                {
                    // BM doesn't support divisors 9 through 11 inclusive
                    tryDivisor = 12;
                }
                else if (divisor < 16)
                {
                    // AM doesn't support divisors 9 through 15 inclusive
                    tryDivisor = 16;
                }
                else
                {
                    if (_type == DeviceType.TYPE_AM)
                    {
                        // TODO
                    }
                    else
                    {
                        if (tryDivisor > 0x1FFFF)
                        {
                            // Round down to maximum supported divisor value (for
                            // BM)
                            tryDivisor = 0x1FFFF;
                        }
                    }
                }

                // Get estimated baud rate (to nearest integer)
                var baudEstimate = (24000000 + (tryDivisor / 2)) / tryDivisor;

                // Get absolute difference from requested baud rate
                if (baudEstimate < baudrate)
                {
                    baudDiff = baudrate - baudEstimate;
                }
                else
                {
                    baudDiff = baudEstimate - baudrate;
                }

                if (i == 0 || baudDiff < bestBaudDiff)
                {
                    // Closest to requested baud rate so far
                    bestDivisor = tryDivisor;
                    bestBaud = baudEstimate;
                    bestBaudDiff = baudDiff;
                    if (baudDiff == 0)
                    {
                        // Spot on! No point trying
                        break;
                    }
                }
            }

            // Encode the best divisor value
            long encodedDivisor = (bestDivisor >> 3) | (fracCode[bestDivisor & 7] << 14);
            // Deal with special cases for encoded value
            if (encodedDivisor == 1)
            {
                encodedDivisor = 0; // 3000000 baud
            }
            else if (encodedDivisor == 0x4001)
            {
                encodedDivisor = 1; // 2000000 baud (BM only)
            }

            // Split into "value" and "index" values
            long value = encodedDivisor & 0xFFFF;
            long index;
            if (_type == DeviceType.TYPE_2232C || _type == DeviceType.TYPE_2232H
                || _type == DeviceType.TYPE_4232H)
            {
                index = (encodedDivisor >> 8) & 0xffff;
                index &= 0xFF00;
                index |= PortNumber;
            }
            else
            {
                index = (encodedDivisor >> 16) & 0xffff;
            }

            // Return the nearest baud rate
            return new[] { bestBaud, index, value };
        }

        public override bool Cd => false;

        public override bool Cts => false;

        public override bool Dsr => false;

        public override bool Dtr
        {
            get { return false; }
            set { }
        }

        public override bool Ri => false;

        public override bool Rts
        {
            get { return false; }
            set { }
        }

        public new bool PurgeHwBuffers(bool purgeReadBuffers, bool purgeWriteBuffers)
        {
            if (purgeReadBuffers)
            {
                int result = Connection.ControlTransfer(FtdiDeviceOutReqtype, _sioResetRequest,
                    SioResetPurgeRx, PortNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);

                if (result != 0)
                {
                    throw new IOException("Flushing RX failed: result=" + result);
                }
            }

            if (purgeWriteBuffers)
            {
                int result = Connection.ControlTransfer(FtdiDeviceOutReqtype, _sioResetRequest,
                    SioResetPurgeTx, PortNumber + 1, null, 0, USB_WRITE_TIMEOUT_MILLIS);

                if (result != 0)
                {
                    throw new IOException("Flushing RX failed: result=" + result);
                }
            }
            return true;
        }
    }
}
