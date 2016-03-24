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
using System.Collections.Generic;
using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    /**
 * Maps (vendor id, product id) pairs to the corresponding serial driver.
 *
 * @author mike wakerly (opensource@hoho.com)
 */

    public class ProbeTable
    {

        private readonly Dictionary<Tuple<int, int>, Func<UsbDevice, IUsbSerialDriver>> _probeTable =
            new Dictionary<Tuple<int, int>, Func<UsbDevice, IUsbSerialDriver>>();

        /**
         * Adds or updates a (vendor, product) pair in the table.
         *
         * @param vendorId the USB vendor id
         * @param productId the USB product id
         * @param driverClass the driver class responsible for this pair
         * @return {@code this}, for chaining
         */

        public ProbeTable AddProduct(int vendorId, int productId, Func<UsbDevice, IUsbSerialDriver> driverFunc)
        {
            _probeTable.Add(Tuple.Create(vendorId, productId), driverFunc);
            return this;
        }

        /**
     * Internal method to add all supported products from
     * {@code getSupportedProducts} static method.
     *
     * @param driverClass
     * @return
     */

        public ProbeTable AddDriver(Dictionary<int, int[]> productPairs, Func<UsbDevice, IUsbSerialDriver> driverFunc)
        {
            foreach (var entry in productPairs)
            {
                int vendorId = entry.Key;
                foreach (int productId in entry.Value)
                {
                    AddProduct(vendorId, productId, driverFunc);
                }
            }

            return this;
        }

        /**
         * Returns the driver for the given (vendor, product) pair, or {@code null}
         * if no match.
         *
         * @param vendorId the USB vendor id
         * @param productId the USB product id
         * @return the driver class matching this pair, or {@code null}
         */

        public Func<UsbDevice, IUsbSerialDriver> FindDriver(int vendorId, int productId)
        {
            Tuple<int, int> pair = Tuple.Create(vendorId, productId);

            Func<UsbDevice, IUsbSerialDriver> driverFun;
            _probeTable.TryGetValue(pair, out driverFun);

            return driverFun;
        }
    }
}
