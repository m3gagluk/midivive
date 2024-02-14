using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiVive
{
    abstract class Player
    {
        protected long Started;
        protected double Duration;
        private float Tolerance;
        private bool DebugMode;
        
        public long GetTimeStarted()
        {
            return this.Started;
        }
        public bool GetDebug() {
            return this.DebugMode;
        }
        
        public float GetTolerance()
        {
            return Tolerance;
        }

        public void SetTolerance(float tolerance)
        {
            this.Tolerance = tolerance;
        }

        protected long GetUnixTime()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        }

        public bool IsBusy()
        {
            return GetUnixTime() - Started < Duration - Tolerance;
        }

        public abstract bool PlayNote(double frequency, double duration, float volume);

    }
}
