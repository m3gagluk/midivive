using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valve.VR;

namespace MidiTest
{
    class OpenVRHelper
    {
        private OpenVRHelper()
        {

        }
        private static bool initialized = false;
        public static bool InitHMD()
        {
            if (initialized)
                return false; //Already initialized

            EVRInitError error = EVRInitError.None;
            CVRSystem vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Background);

            if (error != EVRInitError.None)
            {
                Console.WriteLine("OpenVR init error: " + error);
                return false;
            }
            //vrSystem.ResetSeatedZeroPose();

            {
                string filePath = string.Concat(Directory.GetCurrentDirectory(), "\\bindings\\actions.json");
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("File {0} doesn't exist!", filePath);
                    return false;
                }
                EVRInputError err = OpenVR.Input.SetActionManifestPath(filePath);
                if (err != EVRInputError.None)
                {
                    Console.WriteLine("OpenVR SetActionManifestPath error: {0}", err);
                    return false;
                }
            }

            handles = new ulong[2];
            string[] sources = { "/user/hand/left", "/user/hand/right" };
            for (int i = 0; i < 2; i++)
            {
                EVRInputError err = OpenVR.Input.GetInputSourceHandle(sources[i], ref handles[i]);
                if (err != EVRInputError.None)
                {
                    Console.WriteLine("OpenVR GetInputSourceHandle error: {0}", err);
                    return false;
                }
            }
            vibrationHandle = 0;
            {
                EVRInputError err = OpenVR.Input.GetActionHandle("/actions/default/out/Haptic", ref vibrationHandle);
                if (err != EVRInputError.None)
                {
                    Console.WriteLine("OpenVR GetActionHandle error: {0}", err);
                    return false;
                }
            }
            InitActionSet();
            int periodMS = 250;
            Timer timer = new Timer(Refresh, null, 0, periodMS);
            Refresh(null);
            //wait for the timer to start
            Thread.Sleep(periodMS);
            initialized = true;
            return true;
        }

        private static ulong[] handles;

        private static ulong vibrationHandle = 0;

        public static void PlayNote(int controller, float duration, float frequency, float volume)
        {
            if (!initialized)
                return;
            EVRInputError err = OpenVR.Input.TriggerHapticVibrationAction(vibrationHandle, 0, duration, frequency, volume, handles[controller]);
            Refresh(null);
            if (err != EVRInputError.None)
            {
                Console.WriteLine("OpenVR output error: {0}", err);
                return;
            }
        }

        private static VRActiveActionSet_t[] actionSet;

        private static uint actionSetSize = Convert.ToUInt32(Marshal.SizeOf(typeof(VRActiveActionSet_t)));

        private static void InitActionSet()
        {
            ulong handleLegacy = 0;
            OpenVR.Input.GetActionSetHandle("/actions/default", ref handleLegacy);
            actionSet = new VRActiveActionSet_t[1];
            actionSet[0] = new VRActiveActionSet_t
            {
                ulActionSet = handleLegacy,
                ulRestrictedToDevice = 0
            };
        }

        private static void Refresh(object state)
        {
            EVRInputError err = OpenVR.Input.UpdateActionState(actionSet, actionSetSize);
            if (err != EVRInputError.None)
            {
                Console.WriteLine("OpenVR UpdateActionState error: {0}", err);
            }
        }

        public static void Shutdown()
        {
            OpenVR.Shutdown();
        }
    }
}
