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

using System.Collections.Generic;
using Android.Hardware.Usb;
using Java.Lang;

namespace XamarinUsbDriver.UsbSerial
{
    /// <summary>
    /// 
    /// </summary>
    public class FtdiSerialDriver : IUsbSerialDriver
    {
        public UsbDevice Device { get; }

        public List<IUsbSerialPort> Ports { get; } = new List<IUsbSerialPort>();

        private Thread _receiver;

        private UsbDeviceConnection _connection;

        public FtdiSerialDriver(UsbDevice device)
        {
            Device = device;

            if (device.ProductId == UsbId.FtdiFt2322)
            {
                Ports.Add(new FtdiSerialPort(Device, 0, this));
                Ports.Add(new FtdiSerialPort(Device, 1, this));
            }
            else
            {
                Ports.Add(new FtdiSerialPort(Device, 0, this));
            }
        }

        public static Dictionary<int, int[]> GetSupportedDevices()
        {
            return new Dictionary<int, int[]>
            {
                {
                    UsbId.VendorFtdi, new[]
                    {
                        UsbId.FtdiFt232R,
                        UsbId.FtdiFt231X,
                        UsbId.FtdiFt2322
                    }
                }
            };
        }
    }
    internal enum DeviceType
    {
        TYPE_BM,
        TYPE_AM,
        TYPE_2232C,
        TYPE_R,
        TYPE_2232H,
        TYPE_4232H
    }
}
