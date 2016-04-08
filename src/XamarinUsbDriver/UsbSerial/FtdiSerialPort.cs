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
using System.Diagnostics;
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

        private int _index;

        public FtdiSerialPort(UsbDevice device, int portNumber, FtdiSerialDriver driver) : base(device, portNumber)
        {
            _index = portNumber + 1;
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

        public void Reset()
        {
            int result = Connection.ControlTransfer(FtdiDeviceOutReqtype, _sioResetRequest,
                SioResetSio, _index, null, 0, USB_WRITE_TIMEOUT_MILLIS);
            if (result != 0)
            {
                throw new IOException("Reset failed: result=" + result);
            }

            var productId = Device.ProductId;

            if(productId == 0x6001)
                _type = DeviceType.TYPE_BM;
            else if(productId == 0x6010)
                _type = DeviceType.TYPE_2232H;
            else if (productId == 0x6011)
                _type = DeviceType.TYPE_4232H;
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
            UsbEndpoint endpoint = Device.GetInterface(PortNumber).GetEndpoint(0);

            lock (ReadBufferLock)
            {
                int bytesLeft = dest.Length;
                int bytesRead = 0;
                var watch = Stopwatch.StartNew();

                while (watch.ElapsedMilliseconds < timeoutMillis && bytesLeft != 0)
                {
                    var bytesToRead = Math.Min(bytesLeft + 2, ReadBuffer.Length);
                    var totalBytesRead = Connection.BulkTransfer(endpoint, ReadBuffer, bytesToRead, timeoutMillis);

                    if (totalBytesRead == -1)
                        continue;

                    if (totalBytesRead < ModemStatusHeaderLength)
                    {
                        throw new IOException("Expected at least " + ModemStatusHeaderLength + " bytes");
                    }

                    if (totalBytesRead <= 2)
                        continue;

                    Buffer.BlockCopy(ReadBuffer, 2, dest, bytesRead, totalBytesRead - 2);
                    bytesLeft -= totalBytesRead - 2;
                    bytesRead += totalBytesRead - 2;
                }

                return bytesRead;
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
            int errorCount = 0;

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
                        errorCount++;
                        if (errorCount >= 3)
                            return 0;

                        Thread.Sleep(10);
                        amtWritten = 0;
                    }

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
            var vals = GetBaudRate(baudRate);

            int result = Connection.ControlTransfer(FtdiDeviceOutReqtype,
                SioSetBaudRateRequest, vals.Value, vals.Index,
                null, 0, USB_WRITE_TIMEOUT_MILLIS);

            if (result != 0)
            {
                throw new IOException($"Setting baudrate failed: result={result}");
            }

            return vals.BaudRate;
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
                SioSetDataRequest, config, _index,
                null, 0, USB_WRITE_TIMEOUT_MILLIS);

            if (result != 0)
            {
                throw new IOException("Setting parameters failed: result=" + result);
            }
        }

        private uint H_CLK = 120000000;
        private uint C_CLK = 48000000;

        private BaudRateResponse GetBaudRate(int baudRate)
        {
            int result = 1;
            int[] divisors = new int[2];
            int status = 0;

            if (_type != DeviceType.TYPE_4232H)
            {
                switch (baudRate)
                {
                    case 300:
                        divisors[0] = 10000;
                        break;
                    case 600:
                        divisors[0] = 5000;
                        break;
                    case 1200:
                        divisors[0] = 2500;
                        break;
                    case 2400:
                        divisors[0] = 1250;
                        break;
                    case 4800:
                        divisors[0] = 625;
                        break;
                    case 9600:
                        divisors[0] = 16696;
                        break;
                    case 19200:
                        divisors[0] = 32924;
                        break;
                    case 38400:
                        divisors[0] = 49230;
                        break;
                    case 57600:
                        divisors[0] = 52;
                        break;
                    case 115200:
                        divisors[0] = 26;
                        break;
                    case 230400:
                        divisors[0] = 13;
                        break;
                    case 460800:
                        divisors[0] = 16390;
                        break;
                    case 921600:
                        divisors[0] = 32771;
                        break;
                        //default:
                        //    if ((isHiSpeed()) && (baudRate >= 1200))
                        //    {
                        //        result = FT_BaudRate.FT_GetDivisorHi(baudRate, divisors);
                        //    }
                        //    else {
                        //        result = FT_BaudRate.FT_GetDivisor(baudRate, divisors,
                        //          isBmDevice());
                        //    }
                        //    status = 255;
                        //    break;
                }
            }
            else
            {
                divisors[0] = (int)Ftdi2232HBaudToDivisor(baudRate);
            }

            var urbValue = (UInt16) divisors[0];

            var index = (UInt16) (divisors[0] >> 16);

            if (isMultiIfDevice())
            {
                index = (UInt16)((index << 8) | _index);
            }

            return new BaudRateResponse
            {
                Index = index,
                Value = urbValue
            };
        }

        private uint Ftdi2232HBaudToDivisor(int baud)
        {
            return Ftdi2232HBaudBaseToDivisor(baud, 120000000);
        }

        private uint Ftdi2232HBaudBaseToDivisor(int baud, int @base)
        {
            int[] divfrac = { 0, 3, 2, 4, 1, 5, 6, 7 };
            uint divisor3 = (uint)(@base / 10 / baud) * 8; // hi-speed baud rate is 10-bit sampling instead of 16-bit
            uint divisor = divisor3 >> 3;
            divisor |= (uint)divfrac[divisor3 & 0x7] << 14;
            /* Deal with special cases for highest baud rates. */
            if (divisor == 1) divisor = 0;
            else    // 1.0
            if (divisor == 0x4001) divisor = 1; // 1.5
                                                /* Set this bit to turn off a divide by 2.5 on baud rate generator */
                                                /* This enables baud rates up to 12Mbaud but cannot reach below 1200 baud with this bit set */
            divisor |= 0x00020000;

            return divisor;
        }

        private bool isHiSpeed()
        {
            return _type == DeviceType.TYPE_232H 
                || _type == DeviceType.TYPE_2232H 
                || _type == DeviceType.TYPE_4232H;
        }

        private bool isBmDevice()
        {
            return _type == DeviceType.TYPE_BM;
            //(isFt232b()) || (isFt2232()) || (isFt232r()) || (isFt2232h()) || (isFt4232h()) || (isFt232h()) || (isFt232ex());
        }

        bool isMultiIfDevice()
        {
            return _type == DeviceType.TYPE_2232H
                   || _type == DeviceType.TYPE_4232H
                   || _type == DeviceType.TYPE_2232C;
        }

        private BaudRateResponse FtdiConverBaudrate(int baudrate)
        {
            int bestBaud;
            ulong encodedDivisor = 0;
            ushort index;

            if(baudrate <= 0)
                throw new ArgumentException($"baudrate cannot be 0 or lower", nameof(baudrate));

            if (_type == DeviceType.TYPE_2232H || _type == DeviceType.TYPE_4232H | _type == DeviceType.TYPE_232H)
            {
                if (baudrate*10 > H_CLK/0x3fff)
                {
                    bestBaud = FtdiToClkbits(baudrate, H_CLK, 10, ref encodedDivisor);
                    encodedDivisor |= 0x20000;
                }
                else
                {
                    bestBaud = FtdiToClkbits(baudrate, C_CLK, 16, ref encodedDivisor);
                }
            }
            else if (_type == DeviceType.TYPE_BM || _type == DeviceType.TYPE_2232C || _type == DeviceType.TYPE_R)
            {
                bestBaud = FtdiToClkbits(baudrate, C_CLK, 16, ref encodedDivisor);
            }
            else
            {
                bestBaud = ftdi_to_clkbits_AM(baudrate, ref encodedDivisor);
            }

            var value = (ushort)(encodedDivisor & 0xffff);
            if (_type == DeviceType.TYPE_2232H || _type == DeviceType.TYPE_4232H || _type == DeviceType.TYPE_232H)
            {
                index = (ushort) (encodedDivisor >> 8);
                index &= 0xff00;
                index |= (ushort) (PortNumber + 1);
            }
            else
                index = (ushort) (encodedDivisor >> 16);

            return new BaudRateResponse
            {
                Index = index,
                BaudRate = bestBaud,
                Value = value
            };
        }

        private int FtdiToClkbits(int baudrate, uint clk, int clkDivisor, ref ulong encodedDivisor)
        {
            int[] fracCode = { 0, 3, 2, 4, 1, 5, 6, 7 };
            int bestBaud;

            if (baudrate >= clk/clkDivisor)
            {
                encodedDivisor = 0;
                bestBaud = (int)(clk/clkDivisor);
            }
            else if (baudrate >= clk/(clkDivisor + clkDivisor/2))
            {
                encodedDivisor = 1;
                bestBaud = (int)(clk/(2*clkDivisor));
            }
            else if (baudrate >= clk/(2*clkDivisor))
            {
                encodedDivisor = 2;
                bestBaud = (int) (clk/(2*clkDivisor));
            }
            else
            {
                var divisor = (int)(clk*16/clkDivisor/baudrate);
                int bestDivisor;
                if ((divisor & 1) > 0)
                {
                    bestDivisor = divisor/2 + 1;
                }
                else
                {
                    bestDivisor = divisor/2;
                }

                if (bestDivisor > 0x20000)
                    bestDivisor = 0x1ffff;

                bestBaud = (int)(clk*16/clkDivisor/bestDivisor);
                encodedDivisor = (ulong)((bestDivisor >> 1) | (fracCode[bestDivisor & 0x7] << 14));
            }

            return bestBaud;
        }

        private static int ftdi_to_clkbits_AM(int baudrate, ref ulong encodedDivisor)
        {
            int[] fracCode = { 0, 3, 2, 4, 1, 5, 6, 7 };

            int[] amAdjustUp = { 0, 0, 0, 1, 0, 3, 2, 1 };

            int[] amAdjustDn = { 0, 0, 0, 1, 0, 1, 2, 3 };

            int i;

            var divisor = 24000000 / baudrate;

            divisor -= amAdjustDn[divisor & 7];

            var bestDivisor = 0;
            var bestBaud = 0;
            var bestBaudDiff = 0;

            for (i = 0; i < 2; i++)
            {
                int tryDivisor = divisor + i;

                int baudDiff;

                if (tryDivisor <= 8)
                {
                    tryDivisor = 8;
                }
                else if (divisor < 16)
                {
                    tryDivisor = 16;
                }
                else
                {
                    tryDivisor += amAdjustUp[tryDivisor & 7];
                    if (tryDivisor > 0x1fff8)
                    {
                        tryDivisor = 0x1fff8;
                    }
                }

                var baudEstimate = (24000000 + (tryDivisor/2))/tryDivisor;

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
                    bestDivisor = tryDivisor;
                    bestBaud = baudEstimate;
                    bestBaudDiff = baudDiff;
                    if (baudDiff == 0)
                        break;
                }
            }

            encodedDivisor = (ulong)((bestDivisor >> 3) | (fracCode[bestDivisor & 7] << 14));
            if (encodedDivisor == 1)
            {
                encodedDivisor = 0;
            }
            else if (encodedDivisor == 0x4001)
            {
                encodedDivisor = 1;
            }

            return bestBaud;
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

    public class BaudRateResponse
    {
        public int Value { get; set; }
        public int Index { get; set; }
        public int BaudRate { get; set; }
    }
}
