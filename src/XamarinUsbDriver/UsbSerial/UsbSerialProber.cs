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

using System.Collections.Generic;
using System.Linq;
using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    public class UsbSerialProber
    {
        private readonly ProbeTable _probeTable;

        public UsbSerialProber(ProbeTable probeTable)
        {
            _probeTable = probeTable;
        }

        public static UsbSerialProber GetDefaultProber()
        {
            return new UsbSerialProber(GetDefaultProbeTable());
        }

        public static ProbeTable GetDefaultProbeTable()
        {
            ProbeTable probeTable = new ProbeTable();
            probeTable.AddDriver(CdcAcmSerialDriver.GetSupportedDevices(), device => new CdcAcmSerialDriver(device));
            probeTable.AddDriver(FtdiSerialDriver.GetSupportedDevices(), device => new FtdiSerialDriver(device));
            return probeTable;
        }

        /// <summary>
        /// Finds and builds all possible {@link UsbSerialDriver UsbSerialDrivers}
        /// from the currently-attached {@link UsbDevice} hierarchy. This method does
        /// not require permission from the Android USB system, since it does not
        /// open any of the devices.
        /// </summary>
        /// <param name="usbManager"></param>
        /// <returns>a list, possibly empty, of all compatible drivers</returns>
        public List<IUsbSerialDriver> FindAllDrivers(UsbManager usbManager)
        {
            return usbManager.DeviceList.Values
                .Select(ProbeDevice)
                .Where(driver => driver != null)
                .ToList();
        }

        /// <summary>
        /// Probes a single device for a compatible driver.
        /// </summary>
        /// <param name="usbDevice">the usb device to probe</param>
        /// <returns>a new {@link UsbSerialDriver} compatible with this device, or </returns> {@code null} if none available.
        public IUsbSerialDriver ProbeDevice(UsbDevice usbDevice)
        {
            int vendorId = usbDevice.VendorId;
            int productId = usbDevice.ProductId;

            var driverFunc = _probeTable.FindDriver(vendorId, productId);

            return driverFunc?.Invoke(usbDevice);
        }
    }
}
