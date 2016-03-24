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

using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    /**
 * A base class shared by several driver implementations.
 *
 * @author mike wakerly (opensource@hoho.com)
 */

    public abstract class CommonUsbSerialPort : IUsbSerialPort
    {
        public static int DEFAULT_READ_BUFFER_SIZE = 16*1024;
        public static int DEFAULT_WRITE_BUFFER_SIZE = 16*1024;

        protected UsbDevice mDevice;
        protected int mPortNumber;

        // non-null when open()
        protected UsbDeviceConnection mConnection = null;

        protected object mReadBufferLock = new object();
        protected object mWriteBufferLock = new object();

        /** Internal read buffer.  Guarded by {@link #mReadBufferLock}. */
        protected byte[] mReadBuffer;

        /** Internal write buffer.  Guarded by {@link #mWriteBufferLock}. */
        protected byte[] mWriteBuffer;

        public CommonUsbSerialPort(UsbDevice device, int portNumber)
        {
            mDevice = device;
            mPortNumber = portNumber;

            mReadBuffer = new byte[DEFAULT_READ_BUFFER_SIZE];
            mWriteBuffer = new byte[DEFAULT_WRITE_BUFFER_SIZE];
        }

        public override string ToString()
        {
            return "";
            //return String.format("<%s device_name=%s device_id=%s port_number=%s>",
            //        getClass().getSimpleName(), mDevice.getDeviceName(),
            //        mDevice.getDeviceId(), mPortNumber);
        }

        /**
     * Returns the currently-bound USB device.
     *
     * @return the device
     */
        public UsbDevice Device { get; }

        public int PortNumber { get; }

        public IUsbSerialDriver Driver { get; }

        /**
     * Returns the device serial number
     *  @return serial number
     */
        public string Serial => mConnection.Serial;

        /**
     * Sets the size of the internal buffer used to exchange data with the USB
     * stack for read operations.  Most users should not need to change this.
     *
     * @param bufferSize the size in bytes
     */

        public void SetReadBufferSize(int bufferSize)
        {
            lock (mReadBufferLock)
            {
                if (bufferSize == mReadBuffer.Length)
                {
                    return;
                }
                mReadBuffer = new byte[bufferSize];
            }
        }

        /**
     * Sets the size of the internal buffer used to exchange data with the USB
     * stack for write operations.  Most users should not need to change this.
     *
     * @param bufferSize the size in bytes
     */

        public void SetWriteBufferSize(int bufferSize)
        {
            lock (mWriteBufferLock)
            {
                if (bufferSize == mWriteBuffer.Length)
                {
                    return;
                }
                mWriteBuffer = new byte[bufferSize];
            }
        }

        public abstract void Open(UsbDeviceConnection connection);

        public abstract void Close();

        public abstract int Read(byte[] dest, int timeoutMillis);

        public abstract int Write(byte[] src, int timeoutMillis);

        public abstract void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity);

        public abstract bool CD { get; }

        public abstract bool CTS { get; }

        public abstract bool DSR { get; }

        public abstract bool DTR { get; set; }

        public abstract bool RI { get; }

        public abstract bool RTS { get; set; }

        public bool PurgeHwBuffers(bool flushReadBuffers, bool flushWriteBuffers)
        {
            return !flushReadBuffers && !flushWriteBuffers;
        }
    }
}
