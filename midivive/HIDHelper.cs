using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiVive
{
    class HIDHelper
    {
        private static double STEAM_CONTROLLER_MAGIC_PERIOD_RATIO = 495483.0F;
        public static void GetDevices()
        {
            //GetWands();
            GetControllers();
        }

        static HidDevice[] ViveWands;
        private static void GetWands()
        {
            ViveWands = HidDevices.Enumerate(0x28de, 0x2012).ToArray();//wired
                                                                       //0x2101 wireless
            foreach (HidDevice wand in ViveWands)
            {
                if (wand.Capabilities.OutputReportByteLength < 1)
                {
                    continue;
                }
                Console.WriteLine("Found device {0}", wand.ToString());
                byte[] HapticPulse = { 0xff, 0x8f, 0x07, 0x00, 0xf4, 0x01, 0xb5, 0xa2, 0x01, 0x00 };
                //byte[] OutData = new byte[wand.Capabilities.OutputReportByteLength - 1];
                Console.WriteLine("Report length: {0}", wand.Capabilities.OutputReportByteLength);
            }
        }

        private static void GetControllers()
        {
            //0x28DE, 0x1102 // Wired Steam Controller
            //0x28DE, 0x1142 // Steam Controller dongle
            //HidDevice[] controllers = HidDevices.Enumerate(0x28de, 0x1142).ToArray();
            foreach (HidDevice controller in HidDevices.Enumerate(0x28de, 0x1142))
             {
                 Console.WriteLine("Found device {0}", controller.ToString());
                
                Console.WriteLine("Report length: {0}", controller.Capabilities.OutputReportByteLength);
                if (controller.Capabilities.OutputReportByteLength != 65)
                {
                    continue;
                }

                controller.OpenDevice();
                Console.WriteLine("Device open: {0}", controller.IsOpen);

                 byte[] dataBlob = {0x8f,// STEAMCONTROLLER_TRIGGER_HAPTIC_PULSE
                                   0x07,
                                   0x00, //Trackpad select : 0x01 = left, 0x00 = right
                                   0xff, //LSB Pulse High Duration
                                   0xff, //MSB Pulse High Duration
                                   0xff, //LSB Pulse Low Duration
                                   0xff, //MSB Pulse Low Duration
                                   0xff, //LSB Pulse repeat count
                                   0x04, //MSB Pulse repeat count
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
                 byte haptic = 1;
                 int frequency = 100;
                 int duration = 1000;

                 double period = 1.0 / frequency;
                 int periodCommand = (int) (period * STEAM_CONTROLLER_MAGIC_PERIOD_RATIO);
                 //Compute number of repeat. If duration < 0, set to maximum
                 int repeatCount = (duration >= 0.0) ? (int)(duration / period) : 0x7FFF;
                 Console.WriteLine("Period command: {0}, repeat count: {1}, length: {2}", periodCommand, repeatCount, dataBlob.Length);


                 dataBlob[2] = haptic;
                 dataBlob[3] = (byte)((periodCommand >> 0) & 0xff);
                 dataBlob[4] = (byte)((periodCommand >> 8) & 0xff);
                 dataBlob[5] = (byte)((periodCommand >> 16) & 0xff);
                 dataBlob[6] = (byte)((periodCommand >> 32) & 0xff);
                 dataBlob[7] = (byte)((repeatCount >> 0) & 0xff);
                 dataBlob[8] = (byte)((repeatCount >> 8) & 0xff);
                //var readReport = controller.ReadReport(64);
                //var readData = readReport.Data;
                Console.WriteLine(dataBlob.ToString());
                 HidReport report = new HidReport(1)
                 {
                     Data = dataBlob
                 };

                 bool isQueryWritten = controller.WriteReportSync(report);
                 Console.WriteLine("Report written: {0}", isQueryWritten);
                 Thread.Sleep(1000);
                 controller.CloseDevice();
             }

        }
    }
}