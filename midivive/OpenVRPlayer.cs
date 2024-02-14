using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valve.VR;

namespace MidiVive
{
    class OpenVRPlayer : Player
    {
        private readonly ulong _deviceHandle;
        private OpenVRPlayer(ulong deviceHandle)
        {
            _deviceHandle = deviceHandle;
        }
        
        public override bool PlayNote(double frequency, double duration, float volume)
        {
            if (!_initialized)
                return false;
            //Console.WriteLine("PlayNote: action {0}, restrict {1}", vibrationHandle, DeviceHandles[controller]);
            EVRInputError err = OpenVR.Input.TriggerHapticVibrationAction(_hapticHandle, 0, (float)(duration/1000F), (float)frequency, volume, _deviceHandle);
            Refresh(null);
            if (err != EVRInputError.None)
            {
                Console.WriteLine("OpenVR output error: {0}", err);
                return false;
            }
            this.Duration = duration;
            this.Started = this.GetUnixTime();
            return true;
        }

        public static List<OpenVRPlayer> GetPlayers()
        {
            List<OpenVRPlayer> result = new List<OpenVRPlayer>();

            if (!_initialized && !Init())
                return result;
            
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                //ETrackedDeviceProperty.Prop_RegisteredDeviceType_String returns htc/vive_controllerLHR-XXXXXXX
                string path = OpenVRHelper.GetTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_RegisteredDeviceType_String);
                if (path == null)
                    continue;
                Console.WriteLine(i);
                Console.WriteLine(path);
                path = "/devices/" + path;
                Console.WriteLine("Found device {0} with path {1}", i, path);
                ulong handle = 0;
                EVRInputError err = OpenVR.Input.GetInputSourceHandle(path, ref handle);
                if (err != EVRInputError.None)
                {
                    Console.WriteLine("OpenVR GetInputSourceHandle error: {0}", err);
                    return result;
                }
                result.Add(new OpenVRPlayer(handle));
            }
            return result;
        }

        private static bool _initialized = false;
        private static ulong _hapticHandle = 0;

        public static bool Init()
        {
            if (_initialized)
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
            _hapticHandle = 0;
            {
                EVRInputError err = OpenVR.Input.GetActionHandle("/actions/default/out/haptic", ref _hapticHandle);
                if (err != EVRInputError.None)
                {
                    Console.WriteLine("OpenVR GetActionHandle error: {0}", err);
                    return false;
                }
            }

            InitActionSet();
            int periodMS = 500;
            Timer timer = new Timer(Refresh, null, 0, periodMS);
            Refresh(null);
            //wait for the timer to start
            Thread.Sleep(periodMS);
            _initialized = true;
            return true;
        }


        public static void Shutdown()
        {
            if (_initialized)
                OpenVR.Shutdown();
        }

        private static VRActiveActionSet_t[] _actionSet;

        private static uint _actionSetSize = Convert.ToUInt32(Marshal.SizeOf(typeof(VRActiveActionSet_t)));
        

        private static void Refresh(object state)
        {
            EVRInputError err = OpenVR.Input.UpdateActionState(_actionSet, _actionSetSize);
            if (err != EVRInputError.None)
            {
                Console.WriteLine("OpenVR UpdateActionState error: {0}", err);
            }
        }
        
        private static void InitActionSet()
        {
            ulong handleLegacy = 0;
            OpenVR.Input.GetActionSetHandle("/actions/default", ref handleLegacy);
            _actionSet = new VRActiveActionSet_t[1];
            _actionSet[0] = new VRActiveActionSet_t
            {
                ulActionSet = handleLegacy,
                ulRestrictedToDevice = 0
            };
        }
        
    }
}
