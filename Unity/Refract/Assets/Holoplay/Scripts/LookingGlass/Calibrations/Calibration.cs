//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using UnityEngine;

namespace LookingGlass {
    [Serializable]
    public struct Calibration {
        public const int XPOS_DEFAULT = 0;
        public const int YPOS_DEFAULT = 0;
        public const string DEFAULT_NAME = "PORT";
        public const float DEFAULT_VIEWCONE = 35;

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
        public float rawSlope;
        public float flipImageX;

        public bool IsPortrait => string.IsNullOrEmpty(serial) || serial.Contains("PORT") || serial.Contains("Portrait");

        public Calibration(
            int index,
            int unityIndex,
            int screenWidth,
            int screenHeight,
            float subp,
            float viewCone,
            float aspect,
            float pitch,
            float slope,
            float center,
            float fringe,
            string serial,
            string LKGname,
            int xpos,
            int ypos,
            float rawSlope,
            float flipImageX) {

            this.index = index;
            this.unityIndex = unityIndex;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
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
            this.rawSlope = rawSlope;
            this.flipImageX = flipImageX;
        }

        public Calibration(int index) {
            this.index = index;

            HoloplayDevice.Type defaultType = HoloplayDevice.Type.Portrait;
            HoloplayDevice.Settings settings = HoloplayDevice.GetSettings(defaultType);
            this.screenWidth = settings.screenWidth;
            this.screenHeight = settings.screenHeight;
            this.subp = 0;
            this.viewCone = 0;
            this.aspect = this.screenWidth * 1f / this.screenHeight;
            this.pitch = 10;
            this.slope = 1;
            this.center = 0;
            this.fringe = 0;
            this.serial = DEFAULT_NAME;
            this.LKGname = "";
            this.unityIndex = 0;
            this.xpos = XPOS_DEFAULT;
            this.ypos = YPOS_DEFAULT;
            this.rawSlope = 0;
            this.flipImageX = 0;
        }

        public Calibration(int index, int screenWidth, int screenHeight) {
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
            this.rawSlope = 0;
            this.flipImageX = 0;
        }
    }
}