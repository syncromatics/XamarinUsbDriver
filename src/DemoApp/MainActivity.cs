using System;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
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
            var devices = UsbSerialProber.GetDefaultProber().FindAllDrivers(manager);

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
            var port2 = device.Ports[1];

            UsbDeviceConnection connection = manager.OpenDevice(device.Device);

            port1.Open(connection);
            port1.SetParameters(9600, DataBits._8, StopBits._1, Parity.None);
            port2.Open(connection);
            port2.SetParameters(9600, DataBits._8, StopBits._1, Parity.None);

            var message = new byte[] {88,1,8,0,159};

            var buffer = new byte[30];

            while (true)
            {
                port1.Write(message, (int)TimeSpan.FromSeconds(2).TotalMilliseconds);
                Thread.Sleep(1000);
                var bytesRead = port1.Read(buffer, (int) TimeSpan.FromSeconds(10).TotalMilliseconds);
                if (bytesRead > 0)
                {
                    
                }
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

