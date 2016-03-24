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

using Java.Lang;

namespace XamarinUsbDriver.UsbSerial
{
    /**
     * Registry of USB vendor/product ID constants.
     *
     * Culled from various sources; see
     * <a href="http://www.linux-usb.org/usb.ids">usb.ids</a> for one listing.
     *
     * @author mike wakerly (opensource@hoho.com)
     */
    public static class UsbId
    {

        public static int VENDOR_FTDI = 0x0403;
        public static int FTDI_FT232R = 0x6001;
        public static int FTDI_FT231X = 0x6015;

        public static int VENDOR_ATMEL = 0x03EB;
        public static int ATMEL_LUFA_CDC_DEMO_APP = 0x2044;

        public static int VENDOR_ARDUINO = 0x2341;
        public static int ARDUINO_UNO = 0x0001;
        public static int ARDUINO_MEGA_2560 = 0x0010;
        public static int ARDUINO_SERIAL_ADAPTER = 0x003b;
        public static int ARDUINO_MEGA_ADK = 0x003f;
        public static int ARDUINO_MEGA_2560_R3 = 0x0042;
        public static int ARDUINO_UNO_R3 = 0x0043;
        public static int ARDUINO_MEGA_ADK_R3 = 0x0044;
        public static int ARDUINO_SERIAL_ADAPTER_R3 = 0x0044;
        public static int ARDUINO_LEONARDO = 0x8036;

        public static int VENDOR_VAN_OOIJEN_TECH = 0x16c0;
        public static int VAN_OOIJEN_TECH_TEENSYDUINO_SERIAL = 0x0483;

        public static int VENDOR_LEAFLABS = 0x1eaf;
        public static int LEAFLABS_MAPLE = 0x0004;

        public static int VENDOR_SILABS = 0x10c4;
        public static int SILABS_CP2102 = 0xea60;
        public static int SILABS_CP2105 = 0xea70;
        public static int SILABS_CP2108 = 0xea71;
        public static int SILABS_CP2110 = 0xea80;

        public static int VENDOR_PROLIFIC = 0x067b;
        public static int PROLIFIC_PL2303 = 0x2303;

        public static int VENDOR_QINHENG = 0x1a86;
        public static int QINHENG_HL340 = 0x7523;
    }
}
