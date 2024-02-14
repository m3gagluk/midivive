using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiVive
{
    class ViveControllerPlayer : Player
    {
        private HidStream _stream;

        private readonly double STEAM_CONTROLLER_MAGIC_PERIOD_RATIO = 2*495483.0;

        public ViveControllerPlayer(HidStream stream)
        {
            this._stream = stream;
        }

        public override bool PlayNote(double frequency, double duration, float volume)
        {
            this.Duration = duration;
            double period = 1.0 / frequency;
            int periodCommand = (int)(period * STEAM_CONTROLLER_MAGIC_PERIOD_RATIO);
            double dutyCycle = 0.5D;
            int periodHigh = (int)(periodCommand * (dutyCycle * volume));
            int periodLow = (int)(periodCommand*(1-dutyCycle*volume));
            //Console.WriteLine("high " + periodHigh + " low " + periodLow);
            //convert duration back to seconds
            duration = duration / 1000D;

            //Compute number of repeat. If duration < 0, set to maximum
            int repeatCount = (duration >= 0.0) ? (int)(duration / period) : 0x7FFF;
            //exactly the same packet as the steamcontroller one
            byte[] dataBlob = {0xff, 0x8f,// STEAMCONTROLLER_TRIGGER_HAPTIC_PULSE
                                   0x07,
                                   0x00, //motor id 0
                                   0x6c, //LSB Pulse High Duration
                                   0x07, //MSB Pulse High Duration
                                   0x6c, //LSB Pulse Low Duration
                                   0x07, //MSB Pulse Low Duration
                                   0x7f, //LSB Pulse repeat count
                                   0x80, //MSB Pulse repeat count
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            dataBlob[4] = (byte)((periodHigh >> 0) & 0xff);
            dataBlob[5] = (byte)((periodHigh >> 8) & 0xff);
            dataBlob[6] = (byte)((periodLow >> 0) & 0xff);
            dataBlob[7] = (byte)((periodLow >> 8) & 0xff);
            dataBlob[8] = (byte)((repeatCount >> 0) & 0xff);
            dataBlob[9] = (byte)((repeatCount >> 8) & 0xff);
            _stream.SetFeature(dataBlob);

            this.Started = this.GetUnixTime();
            return true;
        }

        public static List<ViveControllerPlayer> GetPlayers()
        {
            List<ViveControllerPlayer> result = new List<ViveControllerPlayer>();

            var devices = DeviceList.Local.GetHidDevices();
            foreach (HidDevice controller in devices)
            {
                if(controller.VendorID != 0x28DE) //not Valve
                {
                    continue;
                }
                int pid = controller.ProductID;
                string name = controller.GetProductName();

                bool viveController = pid == 0x2012 && name.Equals("Valve");
                bool indexController = pid == 0x2300 && name.Equals("Controller");
                if (!viveController && !indexController)
                {
                    continue;
                }
                
                //Console.WriteLine("Found {0} {1:X}:{2:X}", controller.GetFriendlyName(), controller.VendorID, controller.ProductID);
                HidStream stream;
                bool success = controller.TryOpen(out stream);
                if (!success)
                {
                    Console.WriteLine("Couldn't open device");
                    continue;
                }
                /*byte[] vive_magic_power_on = { 0x00, 0x04, 0x78, 0x29, 0x38, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00,
                                               0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                               0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                               0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
                stream.SetFeature(vive_magic_power_on);*/

                ViveControllerPlayer player = new ViveControllerPlayer(stream);
                result.Add(player);
            }
            return result;
        }
    }
}
