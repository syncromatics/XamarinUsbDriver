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
    public enum DataBits
    {
        _5 = 5,
        _6 = 6,
        _7 = 7,
        _8 = 8
    }

    public enum FlowControl
    {
        None = 0,
        RtsCtsIn = 1,
        RtsCtsOut = 2,
        XonXofffIn = 4,
        XonXoffOut = 8
    }

    public enum Parity
    {
        None = 0,
        Odd = 1,
        Even = 2,
        Mark = 3,
        Space = 4
    }

    public enum StopBits
    {
        _1 = 1,
        _1_5 = 3,
        _2 = 2
    }

    /// <summary>
    /// Interface for a single serial port.
    /// 
    /// @author mike wakerly (opensource@hoho.com)
    /// </summary>
    public interface IUsbSerialPort
    {
        IUsbSerialDriver Driver { get; }

        UsbDevice Device { get; }

        /// <summary>
        /// Port number within driver.
        /// </summary>
        int PortNumber { get; }

        /// <summary>
        /// The serial number of the underlying UsbDeviceConnection, or {@code null}.
        /// </summary>
        string Serial { get; }

        /// <summary>
        /// Opens and initializes the port. Upon success, caller must ensure that {@link #close()} is eventually called.
        /// </summary>
        /// <param name="connection">
        /// an open device connection, acquired with {@link UsbManager#openDevice(android.hardware.usb.UsbDevice)}
        /// </param>
        void Open(UsbDeviceConnection connection);

        /// <summary>
        /// Closes the port.
        /// </summary>
        void Close();

        /// <summary>
        /// Reads as many bytes as possible into the destination buffer.
        /// </summary>
        /// <param name="dest">the destination byte buffer</param>
        /// <param name="timeoutMillis">the timeout for reading</param>
        /// <returns>the actual number of bytes read</returns>
        int Read(byte[] dest, int timeoutMillis);

        /// <summary>
        /// Reads as many bytes as possible into the destination buffer.
        /// </summary>
        /// <param name="dest">the destination byte buffer</param>
        /// <param name="timeoutMillis">the timeout for reading</param>
        /// <returns>the actual number of bytes read</returns>
        Task<int> ReadAsync(byte[] dest, int timeoutMillis);

        /// <summary>
        /// Writes as many bytes as possible from the source buffer.
        /// </summary>
        /// <param name="src">the source byte buffer</param>
        /// <param name="timeoutMillis">the timeout for writing</param>
        /// <returns>the actual number of bytes written</returns>
        int Write(byte[] src, int timeoutMillis);

        /// <summary>
        /// Writes as many bytes as possible from the source buffer.
        /// </summary>
        /// <param name="src">the source byte buffer</param>
        /// <param name="timeoutMillis">the timeout for writing</param>
        /// <returns>the actual number of bytes written</returns>
        Task<int> WriteAsync(byte[] src, int timeoutMillis);

        /// <summary>
        /// Sets various serial port parameters.
        /// </summary>
        /// <param name="baudRate">baud rate as an integer, for example {@code 115200}.</param>
        /// <param name="dataBits">one of {@link #DATABITS_5}, {@link #DATABITS_6}, 
        /// {@link #DATABITS_7}, or {@link #DATABITS_8}.</param>
        /// <param name="stopBits">one of {@link #STOPBITS_1}, {@link #STOPBITS_1_5}, 
        /// or {@link #STOPBITS_2}</param>
        /// <param name="parity">one of {@link #PARITY_NONE}, {@link #PARITY_ODD}, 
        /// {@link #PARITY_EVEN}, {@link #PARITY_MARK}, or {@link #PARITY_SPACE}</param>
        void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity);

        /// <summary>
        /// Gets the CD (Carrier Detect) bit from the underlying UART.
        /// </summary>
        bool Cd { get; }

        /// <summary>
        /// Gets the CTS (Clear To Send) bit from the underlying UART.
        /// </summary>
        bool Cts { get; }

        /// <summary>
        /// Gets the DSR (Data Set Ready) bit from the underlying UART.
        /// </summary>
        bool Dsr { get; }

        /// <summary>
        /// Gets and sets the DTR (Data Terminal Ready) bit from the underlying UART.
        /// </summary>
        bool Dtr { get; set; }

        /// <summary>
        /// Gets the RI (Ring Indicator) bit from the underlying UART.
        /// </summary>
        bool Ri { get; }

        /// <summary>
        /// Gets and sets the RTS (Request To Send) bit from the underlying UART.
        /// </summary>
        bool Rts { get; set; }

        /// <summary>
        /// Flush non-transmitted output data and / or non-read input data
        /// </summary>
        /// <param name="flushRx">{@code true} to flush non-transmitted output data</param>
        /// <param name="flushTx">{@code true} to flush non-read input data</param>
        /// <returns>{@code true} if the operation was successful, or {@code false} if the 
        /// operation is not supported by the driver or device</returns>
        bool PurgeHwBuffers(bool flushRx, bool flushTx);
    }
}
