//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using UnityEngine;

namespace LookingGlass
{
    public static class Quilt
    {

        // classes
        [Serializable]
        public struct Settings
        {
            [Range(256, 8192)] public int quiltWidth;
            [Range(256, 8192)] public int quiltHeight;
            [Range(1, 32)] public int viewColumns;
            [Range(1, 32)] public int viewRows;
            [Range(1, 128)] public int numViews;
            [System.NonSerialized] public int viewWidth;
            [System.NonSerialized] public int viewHeight;
            [System.NonSerialized] public int paddingHorizontal;
            [System.NonSerialized] public int paddingVertical;
            [System.NonSerialized] public float viewPortionHorizontal;
            [System.NonSerialized] public float viewPortionVertical;
            [Tooltip("To use the default aspect for the current Looking Glass, keep at -1")]
            public float aspect;
            [Tooltip("If custom aspect differs from current Looking Glass aspect, " +
                "this will toggle between overscan (zoom w/ crop) or letterbox (black borders)")]
            public bool overscan;

            public Settings(int quiltWidth, int quiltHeight, int viewColumns, int viewRows,
                int numViews, float aspect = -1, bool overscan = false) : this()
            {
                this.quiltWidth = quiltWidth;
                this.quiltHeight = quiltHeight;
                // Debug.Log(quiltWidth + "," + quiltHeight + "," + viewColumns + "," + viewRows);
                if(viewColumns <= 0) viewColumns = 1;
                this.viewColumns = viewColumns;
                if(viewRows <= 0) viewRows = 1;
                this.viewRows = viewRows;
                this.numViews = numViews;
                this.aspect = aspect;
                this.overscan = overscan;
                Setup();
            }
            public void Setup()
            {
                if(viewColumns == 0 || viewRows == 0)
                {
                    // Debug.LogError("view columns and rows couldn't be 0");
                    // return;
                    viewWidth = quiltWidth;
                    viewHeight = quiltHeight;
                }
                else
                {
                    viewWidth = quiltWidth / viewColumns;
                    viewHeight = quiltHeight / viewRows;
                }

                // if(aspect == -1)
                // {
                //     aspect = viewWidth/(float)viewHeight;
                //     Debug.Log("assign aspect " + aspect);
                // }
                viewPortionHorizontal = (float)viewColumns * viewWidth / (float)quiltWidth;
                viewPortionVertical = (float)viewRows * viewHeight / (float)quiltHeight;
                paddingHorizontal = quiltWidth - viewColumns * viewWidth;
                paddingVertical = quiltHeight - viewRows * viewHeight;
            }

            public bool Equals(Settings otherSettings)
            {
                if (this.quiltWidth == otherSettings.quiltWidth
                    && this.quiltHeight == otherSettings.quiltHeight
                    && this.viewColumns == otherSettings.viewColumns
                    && this.viewRows == otherSettings.viewRows
                    && this.numViews == otherSettings.numViews
                    && this.aspect == otherSettings.aspect
                    && this.overscan == otherSettings.overscan
                    )
                    return true;
                return false;
            }
            // todo: have an override that only takes view count, width, and height
            // and creates as square as possible quilt settings from that
        }
        public enum Preset
        {
            Automatic = -1,
            Portrait = 0,
            HiResPortrait = 1,
            FourKStandard = 2,
            EightKStandard = 3,
            Custom = -2,
        }

        // variables
        public static readonly Settings[] presets = new Settings[] {
             new Settings(3360 , 3360 , 8, 6, 48),  // portrait
            new Settings(3840, 3840, 8, 6, 48),  // hi res portrait
            new Settings(4096, 4096, 5, 9, 45), // 4k standard
            new Settings(8192, 8192, 5, 9, 45), // 8k standard
            // new Settings(2048, 2048, 4, 8, 32), // standard
            // new Settings(7680, 6400, 6, 8, 48), // ultra hi
            // new Settings(1600, 1440, 4, 6, 24), // extra low
        };

        // functions
        public static Settings GetPreset(Preset preset, Calibration cal)
        {
            if (preset != Preset.Automatic) return presets[(int)preset];
            
            // making an exception here for portrait
            if (cal.IsPortrait)
            {
                return presets[(int)Preset.Portrait];
            }

            if (QualitySettings.lodBias > 1f)
            {               
                // making an exception here for higher res systems
                if (cal.screenWidth > 4000 && cal.screenHeight > 2000)
                {
                    return presets[(int)Preset.EightKStandard];
                }

                return presets[(int)Preset.FourKStandard];
            }
            if (QualitySettings.lodBias > 0.5f) return presets[(int)Preset.FourKStandard];
            return presets[(int)Preset.Portrait];
        }

        // get default quilt settings for a specific device
        public static Settings GetPreset(HoloplayDevice.Type emulatedDevice)
        {
            HoloplayDevice.Settings deviceSettings = HoloplayDevice.GetSettings(emulatedDevice);
            Quilt.Settings quiltSettings = presets[(int)deviceSettings.quiltPreset];
            // force device aspect ratio to be that of emulated device because it defaults to read from calibration
            quiltSettings.aspect = deviceSettings.aspectRatio;
            return quiltSettings;
        }
    }
}