//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace LookingGlass {
	/// <summary>
	/// The callback signature used for setting up post-processing, if it is present.
	/// </summary>
	/// <param name="behaviour">The <see cref="Holoplay"/> component in the scene.</param>
	internal delegate HoloplayPostProcessSetupData HoloplayPostProcessSetup(Holoplay behaviour);

	/// <summary>
	/// The callback signature used for disposing of post-processing, if it is present.
	/// </summary>
	/// <param name="behaviour">The <see cref="Holoplay"/> component in the scene.</param>
	internal delegate void HoloplayPostProcessDispose(Holoplay behaviour);

    [ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
    [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/Holoplay/")]
	public class Holoplay : MonoBehaviour {
		public enum DisplayTarget {
			Display1 = 0,
			Display2,
			Display3,
			Display4,
			Display5,
			Display6,
			Display7,
			Display8,
		}

		// singleton
		private static Holoplay instance;

		/// <summary>
		/// The most-recently enabled <see cref="Holoplay"/> component, or <c>null</c> if there is none.
		/// </summary>
		public static Holoplay Instance { 
			get { 
				if (instance == null)
					instance = FindObjectOfType<Holoplay>();
				return instance;
			} 
		}

#if UNITY_POST_PROCESSING_STACK_V2
		internal static HoloplayPostProcessSetup postProcessSetup;
		internal static HoloplayPostProcessDispose postProcessDispose;
#endif

		internal const string SingleViewCameraName = "Single-View Camera";
		private const string FinalScreenBlitterCameraName = "Final Screen Camera";

		// info
		public static readonly Version version = new Version(1,4,3);
		public const string versionLabel = "";

		//NOTE: We are NOT trying to hide this top tab bar anymore, but instead, just shift our pixels and adjust our aspect ratio as needed.
		/// <summary>
		/// In Unity 2020.3+, the top bar of the Game View cannot be hidden, and is difficult to detect through the scripting API.<br />
		/// To make sure the Game View preview is working regardless, we need to subtract <see cref="EditorWindowTabSize"/> from the rendering height.
		/// </summary>
		internal const int EditorWindowTabSize = 21;

		// camera
		public CameraClearFlags clearFlags = CameraClearFlags.Color;
		public Color background = Color.black;
		public LayerMask cullingMask = -1;
		public float size = 5f;
		public float depth;
		public RenderingPath renderingPath = RenderingPath.UsePlayerSettings;
		public bool occlusionCulling = true;
		public bool allowHDR = true;
		public bool allowMSAA = true;
#if UNITY_2017_3_OR_NEWER
		public bool allowDynamicResolution = false;
#endif

		[Tooltip("WARNING: This field is temporarily not used.")]
		public int targetDisplay;

		private string lkgName;
		[NonSerialized] public int targetLKG;

		[SerializeField] private bool preview2D = false;
		[NonSerialized] private bool hadPreview2D = false; //Used for detecting changes in the editor

		public HoloplayDevice.Type emulatedDevice = HoloplayDevice.Type.Portrait;
		[Range(5f, 90f)] public float fov = 14f;
		[Range(0.01f, 5f)] public float nearClipFactor = HoloplayDevice.GetSettings(HoloplayDevice.Type.Portrait).nearFlip;
		[Range(0.01f, 40f)] public float farClipFactor = 4f;
		public bool scaleFollowsSize;
		[Range(0f, 1f)] public float viewconeModifier = 1f;
		[Range(0f, 1f)] public float centerOffset;
		[Range(-90f, 90f)] public float horizontalFrustumOffset;
		[Range(-90f, 90f)] public float verticalFrustumOffset;
		public bool useFrustumTarget;
		public Transform frustumTarget;
		
		// quilt
		public Quilt.Preset quiltPreset = Quilt.Preset.Automatic;
		public Quilt.Preset GetQuiltPreset() { return quiltPreset; }
		public void SetQuiltPreset(Quilt.Preset preset) {
			quiltPreset = preset;
			SetupQuilt();
		}
		public Quilt.Settings quiltSettings {
			get { 
				if (quiltPreset == Quilt.Preset.Custom)
					return customQuiltSettings;	
				else
					return Quilt.GetPreset(quiltPreset, cal); 
			} 
		}

		public Quilt.Settings customQuiltSettings = Quilt.GetPreset(HoloplayDevice.Type.Portrait); // portrait
		public KeyCode screenshot2DKey = KeyCode.F9;
        public KeyCode screenshotQuiltKey = KeyCode.F10;
		public Texture overrideQuilt;
		public bool renderOverrideBehind;
		public RenderTexture quiltRT;
		[NonSerialized] public Material lightfieldMat;

		// gizmo
		public Color frustumColor = new Color32(0, 255, 0, 255);
		public Color middlePlaneColor = new Color32(150, 50, 255, 255);
		public Color handleColor = new Color32(75, 100, 255, 255);
		public bool drawHandles = true;
		private float[] cornerDists = new float[3];
		private Vector3[] frustumCorners = new Vector3[12];

		// events
		[Tooltip("If you have any functions that rely on the calibration having been loaded " +
			"and the screen size having been set, let them trigger here")]
		public LoadEvent onHoloplayReady;
		[NonSerialized] public LoadResults loadResults;
		[Tooltip("Will fire before each individual view is rendered. " +
			"Passes [0, numViews), then fires once more passing numViews (in case cleanup is needed)")]
		public ViewRenderEvent onViewRender;
		[Serializable]
		public class ViewRenderEvent : UnityEvent<Holoplay, int> {};

		// optimization
		public enum ViewInterpolationType {
			None,
			EveryOther,
			Every4th,
			Every8th,
			_4Views,
			_2Views
		}
		public ViewInterpolationType viewInterpolation = ViewInterpolationType.None;
		public int ViewInterpolation {
			get {
				switch (viewInterpolation) {
					case ViewInterpolationType.None:
					default:
						return 1;
					case ViewInterpolationType.EveryOther:
						return 2;
					case ViewInterpolationType.Every4th:
						return 4;
					case ViewInterpolationType.Every8th:
						return 8;
					case ViewInterpolationType._4Views:
						return quiltSettings.numViews / 3;
					case ViewInterpolationType._2Views:
						return quiltSettings.numViews;
				}
			}
		}

		public bool reduceFlicker;
		public bool fillGaps;
		public bool blendViews;
		private ComputeShader interpolationComputeShader = null;

		private Camera singleViewCamera;
		private Camera postProcessCamera;
		private Camera finalScreenCamera;
		private HoloplayScreenBlitter screenBlitter;

		// not in inspector
		[NonSerialized] public Calibration cal;
		[NonSerialized] public float camDist;

		private bool frameRendered;
		private bool debugInfo;

		private RenderTexture preview2DRT;

		public int ScreenWidth => loadResults.calibrationFound ? cal.screenWidth: HoloplayDevice.GetSettings(emulatedDevice).screenWidth;
		public int ScreenHeight => loadResults.calibrationFound ? cal.screenHeight: HoloplayDevice.GetSettings(emulatedDevice).screenHeight;

		public float Aspect {
			get {
#if UNITY_2020_3_OR_NEWER && UNITY_EDITOR
				if (loadResults.calibrationFound)
					return cal.screenWidth / (float) (cal.screenHeight - EditorWindowTabSize);
#endif
				return loadResults.calibrationFound ? 
					cal.aspect : 
					HoloplayDevice.GetSettings(emulatedDevice).aspectRatio; 
			}
		}

		public string DeviceTypeName => loadResults.calibrationFound ? HoloplayDevice.GetName(cal) : HoloplayDevice.GetSettings(emulatedDevice).name;
		public string LKGName {
			get { return lkgName; }
			set {
				lkgName = value;
				ReloadCalibration();
			}
		}

		public bool Preview2D {
			get { return preview2D; }
			set {
				hadPreview2D = preview2D = value;

				//If we need anything to change immediately when setting Preview2D, we can do that here
			}
		}

		//How the cameras work:
		//1. The finalScreenCamera begins rendering automatically, since it is enabled.
		//2. The singleViewCamera renders into RenderTextures,
		//		either for rendering the quilt, or the 2D preview.
		//3. Then, the postProcessCamera is set to render no Meshes, and discards its own RenderTexture source.
		//		INSTEAD, it takes a RenderTexture (quiltRT) from Holoplay.cs and blits it with the lightfield shader back into the RenderTexture.
		//4. Finally, the finalScreenCamera blits the result ONTO THE SCREEN.(A camera required for that), since its targetTexture is always null.

		/// <summary>
		/// <para>Renders individual views of the scene, where each view may be composited into the <see cref="Holoplay"/> quilt.</para>
		/// <para>When in 2D preview mode, only 1 view is rendered directly to the screen.</para>
		/// <para>This camera is not directly used for rendering to the screen. The results of its renders are used as intermediate steps in the rendering process.</para>
		/// </summary>
		public Camera SingleViewCamera => singleViewCamera;

		/// <summary>
		/// <para>The <see cref="Camera"/> used apply final post-processing to a single view of the scene, or a quilt of the scene.</para>
		/// <para>This camera is not directly used for rendering to the screen. It is only used for applying graphical changes in internal <see cref="RenderTexture"/>s.</para>
		/// </summary>
		public Camera PostProcessCamera => postProcessCamera;

		/// <summary>
		/// The camera used for blitting the final <see cref="RenderTexture"/> to the screen.<br />
		/// In Unity, the easiest and best-supported way to do this is by using a Camera directly.
		/// </summary>
		internal Camera FinalScreenCamera => finalScreenCamera;
		internal HoloplayScreenBlitter ScreenBlitter => screenBlitter;

		public RenderTexture Preview2DRT => preview2DRT;

#region Unity Messages
        private void OnValidate() {
			// make sure size can't go negative
			// using this here instead of [Range()] attribute because size shouldn't need a slider
			size = Mathf.Max(0.01f, size);

			if (preview2D != hadPreview2D)
				Preview2D = preview2D;
		}

		private void OnEnable() {
#if !UNITY_2018_1_OR_NEWER || !UNITY_EDITOR
			PluginCore.Reset();
#endif

#if UNITY_POST_PROCESSING_STACK_V2
			HoloplayPostProcessSetupData data;
			if (postProcessSetup != null && (data = postProcessSetup(this)).camera != null) {
				postProcessCamera = data.postProcessCamera;
				singleViewCamera = data.camera;
			} else
#endif
			{
				singleViewCamera = GetComponent<Camera>();
				singleViewCamera.hideFlags = HideFlags.HideInInspector;
			}
			//NOTE: Only the finalScreenCamera is set with enabled = true, because it's the only camera here meant to write to the screen.
			//Thus, its targetTexture is null, and it's enabled to call OnRenderImage(...) and write each frame to the screen.
			//These other cameras are just for rendering intermediate results.
			singleViewCamera.enabled = false;
			if (postProcessCamera != null)
				postProcessCamera.enabled = false;

			lightfieldMat = new Material(Shader.Find("Holoplay/Lightfield"));
			instance = this;

			finalScreenCamera = new GameObject(FinalScreenBlitterCameraName).AddComponent<Camera>();
			finalScreenCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
			finalScreenCamera.transform.SetParent(transform);

			screenBlitter = finalScreenCamera.gameObject.AddComponent<HoloplayScreenBlitter>();
			screenBlitter.holoplay = this;

#if UNITY_2017_3_OR_NEWER
			finalScreenCamera.allowDynamicResolution = false;
#endif
			finalScreenCamera.allowHDR = false;
			finalScreenCamera.allowMSAA = false;
			finalScreenCamera.cullingMask = 0;
			finalScreenCamera.clearFlags = CameraClearFlags.Nothing;

			Preview2D = preview2D;

			ReloadCalibration();

			if (!Application.isEditor) {
				//NOTE: This is REQUIRED for using Display.SetParams(...)!
				//See Unity docs on this at: https://docs.unity3d.com/ScriptReference/Display.SetParams.html

				//NOTE: WITHOUT this line, subsequent calls to Display.displays[0].SetParams(...) HAVE NO EFFECT!
				Display.displays[0].Activate();
#if UNITY_STANDALONE_WIN
				Display.displays[0].SetParams(cal.screenWidth, cal.screenHeight, cal.xpos, cal.ypos);
#endif
			}

			//This sets up the window to play on the looking glass,
			//NOTE: This must be executed after display reposition
			//YAY! This FIXED the issue with cal.screenHeight or 0 as the SetParams height making the window only go about half way down the screen!
			//This also lets the lenticular shader render properly!
			Screen.SetResolution(cal.screenWidth, cal.screenHeight, true);

			SetupQuilt();

			onHoloplayReady?.Invoke(loadResults);
		}

		private void OnDisable() {
			if (lightfieldMat != null)
				DestroyImmediate(lightfieldMat);
			if (RenderTexture.active == quiltRT) 
				RenderTexture.active = null;
			if (quiltRT != null) 
				DestroyImmediate(quiltRT);
			if (preview2DRT != null)
				DestroyImmediate(preview2DRT);
			if (finalScreenCamera != null)
				DestroyImmediate(finalScreenCamera.gameObject);
				
#if UNITY_POST_PROCESSING_STACK_V2
			postProcessDispose?.Invoke(this);
#endif


#if !UNITY_2018_1_OR_NEWER || !UNITY_EDITOR
			PluginCore.Reset();
#endif
		}

		private void OnDestroy() {
#if !UNITY_2018_1_OR_NEWER || !UNITY_EDITOR
			PluginCore.Reset();
#endif
		}

        private void Update() {
            frameRendered = false;

            if (Input.GetKeyDown(screenshot2DKey)) {
                Camera screenshotCamera = singleViewCamera;

                int width = Screen.width;
                int height = Screen.height;
                RenderTexture render = RenderTexture.GetTemporary(width, height, 24);
                RenderTexture depth = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Depth);
                RenderTexture previousTarget = screenshotCamera.targetTexture;

                try {
                    screenshotCamera.SetTargetBuffers(render.colorBuffer, depth.depthBuffer);
                    screenshotCamera.Render();
                    RunPostProcess(render, depth);

                    SaveAsPNGScreenshot(render);
                } finally {
                    screenshotCamera.targetTexture = previousTarget;
                    RenderTexture.ReleaseTemporary(render);
                    RenderTexture.ReleaseTemporary(depth);
                }
            }

            if (Input.GetKeyDown(screenshotQuiltKey)) {
                SaveAsPNGScreenshot(quiltRT);
            }

            // debug info
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.F8))
                debugInfo = !debugInfo;
            if (Input.GetKeyDown(KeyCode.Escape))
                debugInfo = false;
        }

        private void LateUpdate() {
            camDist = ResetCamera();
        }

        private void OnGUI() {
			if (debugInfo) {
				Color previousColor = GUI.color;

				// start drawing stuff
				int unitDiv = 20;
				int unit = Mathf.Min(Screen.width, Screen.height) / unitDiv;
				Rect rect = new Rect(unit, unit, unit*(unitDiv-2), unit*(unitDiv-2));

				GUI.color = Color.black;
				GUI.DrawTexture(rect, Texture2D.whiteTexture);
				rect = new Rect(unit*2, unit*2, unit*(unitDiv-4), unit*(unitDiv-4));

				GUILayout.BeginArea(rect);
				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.fontSize = unit;
				GUI.color = new Color(0.5f, 0.8f, 0.5f, 1);

				GUILayout.Label("Holoplay SDK " + version.ToString() + versionLabel, labelStyle);
				GUILayout.Space(unit);
				GUI.color = loadResults.calibrationFound ? new Color(0.5f, 1, 0.5f) : new Color(1, 0.5f, 0.5f);
				GUILayout.Label("calibration: " + (loadResults.calibrationFound ? "loaded" : "not found"), labelStyle);

				//TODO: This is giving a false positive currently
				//GUILayout.Space(unit);
				//GUI.color = new Color(0.5f, 0.5f, 0.5f, 1);
				//GUILayout.Label("lkg display: " + (loadResults.lkgDisplayFound ? "found" : "not found"), labelStyle);

				GUILayout.EndArea();

				GUI.color = previousColor;
			}
		}

		private void OnDrawGizmos() {
#if UNITY_EDITOR
			// Ensure continuous Update calls. thanks to https://forum.unity.com/threads/solved-how-to-force-update-in-edit-mode.561436/
			if (!Application.isPlaying)
			{
				UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
				UnityEditor.SceneView.RepaintAll();
			}
#endif

			Gizmos.color = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
				frustumColor.gamma : frustumColor;
			// float focalDist = size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
			float focalDist = GetCamDistance();
			cornerDists[0] = focalDist;
			cornerDists[1] = singleViewCamera.nearClipPlane;
			cornerDists[2] = singleViewCamera.farClipPlane;
			for (int i = 0; i < cornerDists.Length; i++) {
				float dist = cornerDists[i];
				int offset = i * 4;
                frustumCorners[offset+0] = singleViewCamera.ViewportToWorldPoint(new Vector3(0, 0, dist));
                frustumCorners[offset+1] = singleViewCamera.ViewportToWorldPoint(new Vector3(0, 1, dist));
                frustumCorners[offset+2] = singleViewCamera.ViewportToWorldPoint(new Vector3(1, 1, dist));
                frustumCorners[offset+3] = singleViewCamera.ViewportToWorldPoint(new Vector3(1, 0, dist));
				// draw each square
				for (int j = 0; j < 4; j++) {
					Vector3 start = frustumCorners[offset+j];
					Vector3 end = frustumCorners[offset+(j+1)%4];
					if (i > 0) {
						// draw a normal line for front and back
						Gizmos.color = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
							frustumColor.gamma : frustumColor;
						Gizmos.DrawLine(start, end);
					} else {
						// draw a broken, target style frame for focal plane
						Gizmos.color = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
							middlePlaneColor.gamma : middlePlaneColor;
						Gizmos.DrawLine(start, Vector3.Lerp(start, end, 0.333f));
						Gizmos.DrawLine(end, Vector3.Lerp(end, start, 0.333f));
					}
				}
			}
			// connect them
			for (int i = 0; i < 4; i++)
				Gizmos.DrawLine(frustumCorners[4+i], frustumCorners[8+i]);
		}

		private void OnApplicationQuit() {
#if !UNITY_2018_1_OR_NEWER || !UNITY_EDITOR
			PluginCore.Reset();
#endif
		}
#endregion

		public void RenderQuilt(bool forceRender = false) {
			if (!forceRender && frameRendered)
				return;
			frameRendered = true;

			// pass the calibration values to lightfield material
			PassSettingsToMaterial(lightfieldMat);

			// set up camera
			float aspect = Aspect;
			
			Matrix4x4 centerViewMatrix = singleViewCamera.worldToCameraMatrix;
			Matrix4x4 centerProjMatrix = singleViewCamera.projectionMatrix;

			depth = Mathf.Clamp(depth, -100, 100);
			singleViewCamera.depth = finalScreenCamera.depth = depth;

			bool skip = false;

			// override quilt
			bool hasOverrideQuilt = overrideQuilt;
			if (hasOverrideQuilt) {
				Graphics.Blit(overrideQuilt, quiltRT);
				// if only rendering override, exit here
				if (!renderOverrideBehind) {
					singleViewCamera.enabled = false;
					finalScreenCamera.enabled = true;
					PassSettingsToMaterial(lightfieldMat);
					skip = true;
				}
			}

			if (!skip) {
				float viewCone = Application.isPlaying && cal.viewCone == 0 ? Calibration.DEFAULT_VIEWCONE : cal.viewCone;
				float viewConeSweep = -camDist * Mathf.Tan(viewCone * viewconeModifier * Mathf.Deg2Rad);

				// projection matrices must be modified in terms of focal plane size
				float projModifier = 1 / (size * singleViewCamera.aspect);
				// fov trick to keep shadows from disappearing
				singleViewCamera.fieldOfView = 135;

				// optimization viewinterp
				RenderTexture viewRT = null;
				RenderTexture viewRTDepth = null;
				RenderTexture quiltRTDepth = null;

				RenderTextureDescriptor depthDescriptor = quiltRT.descriptor;
				depthDescriptor.colorFormat = RenderTextureFormat.RFloat;
				quiltRTDepth = RenderTexture.GetTemporary(depthDescriptor);
				quiltRTDepth.Create();

				// clear the textures as well
				if (!hasOverrideQuilt) {
					RenderTexture.active = quiltRT;
					GL.Clear(true, true, background, 0);
				}

				RenderTexture.active = quiltRTDepth;
				GL.Clear(true, true, Color.black, 2); //TODO: Test if this is the same as clearing with depth = 1! It's supposed to be in range [0, 1]!
				RenderTexture.active = null;

#if UNITY_POST_PROCESSING_STACK_V2
				bool hasPPCam = postProcessCamera != null;
				if (hasPPCam)
					postProcessCamera.CopyFrom(singleViewCamera);
#endif

				// render the views
				for (int i = 0; i < quiltSettings.numViews; i++) {
					if (i % ViewInterpolation != 0 && i != quiltSettings.numViews - 1)
						continue;

					onViewRender?.Invoke(this, i);

					viewRT = RenderTexture.GetTemporary(quiltSettings.viewWidth, quiltSettings.viewHeight, 24);
					viewRTDepth = RenderTexture.GetTemporary(quiltSettings.viewWidth, quiltSettings.viewHeight, 24, RenderTextureFormat.Depth);

					singleViewCamera.SetTargetBuffers(viewRT.colorBuffer, viewRTDepth.depthBuffer);
					singleViewCamera.aspect = aspect;

					// move the camera
					Matrix4x4 viewMatrix = centerViewMatrix;
					Matrix4x4 projMatrix = centerProjMatrix;

					float currentViewLerp = 0f; // if numviews is 1, take center view
					if (quiltSettings.numViews > 1)
						currentViewLerp = (float) i / (quiltSettings.numViews - 1) - 0.5f;

					viewMatrix.m03 += currentViewLerp * viewConeSweep;
					projMatrix.m02 += currentViewLerp * viewConeSweep * projModifier;
					singleViewCamera.worldToCameraMatrix = viewMatrix;
					singleViewCamera.projectionMatrix = projMatrix;

					singleViewCamera.Render();
					CopyViewToQuilt(i, viewRT, quiltRT);

					// gotta create a weird new viewRT now
					RenderTextureDescriptor viewRTRFloatDesc = viewRT.descriptor;
					viewRTRFloatDesc.colorFormat = RenderTextureFormat.RFloat;
					RenderTexture viewRTRFloat = RenderTexture.GetTemporary(viewRTRFloatDesc);
					Graphics.Blit(viewRTDepth, viewRTRFloat);

					CopyViewToQuilt(i, viewRTRFloat, quiltRTDepth);

					singleViewCamera.targetTexture = null;
					RenderTexture.ReleaseTemporary(viewRT);
					RenderTexture.ReleaseTemporary(viewRTDepth);
					RenderTexture.ReleaseTemporary(viewRTRFloat);

					// this helps 3D cursor ReadPixels faster
					GL.Flush();
				}
				// onViewRender final pass
				onViewRender?.Invoke(this, quiltSettings.numViews);

				// reset to center view
				singleViewCamera.worldToCameraMatrix = centerViewMatrix;
				singleViewCamera.projectionMatrix = centerProjMatrix;
				// not really necessary, but keeps gizmo from looking messed up sometimes
				singleViewCamera.aspect = aspect;
				// reset fov after fov trick
				singleViewCamera.fieldOfView = fov;
				// if interpolation is happening, release
				if (ViewInterpolation > 1) {
					// todo: interpolate on the quilt itself
					InterpolateViewsOnQuilt(quiltRTDepth);
				}

#if UNITY_POST_PROCESSING_STACK_V2
				if (hasPPCam) {
#if !UNITY_2018_1_OR_NEWER
				if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
					SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
				{
					FlipRenderTexture(quiltRT);
				}
#endif
					RunPostProcess(quiltRT, quiltRTDepth);
				}
#endif
				var simpleDof = GetComponent<SimpleDOF>();
				if (simpleDof != null && simpleDof.enabled) {
					simpleDof.DoDOF(quiltRT, quiltRTDepth);
				}
				RenderTexture.ReleaseTemporary(quiltRTDepth);
			}
        }

        public void RenderPreview2D() {
			Profiler.BeginSample(nameof(RenderPreview2D), this);
			try {
				Profiler.BeginSample("Create " + nameof(RenderTexture) + "s", this);
				int width = ScreenWidth;
				int height = ScreenHeight;
				if (preview2DRT == null
					|| preview2DRT.width != width
					|| preview2DRT.height != height) {
					if (preview2DRT != null)
						Destroy(preview2DRT);
					preview2DRT = new RenderTexture(width, height, 24);
				}
				RenderTexture depth = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Depth);
				Profiler.EndSample();

				Profiler.BeginSample("Rendering", this);
				try {
					singleViewCamera.SetTargetBuffers(preview2DRT.colorBuffer, depth.depthBuffer);
					singleViewCamera.Render();

#if UNITY_POST_PROCESSING_STACK_V2
					bool hasPPCam = postProcessCamera != null;
					if (hasPPCam) {
						postProcessCamera.CopyFrom(singleViewCamera);
#if !UNITY_2018_1_OR_NEWER
				if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
					SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
					FlipRenderTexture(preview2DRT);
#endif
						RunPostProcess(preview2DRT, depth);
					}
#endif
				} finally {
					RenderTexture.ReleaseTemporary(depth);
					Profiler.EndSample();
                }
			} finally {
				Profiler.EndSample();
            }
		}

		/// <summary>
		/// Applies post-processing effects to the <paramref name="target"/> texture.<br />
		/// Note that this method does NOT draw anything to the screen. It only writes into the <paramref name="target"/> render texture.
		/// </summary>
		/// <param name="target">The render texture to apply post-processing into.</param>
		/// <param name="depthTexture">The depth texture to use for post-processing effects. This is useful, because you can provide a custom depth texture instead of always using a single <see cref="Camera"/>'s depth texture.</param>
		private void RunPostProcess(RenderTexture target, RenderTexture depthTexture) {
            postProcessCamera.cullingMask = 0;
            postProcessCamera.clearFlags = CameraClearFlags.Nothing;
            postProcessCamera.targetTexture = target;

            Shader.SetGlobalTexture("_FAKEDepthTexture", depthTexture);
            postProcessCamera.Render();
        }

		public void CopyViewToQuilt(int view, RenderTexture viewRT, RenderTexture quiltRT, bool forceDrawTex = false) {
			// note: not using graphics.copytexture because it does not honor alpha
			// reverse view because Y is taken from the top
			int x = (view % quiltSettings.viewColumns) * quiltSettings.viewWidth;
			if (SystemInfo.copyTextureSupport != CopyTextureSupport.None && !forceDrawTex) {
				int y = (view / quiltSettings.viewColumns) * quiltSettings.viewHeight;
				Graphics.CopyTexture(
					viewRT, 0, 0,	// src, srcElement, srcMip
					0, 0,	// srcX, srcY
					quiltSettings.viewWidth,	// srcWidth 
					quiltSettings.viewHeight,	// srcHeight
					quiltRT, 0, 0,	// dst, dstElement, dstMip
					x, y	// dstX, dstY
				);
			} else {
				int ri = quiltSettings.viewColumns * quiltSettings.viewRows - view - 1;
				int ry = (ri / quiltSettings.viewColumns) * quiltSettings.viewHeight;
				// again owing to the reverse Y
				Rect rtRect = new Rect(x, ry + quiltSettings.paddingVertical, quiltSettings.viewWidth, quiltSettings.viewHeight);
				Graphics.SetRenderTarget(quiltRT);
				GL.PushMatrix();
				GL.LoadPixelMatrix(0, (int)quiltSettings.quiltWidth, (int)quiltSettings.quiltHeight, 0);
				Graphics.DrawTexture(rtRect, viewRT);
				GL.PopMatrix();
				Graphics.SetRenderTarget(null);
			}
		}

		public void FlipRenderTexture(RenderTexture rt) {
			RenderTexture rtTemp = RenderTexture.GetTemporary(rt.descriptor);
			rtTemp.Create();
			Graphics.CopyTexture(rt, rtTemp);
			Graphics.SetRenderTarget(rt);
			Rect rtRect = new Rect(0, 0, rt.width, rt.height);
			GL.PushMatrix();
			GL.LoadPixelMatrix(0, quiltSettings.quiltWidth, 0, quiltSettings.quiltHeight);
			Graphics.DrawTexture(rtRect, rtTemp);
			GL.PopMatrix();
			Graphics.SetRenderTarget(null);
			RenderTexture.ReleaseTemporary(rtTemp);
		}

		public void InterpolateViewsOnQuilt(RenderTexture quiltRTDepth) {
			if (interpolationComputeShader == null)
				interpolationComputeShader = Resources.Load<ComputeShader>("ViewInterpolation");

			int kernelFwd = interpolationComputeShader.FindKernel("QuiltInterpolationForward");
			int kernelBack = blendViews ? 
				interpolationComputeShader.FindKernel("QuiltInterpolationBackBlend") :
				interpolationComputeShader.FindKernel("QuiltInterpolationBack");
			int kernelFwdFlicker = interpolationComputeShader.FindKernel("QuiltInterpolationForwardFlicker");
			int kernelBackFlicker = blendViews ? 
				interpolationComputeShader.FindKernel("QuiltInterpolationBackBlendFlicker") :
				interpolationComputeShader.FindKernel("QuiltInterpolationBackFlicker");
			interpolationComputeShader.SetTexture(kernelFwd, "Result", quiltRT);
			interpolationComputeShader.SetTexture(kernelFwd, "ResultDepth", quiltRTDepth);
			interpolationComputeShader.SetTexture(kernelBack, "Result", quiltRT);
			interpolationComputeShader.SetTexture(kernelBack, "ResultDepth", quiltRTDepth);
			interpolationComputeShader.SetTexture(kernelFwdFlicker, "Result", quiltRT);
			interpolationComputeShader.SetTexture(kernelFwdFlicker, "ResultDepth", quiltRTDepth);
			interpolationComputeShader.SetTexture(kernelBackFlicker, "Result", quiltRT);
			interpolationComputeShader.SetTexture(kernelBackFlicker, "ResultDepth", quiltRTDepth);
			interpolationComputeShader.SetFloat("_NearClip", singleViewCamera.nearClipPlane);
			interpolationComputeShader.SetFloat("_FarClip", singleViewCamera.farClipPlane);
			interpolationComputeShader.SetFloat("focalDist", GetCamDistance()); // todo: maybe just pass cam dist in w the funciton call
			// aspect corrected fov, used for perspective w component
			float afov = Mathf.Atan(cal.aspect * Mathf.Tan(0.5f * fov * Mathf.Deg2Rad));
			interpolationComputeShader.SetFloat("perspw", 2 * Mathf.Tan(afov));
			interpolationComputeShader.SetVector("viewSize", new Vector4(
				quiltSettings.viewWidth,
				quiltSettings.viewHeight,
				1f / quiltSettings.viewWidth,
				1f / quiltSettings.viewHeight
			));

			List<int> viewPositions = new List<int>();
			List<float> viewOffsets = new List<float>();
			List<int> baseViewPositions = new List<int>();
			int validViewIndex = -1;
			int currentInterp = 1;
			for (int i = 0; i < quiltSettings.numViews; i++) {
				var positions = new [] {
					i % quiltSettings.viewColumns * quiltSettings.viewWidth,
					i / quiltSettings.viewColumns * quiltSettings.viewHeight,
				};
				if (i != 0 && i != quiltSettings.numViews - 1 && i % ViewInterpolation != 0) {
					viewPositions.AddRange(positions);
					viewPositions.AddRange(new [] { validViewIndex, validViewIndex + 1 });
					int div = Mathf.Min(ViewInterpolation, quiltSettings.numViews - 1);
					int divTotal = quiltSettings.numViews / div;
					if (i > divTotal * ViewInterpolation) {
						div = quiltSettings.numViews - divTotal * ViewInterpolation;
					}
					float viewCone = Application.isPlaying && cal.viewCone == 0? Calibration.DEFAULT_VIEWCONE : cal.viewCone;
					float offset = div * Mathf.Tan(viewCone * viewconeModifier * Mathf.Deg2Rad) / (quiltSettings.numViews - 1f);
					float lerp = (float)currentInterp / div;
					currentInterp++;
					viewOffsets.AddRange(new [] { offset, lerp });
				} else {
					baseViewPositions.AddRange(positions);
					validViewIndex++;
					currentInterp = 1;
				}
			}

			int viewCount = viewPositions.Count / 4;
			ComputeBuffer viewPositionsBuffer = new ComputeBuffer(viewPositions.Count / 4, 4 * sizeof(int));
			ComputeBuffer viewOffsetsBuffer = new ComputeBuffer(viewOffsets.Count / 2, 2 * sizeof(float));
			ComputeBuffer baseViewPositionsBuffer = new ComputeBuffer(baseViewPositions.Count / 2, 2 * sizeof(int));
			viewPositionsBuffer.SetData(viewPositions);
			viewOffsetsBuffer.SetData(viewOffsets);
			baseViewPositionsBuffer.SetData(baseViewPositions);

			interpolationComputeShader.SetBuffer(kernelFwd, "viewPositions", viewPositionsBuffer);
			interpolationComputeShader.SetBuffer(kernelFwd, "viewOffsets", viewOffsetsBuffer);
			interpolationComputeShader.SetBuffer(kernelFwd, "baseViewPositions", baseViewPositionsBuffer);
			interpolationComputeShader.SetBuffer(kernelBack, "viewPositions", viewPositionsBuffer);
			interpolationComputeShader.SetBuffer(kernelBack, "viewOffsets", viewOffsetsBuffer);
			interpolationComputeShader.SetBuffer(kernelBack, "baseViewPositions", baseViewPositionsBuffer);
			interpolationComputeShader.SetBuffer(kernelFwdFlicker, "viewPositions", viewPositionsBuffer);
			interpolationComputeShader.SetBuffer(kernelFwdFlicker, "viewOffsets", viewOffsetsBuffer);
			interpolationComputeShader.SetBuffer(kernelFwdFlicker, "baseViewPositions", baseViewPositionsBuffer);
			interpolationComputeShader.SetBuffer(kernelBackFlicker, "viewPositions", viewPositionsBuffer);
			interpolationComputeShader.SetBuffer(kernelBackFlicker, "viewOffsets", viewOffsetsBuffer);
			interpolationComputeShader.SetBuffer(kernelBackFlicker, "baseViewPositions", baseViewPositionsBuffer);
			// interpolationComputeShader.SetInt("viewPositionsCount", viewCount);

			uint blockX,  blockY,  blockZ;
			interpolationComputeShader.GetKernelThreadGroupSizes(kernelFwd, out blockX, out blockY, out blockZ);
			int computeX = quiltSettings.viewWidth / (int)blockX + Mathf.Min(quiltSettings.viewWidth % (int)blockX, 1);
			int computeY = quiltSettings.viewHeight / (int)blockY + Mathf.Min(quiltSettings.viewHeight % (int)blockY, 1);
			int computeZ = viewCount / (int)blockZ + Mathf.Min(viewCount % (int)blockZ, 1);

			if (reduceFlicker) {
				int spanSize = 2 * ViewInterpolation;
				interpolationComputeShader.SetInt("spanSize", spanSize);
				for (int i = 0; i < spanSize; i++) {
					interpolationComputeShader.SetInt("px", i);
					interpolationComputeShader.Dispatch(kernelFwd, quiltSettings.viewWidth / spanSize, computeY, computeZ);
					interpolationComputeShader.Dispatch(kernelBack, quiltSettings.viewWidth / spanSize, computeY, computeZ);
				}
			} else {
				interpolationComputeShader.Dispatch(kernelFwdFlicker, computeX, computeY, computeZ);
				interpolationComputeShader.Dispatch(kernelBackFlicker, computeX, computeY, computeZ);
			}

			if (fillGaps) {
				var fillgapsKernel = interpolationComputeShader.FindKernel("FillGaps");
				interpolationComputeShader.SetTexture(fillgapsKernel, "Result", quiltRT);
				interpolationComputeShader.SetTexture(fillgapsKernel, "ResultDepth", quiltRTDepth);
				interpolationComputeShader.SetBuffer(fillgapsKernel, "viewPositions", viewPositionsBuffer);
				interpolationComputeShader.Dispatch(fillgapsKernel, computeX, computeY, computeZ);
			}

			viewPositionsBuffer.Dispose();
			viewOffsetsBuffer.Dispose();
			baseViewPositionsBuffer.Dispose();
		}

		public float ResetCamera() {
			// scale follows size
			if (scaleFollowsSize)
				transform.localScale = Vector3.one * size;

			// force it to render in perspective
			singleViewCamera.orthographic = false;
			// set up the center view / proj matrix
			if (useFrustumTarget) {
				singleViewCamera.fieldOfView = 2 * Mathf.Atan(Mathf.Abs(size / frustumTarget.localPosition.z)) * Mathf.Rad2Deg;
			} else {
				singleViewCamera.fieldOfView = fov;
			}
			// get distance
			float dist = GetCamDistance();
			
			// set near and far clip planes based on dist
			singleViewCamera.nearClipPlane = Mathf.Max(dist - size * nearClipFactor, 0.1f);
			singleViewCamera.farClipPlane = Mathf.Max(dist + size * farClipFactor, singleViewCamera.nearClipPlane);
			// reset matrices, save center for later
			singleViewCamera.ResetWorldToCameraMatrix();
			singleViewCamera.ResetProjectionMatrix();
			var centerViewMatrix = singleViewCamera.worldToCameraMatrix;
			var centerProjMatrix = singleViewCamera.projectionMatrix;
			centerViewMatrix.m23 -= dist;

			if (useFrustumTarget) {
				Vector3 targetPos = -frustumTarget.localPosition;
				centerViewMatrix.m03 += targetPos.x;
				centerProjMatrix.m02 += targetPos.x / (size * cal.aspect);
				centerViewMatrix.m13 += targetPos.y;
				centerProjMatrix.m12 += targetPos.y / size;
				Debug.Log(
					"View Matrix:\n" +
					centerViewMatrix + "\n" +
					"Proj Matrix:\n" +
					centerProjMatrix
				);
			} else {
				// if we have offsets, handle them here
				if (horizontalFrustumOffset != 0) {
					// centerViewMatrix.m03 += horizontalFrustumOffset * size * cal.aspect;
					float offset = dist * Mathf.Tan(Mathf.Deg2Rad * horizontalFrustumOffset);
					centerViewMatrix.m03 += offset;
					centerProjMatrix.m02 += offset / (size * cal.aspect);
				}
				if (verticalFrustumOffset != 0) {
					float offset = dist * Mathf.Tan(Mathf.Deg2Rad * verticalFrustumOffset);
					centerViewMatrix.m13 += offset;
					centerProjMatrix.m12 += offset / size;
				}
			}
			singleViewCamera.worldToCameraMatrix = centerViewMatrix;
			singleViewCamera.projectionMatrix = centerProjMatrix;
			// set some of the camera properties from inspector
			singleViewCamera.clearFlags = clearFlags;
			singleViewCamera.backgroundColor = background;
			singleViewCamera.cullingMask = cullingMask;
			singleViewCamera.renderingPath = renderingPath;
			singleViewCamera.useOcclusionCulling = occlusionCulling;
			singleViewCamera.allowHDR = allowHDR;
			singleViewCamera.allowMSAA = allowMSAA;
#if UNITY_2017_3_OR_NEWER
			singleViewCamera.allowDynamicResolution = allowDynamicResolution;
#endif
			// return distance (since it is useful after the fact and we have it anyway)
			return dist;
		}

		public void PassSettingsToMaterial(Material lightfieldMat) {
			if (lightfieldMat == null)
				return;

			lightfieldMat.SetFloat("pitch", cal.pitch);

#if UNITY_2020_3_OR_NEWER && UNITY_EDITOR
			// The slope need to be shifted accordingly
			if (loadResults.calibrationFound) {
				float previewHeight = (cal.screenHeight - EditorWindowTabSize);  
				float adjustedSlope = (previewHeight) / (cal.screenWidth * cal.rawSlope) * (cal.flipImageX > 0.5f ? -1.0f : 1.0f);
				lightfieldMat.SetFloat("slope", (float) adjustedSlope);
			} 
#else
            lightfieldMat.SetFloat("slope", cal.slope);
#endif
            lightfieldMat.SetFloat("center", cal.center + centerOffset);
			lightfieldMat.SetFloat("subpixelSize", cal.subp);
			lightfieldMat.SetVector("tile", new Vector4(
                quiltSettings.viewColumns,
                quiltSettings.viewRows,
                quiltSettings.numViews,
                quiltSettings.viewColumns * quiltSettings.viewRows
            ));
            lightfieldMat.SetVector("viewPortion", new Vector4(
                quiltSettings.viewPortionHorizontal,
                quiltSettings.viewPortionVertical
            ));
			
            lightfieldMat.SetVector("aspect", new Vector4(
                Aspect,
                Aspect,
                quiltSettings.overscan ? 1 : 0
            ));
			// Debug.Log( string.Format("set uniforms: \n pitch: {0}, slope: {1}, center: {2}, fringe: {3}, subp: {4}, aspect: {5}",
			// 	cal.pitch, cal.slope, cal.center, cal.fringe, cal.subp, cal.aspect ));

			// so its come to this...
#if UNITY_EDITOR_OSX && UNITY_2019_3_OR_NEWER
			lightfieldMat.SetFloat("verticalOffset", -21f / ScreenHeight);
#elif UNITY_EDITOR_OSX && UNITY_2019_1_OR_NEWER
			lightfieldMat.SetFloat("verticalOffset", -19f / ScreenHeight);
#endif
		}

		public LoadResults ReloadCalibration() {
			Quilt.Settings previousQuiltSettings = quiltSettings;

			LoadResults results = PluginCore.GetLoadResults();
			// loads calibration as well
            // create a calibration object. 
            // if we find that the target display matches a plugged in looking glass,
            // use matching calibration

			cal = new Calibration(0, ScreenWidth, ScreenHeight);

			if (results.calibrationFound) {
				if (!CalibrationManager.TryFindCalibration(LKGName, out Calibration found))
					found = CalibrationManager.GetCalibration(0);
				cal = found;
				targetLKG = cal.index;
			} else {
				// may be unnecessary
				cal.serial = HoloplayDevice.GetSettings(emulatedDevice).name;

				// force aspect to change
				singleViewCamera.aspect = cal.aspect = Aspect;
			}

			// need to set up quilt again bc quilt settings may be changed
			if (!previousQuiltSettings.Equals(quiltSettings)){
				// Debug.Log("set up new quilt settings");
				SetupQuilt();
			}

            PassSettingsToMaterial(lightfieldMat);
            this.loadResults = results;
			return results;
		}

		/// <summary>
		/// Returns the camera's distance from the center.
		/// Will be a positive number.
		/// </summary>
		public float GetCamDistance() {
			if (!useFrustumTarget) {
				return size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
			}
			return Mathf.Abs(frustumTarget.localPosition.z);
		}

		/// <summary>
		/// <para>Sets up the quilt and the quilt <see cref="RenderTexture"/>.</para>
		/// <para>This should be called after modifying custom quilt settings.</para>
		/// </summary>
		public void SetupQuilt() {
			customQuiltSettings.Setup(); // even if not custom quilt, just set this up anyway
			if (quiltRT != null)
				DestroyImmediate(quiltRT);
			
			quiltRT = new RenderTexture(quiltSettings.quiltWidth, quiltSettings.quiltHeight, 0, RenderTextureFormat.Default) {
				filterMode = FilterMode.Point,
				hideFlags = HideFlags.DontSave
			};

			quiltRT.enableRandomWrite = true;
			quiltRT.Create();
			PassSettingsToMaterial(lightfieldMat);

			//Pass some stuff globally for post-processing
			float viewSizeX = (float) quiltSettings.viewWidth / quiltSettings.quiltWidth;
			float viewSizeY = (float) quiltSettings.viewHeight / quiltSettings.quiltHeight;

			Shader.SetGlobalVector("hp_quiltViewSize", new Vector4(
				viewSizeX,
				viewSizeY,
				quiltSettings.viewWidth,
				quiltSettings.viewHeight
			));
		}

		internal Texture2D ReadFrom(RenderTexture source) {
			Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);

			RenderTexture.active = source;
			result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
			result.Apply();
			RenderTexture.active = source;

			return result;
		}

        internal void SaveAsPNGScreenshot(RenderTexture rt) {
			Texture2D screenshot = ReadFrom(rt);

            byte[] bytes = screenshot.EncodeToPNG();
			string quiltInfo = "qs" + quiltSettings.viewColumns + "x" + quiltSettings.viewRows + "a" + cal.aspect;
            string filename = string.Format("{0}/screen_{1}x{2}_{3}_{4}.png",
				Path.GetFullPath("."), rt.width, rt.height, 
				DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), quiltInfo);

            File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
        }
	}
}
