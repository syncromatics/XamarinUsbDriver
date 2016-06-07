using System.Collections.Generic;
using Android.Hardware.Usb;

namespace XamarinUsbDriver.UsbSerial
{
    public class HidSerialDriver : IUsbSerialDriver
    {
        public UsbDevice Device { get; }

        public List<IUsbSerialPort> Ports { get; }

        public HidSerialDriver(UsbDevice device)
        {
            Device = device;
            Ports = new List<IUsbSerialPort> {new HidSerialPort(device, 0)};
        }

        public static Dictionary<int, int[]> GetSupportedDevices()
        {
            return new Dictionary<int, int[]>
            {
                {UsbId.VendorMagtek, new[] {UsbId.MagtekSureSwipe}}
            };
        }
    }
}
