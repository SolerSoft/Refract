//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;

namespace LookingGlass {
	public static class Preview {
		// vars
        static object gameViewSizesInstance;
        public static BindingFlags bindingFlags = 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic;
#if UNITY_2019_1_OR_NEWER
        static int tabSize = 21;
#else
        static int tabSize = 17; //this makes sense i promise
#endif
		public static Type gameViewWindowType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        static MethodInfo getGroup;
		static EditorWindow gameViewWindow;
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
		static int windowInitialized = 2; // a countdown, sort of
		public const string togglePreviewShortcut = "Toggle Preview ⌘E";
#else
		static int windowInitialized = 0;
		public const string togglePreviewShortcut = "Toggle Preview Ctrl + E";
#endif
		public const string manualSettingsPath = "Assets/HoloplayPreviewSettings.asset";
		static ManualPreviewSettings manualPreviewSettings;

		// functions
		// used to subscribe to the scene change update
		[InitializeOnLoadMethod]
		static void InitPreview() {
			// recheck display callback
			EditorSceneManager.sceneOpened += RecheckDisplayTarget;
			// close open windows if there isn't a looking glass
			EditorApplication.update += CloseExtraHoloplayWindows;
		}


		// for when the user switches scenes, update preview if it's open
		static int recheckDelay;
		public static void RecheckDisplayTarget(Scene openScene, OpenSceneMode openSceneMode){
			
			recheckDelay = 1;
			EditorApplication.update += RecheckDisplayTargetDelayed;
		}

		// needs to be delayed because otherwise stuff isn't done setting up
		public static void RecheckDisplayTargetDelayed() {			
			if (recheckDelay-- > 0) return;
			HandlePreview(false);
			EditorApplication.update -= RecheckDisplayTargetDelayed;
		}

		[MenuItem("Holoplay/Toggle Preview %e", false, 1)]	
		public static void TogglePreview() {
			// try to load from manual settings if available
			if (manualPreviewSettings == null) {
				manualPreviewSettings = AssetDatabase.LoadAssetAtPath<ManualPreviewSettings>(manualSettingsPath);
			}
			// handle the preview
			HandlePreview(true);
		}

		public static void HandlePreview(bool toggling = true) {
			var currentWindows = Resources.FindObjectsOfTypeAll(gameViewWindowType);
            
			if (Holoplay.Instance == null){
				// Debug.LogError("[Holoplay] No holoplay instance in the scene");
				// close all windows
				foreach (EditorWindow w in currentWindows) {
					if (w.name == "Holoplay") {
						w.Close();
					} 
				}
				return;
			}
            
			Holoplay.Instance.ReloadCalibration();

			// set standalone resolution
			int screenWidth = Holoplay.Instance.ScreenWidth;
			int screenHeight = Holoplay.Instance.ScreenHeight;
			
			if (UnityEditor.PlayerSettings.defaultScreenWidth != screenWidth)
				UnityEditor.PlayerSettings.defaultScreenWidth = screenWidth;
			if (UnityEditor.PlayerSettings.defaultScreenHeight != screenHeight)
				UnityEditor.PlayerSettings.defaultScreenHeight = screenHeight;
			
			// close the window if its open
			bool windowWasOpen = false;
			int targetDisplay = Holoplay.Instance.targetDisplay;
			string resolutionName = Holoplay.Instance.DeviceTypeName;

			foreach (EditorWindow w in currentWindows) {
				if (w.name == "Holoplay") {
					w.Close();
					windowWasOpen = true;
				} else {
					// to avoid ugliness, if there is a game window open
					// make sure it takes the same resolution
					SetResolution(w, screenWidth, screenHeight, resolutionName);
					// set display number
					SetDisplayNum(w, targetDisplay);
				}
			}
			
			// Debug.Log(windowWasOpen + "was opened " + toggling);
			// // Spawn an object, then immediately destroy it.
            // // This forces Unity to repaint scene, but does not generate a diff in the Unity scene serialization which would require scene to be re-saved
            // // Repainting the scene causes Unity to recalculate UI positions for resized GameViewWindow : EditorWindow
            // GameObject go = new GameObject();
            // GameObject.DestroyImmediate(go);


			if (toggling) {
				if (windowWasOpen) return;
			} else {
				if (!windowWasOpen) return;
			}
			/*
				logic for multiplexing
				- get number of holoplays in the scene
				- for the target display of each Holoplay
					- create a window on that display
					- just assign 
			 */
			var hps = GameObject.FindObjectsOfType<Holoplay>();
			// open up a preview in the looking glass even if no holoplays found
			if (hps.Length == 0) {
				// Debug.Log("no hp");
                // SetupPreviewWindow(null);
			}
			foreach (var hp in hps) {
                SetupPreviewWindow(hp);
			}
		}

		static void SetupPreviewWindow(Holoplay hp) {
            bool isMac = Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor;
			int screenWidth = hp.ScreenWidth;
			int screenHeight = hp.ScreenHeight;
			int xpos = hp.cal.xpos;//Calibration.XPOS_DEFAULT;
			int ypos = hp.cal.ypos;
			int targetDisplay = hp.targetDisplay;
			int targetLKG = hp.targetLKG;
        	// Debug.Log(string.Format("set up preview window for lkg index {0}, target lkg {1}, target display {2}, {3}", hp.cal.index, hp.targetLKG, hp.targetDisplay, hp.name));
						
			if (UnityEditor.PlayerSettings.defaultScreenWidth != screenWidth)
				UnityEditor.PlayerSettings.defaultScreenWidth = screenWidth;
			if (UnityEditor.PlayerSettings.defaultScreenHeight != screenHeight)
				UnityEditor.PlayerSettings.defaultScreenHeight = screenHeight;
			
			// otherwise create one
			gameViewWindow = (EditorWindow)EditorWindow.CreateInstance(gameViewWindowType);
			gameViewWindow.name = "Holoplay";
			// TODO: not sure how it works on mac
			if (!isMac) {
				var showModeType = typeof(Editor).Assembly.GetType("UnityEditor.ShowMode");
				var showWithModeInfo = gameViewWindowType.GetMethod("ShowWithMode", bindingFlags);
				showWithModeInfo.Invoke(gameViewWindow, new [] { Enum.ToObject(showModeType, 1) });
			} else {
				
				if (windowInitialized == 2) {
					EditorApplication.update += UpdateWindowPos;
					windowInitialized = 1;
				}
				gameViewWindow = EditorWindow.GetWindow(gameViewWindowType);
			}
			// set window size and position
			gameViewWindow.maxSize = new Vector2(screenWidth, screenHeight + tabSize);
			gameViewWindow.minSize = gameViewWindow.maxSize; 

			// Debug.Log("set up preview window on target lkg:" + targetDisplay  + " (" + xpos + "," + ypos);
			if (manualPreviewSettings != null && manualPreviewSettings.manualPosition) {
				xpos = manualPreviewSettings.position.x;
				ypos = manualPreviewSettings.position.y;
				gameViewWindow.maxSize = new Vector2(manualPreviewSettings.resolution.x, manualPreviewSettings.resolution.y + tabSize);
				gameViewWindow.minSize = gameViewWindow.maxSize; 
			}

			// tab size
			ypos -= tabSize;

			// account for display scaling
#if UNITY_EDITOR_WIN
			xpos = xpos * 96 / Mathf.RoundToInt(Screen.dpi);
			ypos = ypos * 96 / Mathf.RoundToInt(Screen.dpi);
#endif
			gameViewWindow.position = new Rect(
				xpos, ypos, gameViewWindow.maxSize.x, gameViewWindow.maxSize.y);
			// set the zoom and resolution
			SetZoom(gameViewWindow);

			string resolutionName = Holoplay.Instance.DeviceTypeName;
			SetResolution(gameViewWindow, screenWidth, screenHeight, resolutionName);

			// set display number
			SetDisplayNum(gameViewWindow, targetDisplay);
		}

		static void SetDisplayNum(EditorWindow gameViewWindow, int targetDisplay)
		{
			// set display number
#if UNITY_2019_3_OR_NEWER
			gameViewWindowType.GetMethod("set_targetDisplay", bindingFlags).Invoke(gameViewWindow, new object[] {targetDisplay});
#else
			var displayNum = gameViewWindowType.GetField("m_TargetDisplay", bindingFlags);
			displayNum.SetValue(gameViewWindow, targetDisplay);
#endif
		}

		static void CloseExtraHoloplayWindows() {
			var currentWindows = Resources.FindObjectsOfTypeAll(gameViewWindowType); 
            PluginCore.GetLoadResults();
			if (manualPreviewSettings != null && CalibrationManager.GetCalibrationCount() < 1) {
				foreach (EditorWindow w in currentWindows) {
					if (w.name == "Holoplay") {
						w.Close();
						Debug.Log("[Holoplay] Closing extra Holoplay window");
					}
				}
			}		
			
			EditorApplication.update -= CloseExtraHoloplayWindows;
		}

		// this won't work for multiple monitors
		// but multi-display doesn't work on mac anyway

		static void UpdateWindowPos() {
			if (windowInitialized > 0) {
				windowInitialized--;
			} else {
				int xpos = CalibrationManager.GetCalibration(0).xpos;
				int ypos = CalibrationManager.GetCalibration(0).ypos;
				if (manualPreviewSettings != null && manualPreviewSettings.manualPosition) {
					xpos = manualPreviewSettings.position.x;
					ypos = manualPreviewSettings.position.y;
				}
#if UNITY_2019_3_OR_NEWER && (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
#else
				gameViewWindow.position = new Rect(
					xpos, ypos - 5,
					gameViewWindow.maxSize.x, gameViewWindow.maxSize.y);
#endif
				EditorApplication.update -= UpdateWindowPos;
			}
		}

		static void SetZoom(EditorWindow gameViewWindow) {
            float targetScale = 1;
            var areaField = gameViewWindowType.GetField("m_ZoomArea", bindingFlags);
            var areaObj = areaField.GetValue(gameViewWindow);
            var scaleField = areaObj.GetType().GetField("m_Scale", bindingFlags);
            scaleField.SetValue(areaObj, new Vector2(targetScale, targetScale));
		}

		public static void SetResolution(EditorWindow gameViewWindow, int width, int height, string deviceTypeName) {
            PropertyInfo selectedSizeIndexProp = gameViewWindowType.GetProperty (
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );            

			int index;
            var currentGroupType = CurrentGameViewSizeGroupType;
			string customSizeName = deviceTypeName + string.Format(" {0} x {1}", width, height);
			bool sizeExists = FindSize(currentGroupType, customSizeName, out index);

            if (!sizeExists) {
				AddCustomSize (currentGroupType, width, height, deviceTypeName);           	
            }
			
            selectedSizeIndexProp.SetValue(gameViewWindow, index, null);

			// Spawn an object, then immediately destroy it.
            // This forces Unity to repaint scene, but does not generate a diff in the Unity scene serialization which would require scene to be re-saved
            // Repainting the scene causes Unity to recalculate UI positions for resized GameViewWindow : EditorWindow
            GameObject go = new GameObject();
            GameObject.DestroyImmediate(go);
		}

		public static GameViewSizeGroupType CurrentGameViewSizeGroupType
		{
			get{
				Type sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
				var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
				var instanceProp = singleType.GetProperty("instance");
				getGroup = sizesType.GetMethod("GetGroup");
				gameViewSizesInstance = instanceProp.GetValue(null, null);
				var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
				var currentGroupType = (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
				return currentGroupType;
			}
		}

		[MenuItem("Assets/Create/Holoplay/Manual Preview Settings")]
		static void CreateManualPreviewAsset() {
			ManualPreviewSettings previewSettings = AssetDatabase.LoadAssetAtPath<ManualPreviewSettings>(manualSettingsPath);
			if (previewSettings == null) {
				previewSettings = ScriptableObject.CreateInstance<ManualPreviewSettings>();
				AssetDatabase.CreateAsset(previewSettings, manualSettingsPath);
				AssetDatabase.SaveAssets();
			}
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = previewSettings;
		}

		/// <summary>
        /// Adds a game view size.
        /// </summary>
        /// <param name="viewSizeType(default to 1 now)">Aspect ratio 0 or fixed resolution 1</param>
        /// <param name="sizeGroupType">Build target</param>
        /// <param name="width">Width of game view resolution</param>
        /// <param name="height">Height of game view resolution</param>
        /// <param name="text">Label of game view resolution</param>
        public static void AddCustomSize( /*int viewSizeType,*/ GameViewSizeGroupType sizeGroupType, int width, int height, string text)
        {
			// already there
            var group = GetGroup(sizeGroupType);
            var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize");
            Type gameViewSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");

			// first parameter is 1 bc it will be fixed resolution in current use cases
            object[] gameViewSizeConstructorArgs = new object[] { 1, width, height, text };

            // select a constructor which has 4 elements which are enums/ints/strings
            ConstructorInfo gameViewSizeConstructor = gameViewSizeType.GetConstructors()
                .FirstOrDefault(x =>
             {
                 // lambda function defines a filter/predicate of ConstructorInfo objects.
                 // The first constructor, if any exists, which satisfies the predicate (true) will be returned
                 if (x.GetParameters().Length != gameViewSizeConstructorArgs.Length)
                 {
                     return false;
                 }

                 // iterate through constructor types + constructor args. If any mismatch, reject
                 for (int i = 0; i < gameViewSizeConstructorArgs.Length; i++)
                 {
                     Type constructorParamType = x.GetParameters()[i].ParameterType;
                     Type constructorArgType = gameViewSizeConstructorArgs[i].GetType();

                     bool isMatch = constructorParamType == constructorArgType || constructorParamType.IsEnum && constructorArgType == typeof(int);
                     if (!isMatch) return false;
                 }

                 // constructor with these params must be able to receive these args
                 return true;
             });

            if (gameViewSizeConstructor != null)
            {
                var newSize = gameViewSizeConstructor.Invoke(gameViewSizeConstructorArgs);
                addCustomSize.Invoke(group, new object[] { newSize });
            }
			Type sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            sizesType.GetMethod("SaveToHDD").Invoke(gameViewSizesInstance, null);
        }

		static object GetGroup(GameViewSizeGroupType type)
        {
            return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
        }

		/// <summary>
        /// Retrieves index of a resolution in GetDisplayTexts collection, if it exists in the collection.
        /// </summary>
        /// <param name="sizeGroupType">Group to search: Standalone/Android</param>
        /// <param name="text">String to search GetDisplayTexts for. Only [0-9] chars in label and GetDisplayTexts are actually considered in search</param>
        /// <param name="index">Index of match if a match was found, or first out-of-bounds index if no match was found</param>
        /// <returns>True if resolution in collection, false if resolution is not in collection</returns>
        public static bool FindSize(GameViewSizeGroupType sizeGroupType, string text, out int index)
        {
            index = -1;

            text = System.Text.RegularExpressions.Regex.Replace(text, @"[\D]", "");
            var group = GetGroup(sizeGroupType);
            var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
            var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
            for (int i = 0; i < displayTexts.Length; i++)
            {
                // compare the digits of the known resolution names, to the digits of the ideal resolution
                // if digits are a one-for-one match using string ==, then we have a match
                string display = System.Text.RegularExpressions.Regex.Replace(displayTexts[i], @"[\D]", "");
                if (display == text)
                {
                    index = i;
                    return true;
                }
            }

            // otherwise set to first index outside of array bounds, return false to warn user that size is not actually in array
            // inside of SetGameViewSize we will add the as-of-yet unknown size at index [first_index_outside_of_array_bounds]
            index = displayTexts.Length;
            return false;
        }
	}
}
