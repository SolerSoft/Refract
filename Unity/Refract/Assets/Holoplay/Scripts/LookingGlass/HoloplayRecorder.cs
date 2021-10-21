//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

// Based on MIT licensed FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/FFmpegOut

using UnityEngine;
using System.Collections;
using FFmpegOut;
using System.IO;

namespace LookingGlass
{
    [HelpURL("https://look.glass/unitydocs")]
    public sealed class HoloplayRecorder : MonoBehaviour
    {
        #region Public properties

        [SerializeField] string _outputName = "output";
        public string outputPath {
            get { return _outputName; }
            set { _outputName = value; }
        }

        [SerializeField] FFmpegPreset _preset = FFmpegPreset.VP8Default;

        public FFmpegPreset preset {
            get { return _preset; }
            set { _preset = value; }
        }

        [SerializeField] float _frameRate = 30;
        
        public float frameRate {
            get { return _frameRate; }
            set { _frameRate = value; }
        }

        [Tooltip("Megabits per second")]
        [SerializeField] int _targetBitrateInMegabits = 60;

        public int targetBitrateInMegabits {
            get { return _targetBitrateInMegabits; }
            set { _targetBitrateInMegabits = value; }
        }

        [Tooltip("CRF for VP8/VP9, CQ for NVENC")]
        [SerializeField] int _compression = 18;

        public int compression {
            get { return _compression; }
            set { _compression = value; }
        }

        public bool isOverwrite;
		public Quilt.Settings overwriteQuiltSettings =  Quilt.GetPreset(HoloplayDevice.Type.Portrait);// portrait
		public Quilt.Settings quiltSettings {
			get { 
                Quilt.Settings ret;

                if( isOverwrite ){
					ret = overwriteQuiltSettings;	
                }
                else{
                    ret = Holoplay.Instance.quiltSettings;
                }			
                return ret;
			} 
		}
        public float overwriteNearClipFactor = 0.5f;

        public enum RecorderState
        {
            NotRecording,
            Recording,
            Pausing
        }
        
        public RecorderState State{
            get{
                return _state;
            }
        }

        public string AutoCorrectPath
        {
            get
            {
                _outputName = string.IsNullOrEmpty(_outputName)? "output":_outputName;
                string outputPath = _outputName;
			    // float aspect = loadResults.calibrationFound? cal.aspect: quiltSettings.aspect;

                float aspect = isOverwrite? overwriteQuiltSettings.aspect: Holoplay.Instance.Aspect;
                string ending = "_qs" + quiltSettings.viewColumns + "x" + quiltSettings.viewRows + "a" + aspect;
                bool needEnding = !outputPath.Contains(ending);
                // remove to add later
                if(!needEnding)
                {
                    outputPath = outputPath.Replace(ending, "");
                }
                int startIndex = outputPath.Length;
                if (!outputPath.EndsWith(preset.GetSuffix()))
                {
                    outputPath = Path.ChangeExtension(outputPath, preset.GetSuffix());
                }
                return outputPath.Insert(outputPath.LastIndexOf('.'), ending);
            } 
        }

        #endregion

        #region Private members
        FFmpegSession _session;
        RecorderState _state;

        #endregion

        #region Time-keeping variables

        int _frameCount;
        float _startTime;
        float _pauseTime = 0;
        int _frameDropCount;

        float FrameTime {
            get { return _startTime + _pauseTime + (_frameCount - 0.5f) / _frameRate; }
        }

        void WarnFrameDrop()
        {
            if (++_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }

        #endregion

        #region MonoBehaviour implementation


        public void EndRecord()
        {
            _state = RecorderState.NotRecording;
            _pauseTime = 0;

            if (_session != null)
            {
                // Close and dispose the FFmpeg session.
                Debug.Log("Closing FFmpegSession after " + _frameCount + " frames.");

                _session.Close();
                _session.Dispose();
                _session = null;
            }

            if (GetComponent<FFmpegOut.FrameRateController>() == null) {
#if UNITY_2019_2_OR_NEWER
                Time.captureDeltaTime = oldCaptureDeltatime;
#else
                Time.captureFramerate = Mathf.RoundToInt(1f/oldCaptureDeltatime);
#endif                
                oldCaptureDeltatime = 0f;
            }

            if(isOverwrite)
            {
                RestoreHoloplayQuiltSize();
            }
        }

        float oldCaptureDeltatime;

        IEnumerator Start()
        {
            if (GetComponent<FFmpegOut.FrameRateController>() == null) {
#if UNITY_2019_2_OR_NEWER                
                oldCaptureDeltatime = Time.captureDeltaTime;
                Time.captureDeltaTime = 1.0f / frameRate; 
#else
                oldCaptureDeltatime = 1f / Time.captureFramerate;
                Time.captureFramerate = Mathf.RoundToInt(frameRate);
#endif                
            }

            // Sync with FFmpeg pipe thread at the end of every frame.
            for (var eof = new WaitForEndOfFrame();;)
            {
                yield return eof;
                if (_session != null)
                    _session.CompletePushFrames();
            }
        }

        void OnDisabled()
        {
            EndRecord();

//             if (GetComponent<FFmpegOut.FrameRateController>() == null) {
// #if UNITY_2019_2_OR_NEWER
//                 Time.captureDeltaTime = oldCaptureDeltatime;
// #else
//                 Time.captureFramerate = Mathf.RoundToInt(1f/oldCaptureDeltatime);
// #endif                
//                 oldCaptureDeltatime = 0f;
//             }
        }

        Quilt.Preset previousPreset;
        Quilt.Settings previousCustom;
        bool previousPreviewSettings;
        float previousAspect;
        float previousNearflip;
        bool quiltOverwrited = false;

        public void SetupHoloplayQuiltSize() {

            var holoplay = Holoplay.Instance;
            if (!holoplay)
            {
                Debug.LogWarning("[Holoplay] Failed to set up quilt settings because no Holoplay Capture instance exists"); 
                return;
            }

            if(!quiltOverwrited)
            {
                previousPreset = holoplay.GetQuiltPreset();
                previousCustom = holoplay.customQuiltSettings;
                previousPreviewSettings = holoplay.Preview2D;
                previousAspect = holoplay.cal.aspect;
                previousNearflip = holoplay.nearClipFactor;
            }
            quiltOverwrited = true;

            holoplay.SetQuiltPreset(Quilt.Preset.Custom);
            holoplay.customQuiltSettings = quiltSettings;
            holoplay.customQuiltSettings.Setup();
			holoplay.Preview2D = false;
            holoplay.SetupQuilt();
            holoplay.cal.aspect = quiltSettings.aspect;
            holoplay.nearClipFactor = overwriteNearClipFactor;
        }


        public void RestoreHoloplayQuiltSize() {

            var holoplay = Holoplay.Instance;
            if (!holoplay)
            {
                Debug.LogWarning("[Holoplay] Failed to restore quilt settings because no Holoplay Capture instance exists"); 
                return;
            }
            holoplay.Preview2D = previousPreviewSettings;
            holoplay.customQuiltSettings = previousCustom;
            holoplay.SetQuiltPreset(previousPreset);
            holoplay.cal.aspect = previousAspect;
            holoplay.nearClipFactor = previousNearflip;

            quiltOverwrited = false;
        }

        public void StartRecord()
        {
            // correct the extension if needed
            StartRecordWithPath( AutoCorrectPath );
        }

        public void StartRecordWithPath(string path)
        {
            var holoplay = Holoplay.Instance;
            if (!holoplay)
            {
                Debug.LogWarning("[Holoplay] Fail to start recorder because no HoloPlay Capture instance exists");
                return;
            }

            if (_session != null)
            {
                _session.Dispose();
            }

            if(isOverwrite)
            {
                SetupHoloplayQuiltSize();            
            }
            
            string fullpath = Path.GetFullPath(path);
            
            // Start an FFmpeg session.
            Debug.Log("creating FFmpeg session with size " 
                + holoplay.quiltRT.width + "x" + holoplay.quiltRT.height + ", will be saved at " + fullpath);

            string extraFfmpegOptions = "-b:v " + _targetBitrateInMegabits + "M";
            
#if !UNITY_EDITOR_OSX && !UNITY_STANDALONE_OSX  
            if(preset == FFmpegPreset.H264Nvidia || preset == FFmpegPreset.HevcNvidia){
                extraFfmpegOptions += " -cq:v " + _compression;
            } else{
                extraFfmpegOptions += " -crf " + _compression;
            }
#endif

            _session = FFmpegSession.CreateWithOutputPath(
                path,
                holoplay.quiltRT.width,
                holoplay.quiltRT.height,
                _frameRate, preset, extraFfmpegOptions 
            );

            _startTime = Time.time;
            _frameCount = 0;
            _frameDropCount = 0;

            _state = RecorderState.Recording;
        }

        public void PauseRecord()
        {
            if (_state == RecorderState.Recording)
                _state = RecorderState.Pausing;
            else
                Debug.LogWarning("[Holoplay] Can't pause recording when it's not started");
        }

        public void ResumeRecord()
        {
            if (_state == RecorderState.Pausing)
            {
                _state = RecorderState.Recording;
            }
            else
                Debug.LogWarning("[Holoplay] Can't resume recording when it's not paused");
        }

        void Update()
        {
            if (_state == RecorderState.NotRecording)
            {
                return;
            }

            var holoplay = Holoplay.Instance;
            if (!holoplay)
            {
                Debug.LogWarning("[HoloPlay] Fail to record because no HoloPlay Capture instance exists");
                return;
            }
            
            var gap = Time.time - FrameTime;
            var delta = 1 / _frameRate;

            if (gap < 0 || _state == RecorderState.Pausing)
            {
                // Update without frame data.
                _session.PushFrame(null);
            }
            else if (gap < delta)
            {
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                _session.PushFrame(holoplay.quiltRT);
                _frameCount++;
            }
            else if (gap < delta * 2)
            {
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme
                _session.PushFrame(holoplay.quiltRT);
                _session.PushFrame(holoplay.quiltRT);
                _frameCount += 2;
            }
            else
            {
                // Show a warning message about the situation.
                WarnFrameDrop();

                // Push the current frame to FFmpeg.
                _session.PushFrame(holoplay.quiltRT);

                // Compensate the time delay.
                _frameCount += Mathf.FloorToInt(gap * _frameRate);
            }
        }

        #endregion
    }
}
