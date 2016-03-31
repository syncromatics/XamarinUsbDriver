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
 */

using System;
using System.Threading.Tasks;
using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    /// <summary>
    /// A base class shared by several driver implementations.
    /// </summary>
    public abstract class CommonUsbSerialPort : IUsbSerialPort
    {
        public static int DefaultReadBufferSize = 524;
        public static int DefaultWriteBufferSize = 524;

        /// <summary>
        /// Returns the currently-bound USB device.
        /// </summary>
        public UsbDevice Device { get; }

        public IUsbSerialDriver Driver { get; }

        public int PortNumber { get; }

        protected UsbDeviceConnection Connection = null;

        protected object ReadBufferLock = new object();
        protected AsyncLock WriteBufferLock = new AsyncLock();

        /** Internal read buffer.  Guarded by {@link #mReadBufferLock}. */
        protected byte[] ReadBuffer;

        /** Internal write buffer.  Guarded by {@link #mWriteBufferLock}. */
        protected byte[] WriteBuffer;

        protected CommonUsbSerialPort(UsbDevice device, int portNumber)
        {
            Device = device;
            PortNumber = portNumber;

            ReadBuffer = new byte[DefaultReadBufferSize];
            WriteBuffer = new byte[DefaultWriteBufferSize];
        }

        public override string ToString()
        {
            return $"<{nameof(CommonUsbSerialPort)} device_name={Device.DeviceName} " +
                   $"device_id={Device.DeviceId} port_number={PortNumber}>";
        }

        /// <summary>
        /// Returns the device serial number
        /// </summary>
        public string Serial => Connection.Serial;

        /// <summary>
        /// Sets the size of the internal buffer used to exchange data with the USB 
        /// stack for read operations. Most users should not need to change this.
        /// </summary>
        /// <param name="bufferSize">the size in bytes</param>
        public void SetReadBufferSize(int bufferSize)
        {
            lock (ReadBufferLock)
            {
                if (bufferSize == ReadBuffer.Length)
                {
                    return;
                }
                ReadBuffer = new byte[bufferSize];
            }
        }

       /// <summary>
       /// Sets the size of the internal buffer used to exchange data with the USB 
       /// stack for write operations. Most users should not need to change this.
       /// </summary>
       /// <param name="bufferSize">the size in bytes</param>
        public void SetWriteBufferSize(int bufferSize)
        {
            using (WriteBufferLock.Lock())
            {
                if (bufferSize == WriteBuffer.Length)
                {
                    return;
                }
                WriteBuffer = new byte[bufferSize];
            }
        }

        public abstract void Open(UsbDeviceConnection connection);

        public abstract void Close();

        public abstract int Read(byte[] dest, int timeoutMillis);

        public abstract Task<int> ReadAsync(byte[] dest, int timeoutMillis);

        public abstract int Write(byte[] src, int timeoutMillis);

        public abstract Task<int> WriteAsync(byte[] src, int timeoutMillis);

        public abstract void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity);

        public abstract bool Cd { get; }

        public abstract bool Cts { get; }

        public abstract bool Dsr { get; }

        public abstract bool Dtr { get; set; }

        public abstract bool Ri { get; }

        public abstract bool Rts { get; set; }

        public bool PurgeHwBuffers(bool flushRx, bool flushTx)
        {
            return !flushRx && !flushTx;
        }
    }
}
