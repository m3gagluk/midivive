using Kolrabi.SteamController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiVive
{
    class SteamControllerPlayer : Player
    {
        private readonly SteamControllerDevice _controller;
        private readonly int _motor;

        private readonly double STEAM_CONTROLLER_MAGIC_PERIOD_RATIO = 495483.0;

        private SteamControllerPlayer(SteamControllerDevice controller, int motor)
        {
            _controller = controller;
            _motor = motor;
        }
        public override bool PlayNote(double frequency, double duration, float volume)
        {
            double period = 1.0 / frequency;
            int periodCommand = (int)(period * STEAM_CONTROLLER_MAGIC_PERIOD_RATIO);

            //Compute number of repeat. If duration < 0, set to maximum
            int repeatCount = (duration >= 0.0) ? (int)(duration / period) : 0x7FFF;

            _controller.TriggerHaptic((ushort)_motor, (ushort)periodCommand, (ushort)periodCommand, (ushort)repeatCount);
            this.Duration = duration;
            this.Started = this.GetUnixTime();
            return true;
        }

        public static List<SteamControllerPlayer> GetPlayers()
        {
            List<SteamControllerPlayer> result = new List<SteamControllerPlayer>();
            foreach (SteamControllerDevice controller in SteamControllerDevice.OpenControllers())
            {
                for(int motor = 0; motor < 2; motor++) {
                    result.Add(new SteamControllerPlayer(controller, motor));
                    controller.PlayMelody(1);
                }
            }
            return result;
        }
    }
}
