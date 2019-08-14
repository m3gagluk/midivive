using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiVive
{
    class SteamControllerPlayer : Player
    {
        private readonly HidStream _stream;
        private readonly int _motor;

        private readonly double STEAM_CONTROLLER_MAGIC_PERIOD_RATIO = 495483.0;

        public SteamControllerPlayer(HidStream stream, int motor)
        {
            _stream = stream;
            _motor = motor;
        }
        public override bool PlayNote(double frequency, double duration, float volume)
        {
            double period = 1.0 / frequency;
            int periodCommand = (int)(period * STEAM_CONTROLLER_MAGIC_PERIOD_RATIO);
            //convert duration back to seconds
            duration = duration / 1000D;

            //Compute number of repeat. If duration < 0, set to maximum
            int repeatCount = (duration >= 0.0) ? (int)(duration / period) : 0x7FFF;

            byte[] dataBlob = {0x00, 0x8f,// STEAMCONTROLLER_TRIGGER_HAPTIC_PULSE
                                   0x07,
                                   0x00, //Trackpad select : 0x01 = left, 0x00 = right
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

            dataBlob[3] = (byte)_motor;
            dataBlob[4] = (byte)((periodCommand >> 0) & 0xff);
            dataBlob[5] = (byte)((periodCommand >> 8) & 0xff);
            dataBlob[6] = (byte)((periodCommand >> 0) & 0xff);
            dataBlob[7] = (byte)((periodCommand >> 8) & 0xff);
            dataBlob[8] = (byte)((repeatCount >> 0) & 0xff);
            dataBlob[9] = (byte)((repeatCount >> 8) & 0xff);
            _stream.SetFeature(dataBlob);

            this.Duration = duration;
            this.Started = this.GetUnixTime();
            return true;
        }

        public static List<SteamControllerPlayer> GetPlayers()
        {

            //0x28DE, 0x1102 Wired Steam Controller
            //0x28DE, 0x1142 Steam Controller dongle

            List<SteamControllerPlayer> result = new List<SteamControllerPlayer>();

            var devices = DeviceList.Local.GetHidDevices();
            foreach (HidDevice controller in devices)
            {
                if (controller.VendorID != 0x28DE) //not Valve
                {
                    continue;
                }
                if (controller.ProductID == 0x1102) //wired
                {
                    string name = controller.GetProductName();
                    if (!name.Equals("Valve")) //mouse or keyboard
                    {
                        continue;
                    }
                    Console.WriteLine("Found a wired controller");
                }
                else if (controller.ProductID == 0x1142)//wireless
                {
                    if (controller.GetMaxFeatureReportLength() != 65) //not an input device
                    {
                        continue;
                    }
                    //dongle creates 4 interfaces for 4 controllers, but I can't properly get the interface number
                    if (!controller.GetFileSystemName().Contains("mi_01")) //not thge first interface
                    {
                        continue;
                    }
                    Console.WriteLine("Found a wireless controller dongle, using the first paired controller");
                }
                else continue;

                bool success = controller.TryOpen(out HidStream stream);
                if (!success)
                {
                    Console.WriteLine("Couldn't open device");
                    continue;
                }

                for (int motor = 0; motor < 2; motor++)
                {
                    SteamControllerPlayer player = new SteamControllerPlayer(stream, motor);
                    result.Add(player);
                }
                
            }
            return result;
        }

        public void Shutdown()
        {
            this._stream.Close();
        }
    }
}
