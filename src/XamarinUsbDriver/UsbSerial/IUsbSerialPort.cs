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

        /// <summary>
        /// Port number within driver.
        /// </summary>
        int PortNumber { get; }

        /// <summary>
        /// The serial number of the underlying UsbDeviceConnection, or {@code null}.
        /// </summary>
        string Serial { get; }

        //    /**
        //     * Opens and initializes the port. Upon success, caller must ensure that
        //     * {@link #close()} is eventually called.
        //     *
        //     * @param connection an open device connection, acquired with
        //     *            {@link UsbManager#openDevice(android.hardware.usb.UsbDevice)}
        //     * @throws IOException on error opening or initializing the port.
        //     */
        void Open(UsbDeviceConnection connection);

        //    /**
        //     * Closes the port.
        //     *
        //     * @throws IOException on error closing the port.
        //     */
        void Close();

        //    /**
        //     * Reads as many bytes as possible into the destination buffer.
        //     *
        //     * @param dest the destination byte buffer
        //     * @param timeoutMillis the timeout for reading
        //     * @return the actual number of bytes read
        //     * @throws IOException if an error occurred during reading
        //     */
        int Read(byte[] dest, int timeoutMillis);

        //    /**
        //     * Writes as many bytes as possible from the source buffer.
        //     *
        //     * @param src the source byte buffer
        //     * @param timeoutMillis the timeout for writing
        //     * @return the actual number of bytes written
        //     * @throws IOException if an error occurred during writing
        //     */
        int Write(byte[] src, int timeoutMillis);

        //    /**
        //     * Sets various serial port parameters.
        //     *
        //     * @param baudRate baud rate as an integer, for example {@code 115200}.
        //     * @param dataBits one of {@link #DATABITS_5}, {@link #DATABITS_6},
        //     *            {@link #DATABITS_7}, or {@link #DATABITS_8}.
        //     * @param stopBits one of {@link #STOPBITS_1}, {@link #STOPBITS_1_5}, or
        //     *            {@link #STOPBITS_2}.
        //     * @param parity one of {@link #PARITY_NONE}, {@link #PARITY_ODD},
        //     *            {@link #PARITY_EVEN}, {@link #PARITY_MARK}, or
        //     *            {@link #PARITY_SPACE}.
        //     * @throws IOException on error setting the port parameters
        //     */
        void SetParameters(int baudRate, DataBits dataBits, StopBits stopBits, Parity parity);

        //    /**
        //     * Gets the CD (Carrier Detect) bit from the underlying UART.
        //     *
        //     * @return the current state, or {@code false} if not supported.
        //     * @throws IOException if an error occurred during reading
        //     */
        bool CD { get; }

        //    /**
        //     * Gets the CTS (Clear To Send) bit from the underlying UART.
        //     *
        //     * @return the current state, or {@code false} if not supported.
        //     * @throws IOException if an error occurred during reading
        //     */
        bool CTS { get; }

        //    /**
        //     * Gets the DSR (Data Set Ready) bit from the underlying UART.
        //     *
        //     * @return the current state, or {@code false} if not supported.
        //     * @throws IOException if an error occurred during reading
        //     */
        bool DSR { get; }

        //    /**
        //     * Gets and sets the DTR (Data Terminal Ready) bit from the underlying UART.
        //     *
        //     * @return the current state, or {@code false} if not supported.
        //     * @throws IOException if an error occurred during reading
        //     */
        bool DTR { get; set; }

        //    /**
        //     * Gets the RI (Ring Indicator) bit from the underlying UART.
        //     *
        //     * @return the current state, or {@code false} if not supported.
        //     * @throws IOException if an error occurred during reading
        //     */
        bool RI { get; }

        //    /**
        //     * Gets and sets the RTS (Request To Send) bit from the underlying UART.
        //     *
        //     * @return the current state, or {@code false} if not supported.
        //     * @throws IOException if an error occurred during reading
        //     */
        bool RTS { get; set; }

        //    /**
        //     * Flush non-transmitted output data and / or non-read input data
        //     * @param flushRX {@code true} to flush non-transmitted output data
        //     * @param flushTX {@code true} to flush non-read input data
        //     * @return {@code true} if the operation was successful, or
        //     * {@code false} if the operation is not supported by the driver or device
        //     * @throws IOException if an error occurred during flush
        //     */
        bool PurgeHwBuffers(bool flushRX, bool flushTX);
    }
}
