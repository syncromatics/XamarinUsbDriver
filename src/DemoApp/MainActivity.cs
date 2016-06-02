using System;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Util;
using XamarinUsbDriver.UsbSerial;

namespace DemoApp
{
    
    [Activity(Label = "DemoApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            UsbManager manager = (UsbManager)GetSystemService(UsbService);
            var devices = UsbSerialProber
                .GetDefaultProber()
                .FindAllDrivers(manager)
                .Where(driver => driver is CdcAcmSerialDriver);

            var device = devices.First();

            if (!manager.HasPermission(device.Device))
            {
                var usbPermission = "DemoApp.DemoApp.USB_PERMISSION";

                var permissionIntent = PendingIntent.GetBroadcast(this, 0, new Intent(usbPermission), 0);

                IntentFilter filter = new IntentFilter(usbPermission);

                var receiver = new UsbPermissionReceiver
                {
                    Callback = () => WireupUsb(device)
                };

                RegisterReceiver(receiver, filter);

                manager.RequestPermission(device.Device, permissionIntent);
            }
            else
            {
                WireupUsb(device);
            }
        }

        private void WireupUsb(IUsbSerialDriver device)
        {
            UsbManager manager = (UsbManager) GetSystemService(UsbService);

            var port1 = device.Ports[0];

            UsbDeviceConnection connection = manager.OpenDevice(device.Device);

            port1.Open(connection);

            var message = new byte[] {192, 0, 14, 8, 188, 1, 252, 4, 7, 49, 50, 51, 52, 53, 42, 42, 211};

            message = Encoding.ASCII.GetBytes("ver\r");
            var buffer = new byte[20];

            while (true)
            {
                port1.Write(message, (int)TimeSpan.FromSeconds(2).TotalMilliseconds);

                var bytesRead = port1.Read(buffer, (int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                if (bytesRead > 0)
                {
                    var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                }
                Thread.Sleep(50);

                Log.Debug("main", "here");
            }
        }
    }

    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new [] { "DemoApp.DemoApp.USB_PERMISSION"})]
    public class UsbPermissionReceiver : BroadcastReceiver
    {
        public Action Callback { get; set; }

        public override void OnReceive(Context context, Intent intent)
        {
            Callback();
        }
    }
}

