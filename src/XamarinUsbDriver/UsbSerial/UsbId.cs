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

namespace XamarinUsbDriver.UsbSerial
{
    /// <summary>
    /// Registry of USB vendor/product ID constants.
    ///  Culled from various sources; see 
    /// <a href="http://www.linux-usb.org/usb.ids">usb.ids</a> for one listing.
    /// </summary>
    public static class UsbId
    {
        public static int VendorFtdi = 0x0403;

        public static int FtdiFt232R = 0x6001;
        public static int FtdiFt231X = 0x6015;
        public static int FtdiFt2322 = 0x6010;
        public static int FtdiFt4232H = 0x6011;

        public static int VendorSyncromatics = 0x01A4;

        public static int SyncromaticsCovertAlarm = 0x0001;

        public static int VendorAtmel = 0x03EB;
        public static int AtmelLufaCdcDemoApp = 0x2044;

        public static int VendorArduino = 0x2341;
        public static int ArduinoUno = 0x0001;
        public static int ArduinoMega2560 = 0x0010;
        public static int ArduinoSerialAdapter = 0x003b;
        public static int ArduinoMegaAdk = 0x003f;
        public static int ArduinoMega2560R3 = 0x0042;
        public static int ArduinoUnoR3 = 0x0043;
        public static int ArduinoMegaAdkR3 = 0x0044;
        public static int ArduinoSerialAdapterR3 = 0x0044;
        public static int ArduinoLeonardo = 0x8036;

        public static int VendorVanOoijenTech = 0x16c0;
        public static int VanOoijenTechTeensyduinoSerial = 0x0483;

        public static int VendorLeaflabs = 0x1eaf;
        public static int LeaflabsMaple = 0x0004;

        public static int VendorSilabs = 0x10c4;
        public static int SilabsCp2102 = 0xea60;
        public static int SilabsCp2105 = 0xea70;
        public static int SilabsCp2108 = 0xea71;
        public static int SilabsCp2110 = 0xea80;

        public static int VendorProlific = 0x067b;
        public static int ProlificPl2303 = 0x2303;

        public static int VendorQinheng = 0x1a86;
        public static int QinhengHl340 = 0x7523;

        public static int VendorMagtek = 0x0801;
        public static int MagtekSureSwipe = 0x0002;
    }
}
