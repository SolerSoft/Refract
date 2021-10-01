//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.
using UnityEngine;
using UnityEditor;

namespace LookingGlass {

    public static class HoloplayDevice {
        public enum Type {
            Portrait = 0,
            FourK = 1,
            EightK = 2,          
            EightPointNineInchLegacy = 3,
        }

        [SerializeField]
        public struct Settings {
            public string name;
            public int screenWidth;
            public int screenHeight;
            public float aspectRatio;
            public float nearFlip;
            public Quilt.Preset quiltPreset;

            public Settings (string name, int screenWidth, int screenHeight, float nearClip, Quilt.Preset preset) : this() 
            {
                this.name = name;
                this.screenWidth = screenWidth;
                this.screenHeight = screenHeight;
                this.aspectRatio = screenWidth / (float)screenHeight;
                this.nearFlip = nearClip;
                this.quiltPreset = preset;
            }
        }

        public static readonly Settings[] presets = new Settings[] {
            new Settings( "Looking Glass - Portrait",  1536, 2048, 0.5f, Quilt.Preset.Portrait),
            new Settings( "Looking Glass - 4k",        3840, 2160, 1.5f, Quilt.Preset.FourKStandard),
            new Settings( "Looking Glass - 8K",        7680, 4320, 1.5f, Quilt.Preset.EightKStandard),
            new Settings( "Looking Glass - 8.9inch(Legacy)",   2560, 1600, 1.5f, Quilt.Preset.FourKStandard)
        };
        
        public static Settings GetSettings(Type preset) {
            return presets[(int)preset];
        }

        public static string GetName(Calibration cal)
        {
            foreach(Settings preset in presets)
            {
                if(cal.screenWidth == preset.screenWidth && cal.screenHeight == preset.screenHeight)
                {
                    return preset.name;
                }
            }
            // return default
            return presets[0].name;
        }

    }


}