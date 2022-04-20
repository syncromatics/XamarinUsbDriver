using System.Collections.Generic;
using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    public class ProlificSerialDriver : IUsbSerialDriver
    {
        public UsbDevice Device { get; }

        public List<IUsbSerialPort> Ports { get; }

        public ProlificSerialDriver(UsbDevice device)
        {
            Device = device;

            Ports = new List<IUsbSerialPort>
            {
                new ProlificSerialPort(device, 0)
            };
        }

        public static Dictionary<int, int[]> GetSupportedDevices()
        {
            return new Dictionary<int, int[]>
            {
                {
                    UsbId.VendorProlific, new[]
                    {
                        UsbId.ProlificPl2303,
                        UsbId.ProlificPl23A3,
                    }
                }
            };
        }
    }
}
