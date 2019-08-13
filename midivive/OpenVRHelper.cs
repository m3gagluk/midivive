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
    class OpenVRHelper
    {
        private OpenVRHelper()
        {

        }

        public static string GetTrackedDeviceProperty(uint index, ETrackedDeviceProperty property)
        {


            ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
            uint len = OpenVR.System.GetStringTrackedDeviceProperty(index, property, null, 0, ref error);
            if (len > 1)
            {
                var result = new StringBuilder((int)len);
                OpenVR.System.GetStringTrackedDeviceProperty(index, property, result, len, ref error);
                return result.ToString();
            }
            return null;
        }
    }
}
