//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using UnityEngine;
namespace LookingGlass
{
    public class CalibrationManager
    {
        private static bool isInit = false;
        private static Calibration[] calibrations;

        public static int GetCalibrationCount(){
            if(!isInit){
                Debug.Log("calibration is not inited yet");
                return 0;
            }
            return calibrations.Length;
        }

        public static void Refresh()
        {
            // Debug.Log("init calibrations");
            calibrations = PluginCore.GetCalibrationArray();
            isInit = calibrations != null && calibrations.Length > 0;
        }

        public static Calibration GetCalibration(int index)
        {
            if (!isInit){
                // Debug.Log("[HoloPlay] Calibration is not inited yet");
                return new Calibration(0);
            }
            if (!isIndexValid(index)){
                Debug.Log("calibration index is unvalid");
                return new Calibration(0);
            }

            return calibrations[index];
        }
        public static bool isIndexValid(int index)  { return index >= 0 && isInit && index < calibrations.Length; }
    }

    public struct Calibration
    {
        public const int XPOS_DEFAULT = 0;
        public const int YPOS_DEFAULT = 0;
        public const string DEFAULT_NAME = "PORT";
        public const float DEFAULT_VIEWCONE = 35;


        public Calibration(int index, int unityIndex, int screenW, int screenH,
            float subp, float viewCone, float aspect, 
            float pitch, float slope, float center,
            float fringe, string serial, string LKGname,
            int xpos, int ypos)
        {
            this.index = index;
            this.unityIndex = unityIndex;
            this.screenWidth = screenW;
            this.screenHeight = screenH;
            this.subp = subp;
            this.viewCone = viewCone;
            this.aspect = aspect;
            this.pitch = pitch;
            this.slope = slope;
            this.center = center;
            this.fringe = fringe;
            this.serial = serial;
            this.LKGname = LKGname;
            this.xpos = xpos;
            this.ypos = ypos;
        }
        public Calibration(int index)
        {
            this.index = index;
            // Now default is portrait
            this.screenWidth = HoloplayDevice.GetSettings(HoloplayDevice.Type.Portrait).screenWidth;
            this.screenHeight = HoloplayDevice.GetSettings(HoloplayDevice.Type.Portrait).screenHeight;
            this.subp = 0;
            this.viewCone = 0;
            this.aspect = this.screenWidth *1f / this.screenHeight;
            this.pitch = 10;
            this.slope = 1;
            this.center = 0;
            this.fringe = 0;
            this.serial = DEFAULT_NAME;
            this.LKGname = "";
            this.unityIndex = 0;
            this.xpos = XPOS_DEFAULT;
            this.ypos = YPOS_DEFAULT;
        }

         public Calibration(int index, int screenWidth, int screenHeight)
        {
            this.index = index;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.subp = 0;
            this.viewCone = 0;
            this.aspect = screenWidth * 1f / screenHeight;
            this.pitch = 1;
            this.slope = 1;
            this.center = 0;
            this.fringe = 0;
            this.serial = DEFAULT_NAME;
            this.LKGname = "";
            this.unityIndex = 0;
            this.xpos = XPOS_DEFAULT;
            this.ypos = YPOS_DEFAULT;
        }

        public bool isPortrait()
        {
            return string.IsNullOrEmpty(serial) || serial.Contains("PORT") || serial.Contains("Portrait");
        }

        public int index;
        public int screenWidth;
        public int screenHeight;
        public float subp;
        public float viewCone;
        public float aspect;
        public float pitch;
        public float slope;
        public float center;
        public float fringe;
        public string serial;
        public string LKGname;
        public int unityIndex;

        public int xpos, ypos;
    }
}